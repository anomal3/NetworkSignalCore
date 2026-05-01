using Microsoft.AspNetCore.SignalR;
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Models;
using NetworkSignalCore.Server.Hubs;

namespace NetworkSignalCore.Server.Rpc;

public abstract class NetworkController
{
    private string _callerConnectionId = "";
    private PlayerInfo? _callerPlayer;
    private IHubContext<NetworkHub, INetworkClient>? _hubContext;
    private ISessionManager? _sessions;
    private IRoomManager? _rooms;
    private INetworkSerializer? _serializer;

    internal void SetContext(
        string connectionId,
        PlayerInfo? player,
        IHubContext<NetworkHub, INetworkClient> hubContext,
        ISessionManager sessions,
        IRoomManager rooms,
        INetworkSerializer serializer)
    {
        _callerConnectionId = connectionId;
        _callerPlayer       = player;
        _hubContext         = hubContext;
        _sessions           = sessions;
        _rooms              = rooms;
        _serializer         = serializer;
    }

    internal string InternalConnectionId => _callerConnectionId;

    protected string CallerConnectionId => _callerConnectionId;
    protected PlayerInfo? Caller         => _callerPlayer;
    protected ISessionManager Sessions   => _sessions!;
    protected IRoomManager Rooms         => _rooms!;

    protected Task SendToCallerAsync(string method, object? payload = null)
    {
        var json = Serialize(payload);
        return _hubContext!.Clients.Client(_callerConnectionId).ReceiveClientRpc(GetType().Name, method, json);
    }

    protected Task SendToPlayerAsync(string playerId, string method, object? payload = null)
    {
        var connId = _sessions!.GetConnectionId(playerId);
        if (connId == null) return Task.CompletedTask;
        var json = Serialize(payload);
        return _hubContext!.Clients.Client(connId).ReceiveClientRpc(GetType().Name, method, json);
    }

    protected Task SendToRoomAsync(string method, object? payload = null)
    {
        var room = _rooms!.GetRoomByConnectionId(_callerConnectionId);
        if (room == null) return Task.CompletedTask;
        var json = Serialize(payload);
        return _hubContext!.Clients.Group(room.RoomId).ReceiveClientRpc(GetType().Name, method, json);
    }

    protected Task SendToGroupAsync(string groupId, string method, object? payload = null)
    {
        var json = Serialize(payload);
        return _hubContext!.Clients.Group(groupId).ReceiveClientRpc(GetType().Name, method, json);
    }

    protected Task SendToAllAsync(string method, object? payload = null)
    {
        var json = Serialize(payload);
        return _hubContext!.Clients.All.ReceiveClientRpc(GetType().Name, method, json);
    }

    private string Serialize(object? payload)
        => payload == null ? "{}" : _serializer!.Serialize(payload);
}
