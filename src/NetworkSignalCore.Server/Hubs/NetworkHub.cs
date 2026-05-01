using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Messages;
using NetworkSignalCore.Server.Matchmaking;
using NetworkSignalCore.Server.Rpc;

namespace NetworkSignalCore.Server.Hubs;

public sealed class NetworkHub : Hub<INetworkClient>
{
    private readonly IAuthProvider _auth;
    private readonly ISessionManager _sessions;
    private readonly IRoomManager _rooms;
    private readonly IRpcDispatcher _rpc;
    private readonly MatchmakingManager _matchmaking;
    private readonly ILogger<NetworkHub> _logger;

    public NetworkHub(
        IAuthProvider auth,
        ISessionManager sessions,
        IRoomManager rooms,
        IRpcDispatcher rpc,
        MatchmakingManager matchmaking,
        ILogger<NetworkHub> logger)
    {
        _auth = auth;
        _sessions = sessions;
        _rooms = rooms;
        _rpc = rpc;
        _matchmaking = matchmaking;
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        _matchmaking.DequeueAsync(connectionId);
        await _rooms.LeaveRoomAsync(connectionId);
        _sessions.RemoveSession(connectionId);

        _logger.LogInformation("Client disconnected: {ConnectionId}", connectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public Task Ping(long timestamp)
        => Clients.Caller.ReceivePong(timestamp);

    public async Task Authenticate(string token)
    {
        var result = await _auth.AuthenticateAsync(token, Context.ConnectionAborted);

        if (!result.Success || result.Player is null)
        {
            await Clients.Caller.ReceiveAuthResult(false, string.Empty, result.Message);
            return;
        }

        _sessions.AddSession(Context.ConnectionId, result.Player);
        await Clients.Caller.ReceiveAuthResult(true, result.Player.PlayerId, result.Message);

        _logger.LogInformation("Authenticated: {ConnectionId} → {PlayerId}", Context.ConnectionId, result.Player.PlayerId);
    }

    public async Task InvokeServerRpc(string controller, string method, string payloadJson)
    {
        if (!_sessions.IsAuthenticated(Context.ConnectionId))
        {
            await Clients.Caller.ReceiveError("NOT_AUTHENTICATED", "Authentication required.");
            return;
        }

        await _rpc.DispatchAsync(Context.ConnectionId, controller, method, payloadJson);
    }

    public async Task CreateRoom(CreateRoomRequest request)
    {
        if (!_sessions.IsAuthenticated(Context.ConnectionId))
        {
            await Clients.Caller.ReceiveError("NOT_AUTHENTICATED", "Authentication required.");
            return;
        }

        var result = await _rooms.CreateRoomAsync(Context.ConnectionId, request);
        if (!result.Success)
            await Clients.Caller.ReceiveError("CREATE_ROOM_FAILED", result.Message);
    }

    public async Task JoinRoom(JoinRoomRequest request)
    {
        if (!_sessions.IsAuthenticated(Context.ConnectionId))
        {
            await Clients.Caller.ReceiveError("NOT_AUTHENTICATED", "Authentication required.");
            return;
        }

        var result = await _rooms.JoinRoomAsync(Context.ConnectionId, request);
        if (!result.Success)
            await Clients.Caller.ReceiveError("JOIN_ROOM_FAILED", result.Message);
    }

    public async Task LeaveRoom()
    {
        var result = await _rooms.LeaveRoomAsync(Context.ConnectionId);
        if (!result.Success)
            await Clients.Caller.ReceiveError("LEAVE_ROOM_FAILED", result.Message);
    }

    public async Task KickPlayer(KickPlayerRequest request)
    {
        if (!_sessions.IsAuthenticated(Context.ConnectionId))
        {
            await Clients.Caller.ReceiveError("NOT_AUTHENTICATED", "Authentication required.");
            return;
        }

        var result = await _rooms.KickPlayerAsync(Context.ConnectionId, request);
        if (!result.Success)
            await Clients.Caller.ReceiveError("KICK_FAILED", result.Message);
    }

    public async Task StartGame()
    {
        if (!_sessions.IsAuthenticated(Context.ConnectionId))
        {
            await Clients.Caller.ReceiveError("NOT_AUTHENTICATED", "Authentication required.");
            return;
        }

        var result = await _rooms.StartGameAsync(Context.ConnectionId);
        if (!result.Success)
            await Clients.Caller.ReceiveError("START_GAME_FAILED", result.Message);
    }

    public async Task GetRoomList()
    {
        var rooms = _rooms.GetPublicRooms();
        await Clients.Caller.ReceiveRoomList(rooms.ToArray());
    }

    public Task JoinMatchmakingQueue(JoinQueueRequest request)
    {
        if (!_sessions.IsAuthenticated(Context.ConnectionId))
            return Clients.Caller.ReceiveError("NOT_AUTHENTICATED", "Authentication required.");

        _matchmaking.EnqueueAsync(Context.ConnectionId, request);
        return Task.CompletedTask;
    }

    public Task LeaveMatchmakingQueue()
    {
        _matchmaking.DequeueAsync(Context.ConnectionId);
        return Task.CompletedTask;
    }

    public Task AcceptMatch(string matchId)
        => _matchmaking.AcceptMatchAsync(Context.ConnectionId, matchId);

    public Task DeclineMatch(string matchId)
        => _matchmaking.DeclineMatchAsync(Context.ConnectionId, matchId);
}
