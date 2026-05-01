using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NetworkSignalCore.Client.Connection;
using NetworkSignalCore.Client.Matchmaking;
using NetworkSignalCore.Client.Rooms;
using NetworkSignalCore.Client.Rpc;
using NetworkSignalCore.Client.Sync;
using NetworkSignalCore.Core.Messages;
using NetworkSignalCore.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetworkSignalCore.Client;

public sealed class NetworkClient : IDisposable
{
    private readonly NetworkClientOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<NetworkClient> _logger;

    private readonly SignalRTransport _transport;
    private readonly ClientRpcDispatcher _rpcDispatcher;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    private int _reconnectAttempts;
    private volatile bool _disposed;
    private volatile bool _isConnected;
    private volatile bool _isAuthenticated;

    public bool IsConnected     => _isConnected;
    public bool IsAuthenticated => _isAuthenticated;
    public PlayerInfo? LocalPlayer { get; private set; }

    public ClientRoomManager        Rooms       { get; }
    public ClientMatchmakingManager Matchmaking { get; }
    public ClientSyncManager        Sync        { get; }

    public event Action?                  OnConnected;
    public event Action<Exception?>?      OnDisconnected;
    public event Action<string, string>?  OnError;   // code, message

    /// <param name="options">Connection and behaviour options.</param>
    /// <param name="loggerFactory">
    ///   Optional logger factory. Pass null to suppress all logging.
    ///   In Unity, provide a Microsoft.Extensions.Logging.ILoggerFactory adapter.
    /// </param>
    public NetworkClient(NetworkClientOptions options, ILoggerFactory? loggerFactory = null)
    {
        _options       = options;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

        _logger        = _loggerFactory.CreateLogger<NetworkClient>();
        _transport     = new SignalRTransport(_loggerFactory.CreateLogger<SignalRTransport>());
        _rpcDispatcher = new ClientRpcDispatcher(_loggerFactory.CreateLogger<ClientRpcDispatcher>());

        Sync = new ClientSyncManager(_loggerFactory.CreateLogger<ClientSyncManager>());

        Rooms = new ClientRoomManager(
            invoke: (method, args) => _transport.InvokeAsync(method, args),
            logger: _loggerFactory.CreateLogger<ClientRoomManager>());

        Matchmaking = new ClientMatchmakingManager(
            invoke: (method, args) => _transport.InvokeAsync(method, args),
            logger: _loggerFactory.CreateLogger<ClientMatchmakingManager>());

        WireTransportEvents();
    }

    public NetworkClient(Action<NetworkClientOptions> configure, ILoggerFactory? loggerFactory = null)
        : this(BuildOptions(configure), loggerFactory) { }

    private static NetworkClientOptions BuildOptions(Action<NetworkClientOptions> configure)
    {
        var opts = new NetworkClientOptions();
        configure(opts);
        return opts;
    }

    private void WireTransportEvents()
    {
        _transport.OnConnected += () =>
        {
            _isConnected       = true;
            _reconnectAttempts = 0;
            _logger.LogInformation("Connected to {Url}.", _options.ServerUrl);
            OnConnected?.Invoke();
        };

        _transport.OnDisconnected += ex =>
        {
            _isConnected     = false;
            _isAuthenticated = false;
            LocalPlayer      = null;
            _logger.LogInformation(ex, "Disconnected.");
            OnDisconnected?.Invoke(ex);

            if (_options.AutoReconnect && !_disposed)
                _ = TryReconnectAsync();
        };

        _transport.OnReconnecting += () =>
        {
            _isConnected = false;
            _logger.LogInformation("Reconnecting...");
        };

        _transport.OnReconnected += () =>
        {
            _isConnected = true;
            _logger.LogInformation("Reconnected.");
            OnConnected?.Invoke();
        };

        _transport.OnClientRpc += (controller, method, payload) =>
            _rpcDispatcher.Dispatch(controller, method, payload);

        _transport.OnSyncState += (networkId, field, valueJson) =>
            Sync.ApplySync(networkId, field, valueJson);

        _transport.OnRoomUpdate += room  => Rooms.HandleRoomUpdate(room);
        _transport.OnRoomList   += rooms => Rooms.HandleRoomList(rooms);
        _transport.OnMatchFound += n     => Matchmaking.HandleMatchFound(n);

        _transport.OnError += (code, message) =>
        {
            _logger.LogWarning("Server error [{Code}]: {Message}", code, message);
            OnError?.Invoke(code, message);
        };

        _transport.OnAuthResult += HandleAuthResult;

        _transport.OnPong += timestamp =>
            _logger.LogDebug("Pong received, timestamp {Timestamp}.", timestamp);
    }

