using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using NetworkSignalCore.Core.Messages;
using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Client.Connection;

internal sealed class SignalRTransport : IDisposable
{
    private readonly ILogger<SignalRTransport> _logger;
    private HubConnection? _hub;

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public event Action?             OnConnected;
    public event Action<Exception?>? OnDisconnected;
    public event Action?             OnReconnecting;
    public event Action?             OnReconnected;

    public event Action<string, string, string>?    OnClientRpc;    // controller, method, payloadJson
    public event Action<string, string, string>?    OnSyncState;    // networkId, field, valueJson
    public event Action<RoomInfo>?                  OnRoomUpdate;
    public event Action<RoomInfo[]>?                OnRoomList;
    public event Action<MatchFoundNotification>?    OnMatchFound;
    public event Action<string, string>?            OnError;        // code, message
    public event Action<bool, string, string>?      OnAuthResult;   // success, playerId, message
    public event Action<PartyInfo>?                 OnPartyUpdate;
    public event Action<long>?                      OnPong;

    public SignalRTransport(ILogger<SignalRTransport> logger)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(string url, string? token, CancellationToken ct)
    {
        State = ConnectionState.Connecting;

        var uri = string.IsNullOrEmpty(token)
            ? url
            : $"{url}?access_token={Uri.EscapeDataString(token)}";

        _hub = new HubConnectionBuilder()
            .WithUrl(uri)
            .Build();

        RegisterServerHandlers(_hub);

        _hub.Closed += ex =>
        {
            State = ConnectionState.Disconnected;
            OnDisconnected?.Invoke(ex);
            return Task.CompletedTask;
        };

        _hub.Reconnecting += _ =>
        {
            State = ConnectionState.Reconnecting;
            OnReconnecting?.Invoke();
            return Task.CompletedTask;
        };

        _hub.Reconnected += _ =>
        {
            State = ConnectionState.Connected;
            OnReconnected?.Invoke();
            return Task.CompletedTask;
        };

        await _hub.StartAsync(ct).ConfigureAwait(false);
        State = ConnectionState.Connected;
        OnConnected?.Invoke();
    }

    public async Task DisconnectAsync()
    {
        if (_hub is null) return;
        await _hub.StopAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Invokes a hub method with zero or more arguments supplied as an explicit array.
    /// Using an explicit array avoids the C# params ambiguity where passing object?[] would be
    /// treated as a single element rather than being spread across the switch.
    /// </summary>
    public Task InvokeAsync(string method, object?[] args)
    {
        EnsureConnected();
        return args.Length switch
        {
            0 => _hub!.InvokeAsync(method),
            1 => _hub!.InvokeAsync(method, args[0]),
            2 => _hub!.InvokeAsync(method, args[0], args[1]),
            3 => _hub!.InvokeAsync(method, args[0], args[1], args[2]),
            4 => _hub!.InvokeAsync(method, args[0], args[1], args[2], args[3]),
            _ => throw new ArgumentException("Too many arguments for InvokeAsync overload.")
        };
    }

    /// <summary>Convenience overload for a single argument.</summary>
    public Task InvokeAsync(string method, object? arg)
        => InvokeAsync(method, new[] { arg });

    /// <summary>Convenience overload for zero arguments.</summary>
    public Task InvokeAsync(string method)
        => InvokeAsync(method, Array.Empty<object?>());

    public Task<T> InvokeAsync<T>(string method, object?[] args)
    {
        EnsureConnected();
        return args.Length switch
        {
            0 => _hub!.InvokeAsync<T>(method),
            1 => _hub!.InvokeAsync<T>(method, args[0]),
            2 => _hub!.InvokeAsync<T>(method, args[0], args[1]),
            3 => _hub!.InvokeAsync<T>(method, args[0], args[1], args[2]),
            4 => _hub!.InvokeAsync<T>(method, args[0], args[1], args[2], args[3]),
            _ => throw new ArgumentException("Too many arguments for InvokeAsync<T> overload.")
        };
    }

    public Task<T> InvokeAsync<T>(string method)
        => InvokeAsync<T>(method, Array.Empty<object?>());

    private void RegisterServerHandlers(HubConnection hub)
    {
        hub.On<string, string, string>("ReceiveClientRpc",
            (controller, method, payloadJson) => OnClientRpc?.Invoke(controller, method, payloadJson));

        hub.On<string, string, string>("ReceiveSyncState",
            (networkId, field, valueJson) => OnSyncState?.Invoke(networkId, field, valueJson));

        hub.On<RoomInfo>("ReceiveRoomUpdate",
            room => OnRoomUpdate?.Invoke(room));

        hub.On<RoomInfo[]>("ReceiveRoomList",
            rooms => OnRoomList?.Invoke(rooms));

        hub.On<MatchFoundNotification>("ReceiveMatchFound",
            notification => OnMatchFound?.Invoke(notification));

        hub.On<string, string>("ReceiveError",
            (code, message) => OnError?.Invoke(code, message));

        hub.On<bool, string, string>("ReceiveAuthResult",
            (success, playerId, message) => OnAuthResult?.Invoke(success, playerId, message));

        hub.On<PartyInfo>("ReceivePartyUpdate",
            party => OnPartyUpdate?.Invoke(party));

        hub.On<long>("ReceivePong",
            timestamp => OnPong?.Invoke(timestamp));
    }

    private void EnsureConnected()
    {
        if (_hub is null || State != ConnectionState.Connected)
            throw new InvalidOperationException("Transport is not connected.");
    }

    public void Dispose()
    {
        _hub?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
