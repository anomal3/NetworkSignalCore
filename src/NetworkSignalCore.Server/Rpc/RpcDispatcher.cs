using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Attributes;
using NetworkSignalCore.Server.AntiCheat;
using NetworkSignalCore.Server.Hubs;

namespace NetworkSignalCore.Server.Rpc;

public sealed class RpcDispatcher : IRpcDispatcher
{
    private readonly ConcurrentDictionary<string, RpcMethodEntry> _handlers = new();
    private readonly IHubContext<NetworkHub, INetworkClient> _hubContext;
    private readonly ISessionManager _sessions;
    private readonly IRoomManager _rooms;
    private readonly INetworkSerializer _serializer;
    private readonly AntiCheatService _antiCheat;
    private readonly ILogger<RpcDispatcher> _logger;

    public RpcDispatcher(
        IHubContext<NetworkHub, INetworkClient> hubContext,
        ISessionManager sessions,
        IRoomManager rooms,
        INetworkSerializer serializer,
        AntiCheatService antiCheat,
        ILogger<RpcDispatcher> logger)
    {
        _hubContext = hubContext;
        _sessions   = sessions;
        _rooms      = rooms;
        _serializer = serializer;
        _antiCheat  = antiCheat;
        _logger     = logger;
    }

    public void RegisterController(NetworkController controller)
    {
        var type = controller.GetType();
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var rpcAttr = method.GetCustomAttribute<ServerRpcAttribute>();
            if (rpcAttr == null) continue;

            var methodName = rpcAttr.MethodName ?? method.Name;
            var key        = $"{type.Name}.{methodName}";

            var entry = new RpcMethodEntry
            {
                Controller = controller,
                Method     = method,
                Attribute  = rpcAttr,
                Parameters = method.GetParameters(),
                Validators = method.GetCustomAttributes<ValidatedAttribute>().ToArray(),
            };

            _handlers[key] = entry;
            _logger.LogDebug("Registered ServerRpc: {Key}", key);
        }
    }

    public async Task DispatchAsync(string connectionId, string controller, string method, string payloadJson)
    {
        var key = $"{controller}.{method}";

        if (!_handlers.TryGetValue(key, out var entry))
        {
            _logger.LogWarning("No ServerRpc handler for {Key}", key);
            await SendErrorAsync(connectionId, "RPC_NOT_FOUND", $"No handler: {key}");
            return;
        }

        var player = _sessions.GetPlayer(connectionId);

        if (entry.Attribute.RequireAuth && player == null)
        {
            await SendErrorAsync(connectionId, "NOT_AUTHENTICATED", "Authentication required.");
            return;
        }

        if (entry.Attribute.RequireRoom && _rooms.GetRoomByConnectionId(connectionId) == null)
        {
            await SendErrorAsync(connectionId, "NOT_IN_ROOM", "Must be in a room.");
            return;
        }

        var payload = DeserializePayload(entry, payloadJson);

        var validation = _antiCheat.Validate(connectionId, player!, method, payload);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Anti-cheat blocked {Key} from {ConnectionId}: {Reason}", key, connectionId, validation.Reason);
            await SendErrorAsync(connectionId, "ANTICHEAT_BLOCKED", validation.Reason);
            return;
        }

        entry.Controller.SetContext(connectionId, player, _hubContext, _sessions, _rooms, _serializer);

        try
        {
            var args   = BuildArgs(entry, payload);
            var result = entry.Method.Invoke(entry.Controller, args);

            if (result is Task task)
                await task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ServerRpc {Key}", key);
            await SendErrorAsync(connectionId, "RPC_ERROR", "Internal server error.");
        }
    }

    private object? DeserializePayload(RpcMethodEntry entry, string payloadJson)
    {
        if (entry.Parameters.Length == 0) return null;
        var paramType = entry.Parameters[0].ParameterType;
        return _serializer.Deserialize(payloadJson, paramType);
    }

    private object?[] BuildArgs(RpcMethodEntry entry, object? payload)
    {
        if (entry.Parameters.Length == 0) return Array.Empty<object?>();
        return new[] { payload };
    }

    private Task SendErrorAsync(string connectionId, string code, string message)
        => _hubContext.Clients.Client(connectionId).ReceiveError(code, message);
}