    private void HandleAuthResult(bool success, string playerId, string message)
    {
        _isAuthenticated = success;
        if (success)
        {
            LocalPlayer = new PlayerInfo
            {
                PlayerId    = playerId,
                ConnectedAt = DateTimeOffset.UtcNow,
            };
            _logger.LogInformation("Authenticated as player '{PlayerId}'.", playerId);
        }
        else
        {
            _logger.LogWarning("Authentication failed: {Message}", message);
        }
    }

    public Task ConnectAsync(CancellationToken ct = default)
        => _transport.ConnectAsync(_options.ServerUrl, _options.AuthToken, ct);

    public Task DisconnectAsync()
        => _transport.DisconnectAsync();

    public async Task<bool> AuthenticateAsync(string token)
    {
        var tcs       = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var responded = false;

        // One-shot: fires once, restores the persistent handler, resolves the TCS.
        void OneShot(bool success, string playerId, string message)
        {
            responded = true;
            _transport.OnAuthResult -= OneShot;
            _transport.OnAuthResult += HandleAuthResult;
            HandleAuthResult(success, playerId, message);
            tcs.TrySetResult(success);
        }

        _transport.OnAuthResult -= HandleAuthResult;
        _transport.OnAuthResult += OneShot;

        try
        {
            await _transport.InvokeAsync("Authenticate", new object?[] { token }).ConfigureAwait(false);
            return await tcs.Task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (!responded)
            {
                _transport.OnAuthResult -= OneShot;
                _transport.OnAuthResult += HandleAuthResult;
            }
            _logger.LogError(ex, "AuthenticateAsync failed.");
            return false;
        }
    }

    public Task InvokeServerRpcAsync(string controller, string method, object? payload)
    {
        // Serialize with the runtime type so that derived properties are not lost.
        var payloadJson = payload is null
            ? "{}"
            : JsonSerializer.Serialize(payload, payload.GetType(), SerializerOptions);
        return _transport.InvokeAsync("InvokeServerRpc", new object?[] { controller, method, payloadJson });
    }

    public void RegisterBehaviour(string controllerName, NetworkBehaviourBase behaviour)
    {
        behaviour.Init((ctrl, meth, pl) => InvokeServerRpcAsync(ctrl, meth, pl));
        _rpcDispatcher.Register(controllerName, behaviour);
    }

    public void UnregisterBehaviour(string controllerName)
        => _rpcDispatcher.Unregister(controllerName);

    public Task PingAsync()
        => _transport.InvokeAsync("Ping", new object?[] { DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });

    private async Task TryReconnectAsync()
    {
        while (_reconnectAttempts < _options.MaxReconnectAttempts && !_disposed)
        {
            _reconnectAttempts++;
            _logger.LogInformation(
                "Auto-reconnect attempt {Attempt}/{Max} in {Delay}s...",
                _reconnectAttempts,
                _options.MaxReconnectAttempts,
                _options.ReconnectDelay.TotalSeconds);

            await Task.Delay(_options.ReconnectDelay).ConfigureAwait(false);
            if (_disposed) return;

            try
            {
                await _transport.ConnectAsync(_options.ServerUrl, _options.AuthToken, CancellationToken.None)
                    .ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reconnect attempt {Attempt} failed.", _reconnectAttempts);
            }
        }

        _logger.LogError("All {Max} reconnect attempts exhausted.", _options.MaxReconnectAttempts);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _transport.Dispose();
    }
}
