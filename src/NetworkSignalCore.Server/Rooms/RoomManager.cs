using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Messages;
using NetworkSignalCore.Core.Models;
using NetworkSignalCore.Server.Hubs;

namespace NetworkSignalCore.Server.Rooms;

public sealed class RoomManager : IRoomManager
{
    private readonly ConcurrentDictionary<string, NetworkRoom> _rooms = new();
    private readonly ConcurrentDictionary<string, string> _roomIdByConnectionId = new();
    private readonly IHubContext<NetworkHub, INetworkClient> _hubContext;
    private readonly ISessionManager _sessions;
    private readonly ILogger<RoomManager> _logger;
    private readonly object _lock = new();

    public RoomManager(
        IHubContext<NetworkHub, INetworkClient> hubContext,
        ISessionManager sessions,
        ILogger<RoomManager> logger)
    {
        _hubContext = hubContext;
        _sessions = sessions;
        _logger = logger;
    }

    public async Task<RoomOperationResult> CreateRoomAsync(string ownerConnectionId, CreateRoomRequest request)
    {
        if (!_sessions.IsAuthenticated(ownerConnectionId))
            return Fail("Not authenticated.");

        if (_roomIdByConnectionId.ContainsKey(ownerConnectionId))
            return Fail("Already in a room. Leave first.");

        var room = new NetworkRoom
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = request.Name,
            OwnerId = ownerConnectionId,
            MaxPlayers = request.MaxPlayers,
            IsPrivate = request.IsPrivate,
            Password = request.Password,
            GameMode = request.GameMode,
            Metadata = request.Metadata,
        };

        lock (_lock)
        {
            room.ConnectionIds.Add(ownerConnectionId);
            _rooms[room.Id] = room;
            _roomIdByConnectionId[ownerConnectionId] = room.Id;
        }

        var player = _sessions.GetPlayer(ownerConnectionId);
        if (player is not null)
            player.RoomId = room.Id;

        await _hubContext.Groups.AddToGroupAsync(ownerConnectionId, room.Id);
        await BroadcastRoomUpdateAsync(room);

        _logger.LogInformation("Room created: roomId={RoomId} owner={ConnectionId}", room.Id, ownerConnectionId);
        return new RoomOperationResult { Success = true, Message = room.Id };
    }

    public async Task<RoomOperationResult> JoinRoomAsync(string connectionId, JoinRoomRequest request)
    {
        if (!_sessions.IsAuthenticated(connectionId))
            return Fail("Not authenticated.");

        if (_roomIdByConnectionId.ContainsKey(connectionId))
            return Fail("Already in a room. Leave first.");

        if (!_rooms.TryGetValue(request.RoomId, out var room))
            return Fail("Room not found.");

        RoomOperationResult? lockError = null;
        lock (_lock)
        {
            if (room.State != RoomState.Lobby)
                lockError = Fail("Room is not accepting players.");
            else if (room.ConnectionIds.Count >= room.MaxPlayers)
                lockError = Fail("Room is full.");
            else if (room.Password is not null && room.Password != request.Password)
                lockError = Fail("Invalid password.");
            else
            {
                room.ConnectionIds.Add(connectionId);
                _roomIdByConnectionId[connectionId] = room.Id;
            }
        }

        if (lockError is not null) return lockError;

        var player = _sessions.GetPlayer(connectionId);
        if (player is not null)
            player.RoomId = room.Id;

        await _hubContext.Groups.AddToGroupAsync(connectionId, room.Id);
        await BroadcastRoomUpdateAsync(room);

        _logger.LogInformation("Player joined room: roomId={RoomId} connectionId={ConnectionId}", room.Id, connectionId);
        return new RoomOperationResult { Success = true, Message = room.Id };
    }

    public async Task<RoomOperationResult> LeaveRoomAsync(string connectionId)
    {
        if (!_roomIdByConnectionId.TryGetValue(connectionId, out var roomId))
            return Fail("Not in a room.");

        if (!_rooms.TryGetValue(roomId, out var room))
        {
            _roomIdByConnectionId.TryRemove(connectionId, out _);
            return Fail("Room not found.");
        }

        bool roomClosed = false;
        lock (_lock)
        {
            room.ConnectionIds.Remove(connectionId);
            _roomIdByConnectionId.TryRemove(connectionId, out _);

            var player = _sessions.GetPlayer(connectionId);
            if (player is not null)
                player.RoomId = null;

            if (room.ConnectionIds.Count == 0)
            {
                _rooms.TryRemove(room.Id, out _);
                roomClosed = true;
            }
            else if (room.OwnerId == connectionId)
            {
                room.OwnerId = room.ConnectionIds[0];
                _logger.LogInformation("Room owner transferred: roomId={RoomId} newOwner={NewOwner}", room.Id, room.OwnerId);
            }
        }

        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, roomId);

        if (!roomClosed)
            await BroadcastRoomUpdateAsync(room);

        _logger.LogInformation("Player left room: roomId={RoomId} connectionId={ConnectionId}", roomId, connectionId);
        return new RoomOperationResult { Success = true, Message = roomId };
    }

    public async Task<RoomOperationResult> KickPlayerAsync(string requesterConnectionId, KickPlayerRequest request)
    {
        if (!_roomIdByConnectionId.TryGetValue(requesterConnectionId, out var roomId))
            return Fail("Not in a room.");

        if (!_rooms.TryGetValue(roomId, out var room))
            return Fail("Room not found.");

        if (room.OwnerId != requesterConnectionId)
            return Fail("Only the room owner can kick players.");

        var targetConnId = _sessions.GetConnectionId(request.TargetPlayerId);
        if (targetConnId is null)
            return Fail("Target player not found.");

        if (!room.ConnectionIds.Contains(targetConnId))
            return Fail("Target player is not in this room.");

        await LeaveRoomAsync(targetConnId);

        await _hubContext.Clients.Client(targetConnId)
            .ReceiveError("KICKED", request.Reason);

        return new RoomOperationResult { Success = true, Message = "Player kicked." };
    }

    public async Task<RoomOperationResult> StartGameAsync(string requesterConnectionId)
    {
        if (!_roomIdByConnectionId.TryGetValue(requesterConnectionId, out var roomId))
            return Fail("Not in a room.");

        if (!_rooms.TryGetValue(roomId, out var room))
            return Fail("Room not found.");

        if (room.OwnerId != requesterConnectionId)
            return Fail("Only the room owner can start the game.");

        if (room.State != RoomState.Lobby)
            return Fail("Game is already started.");

        lock (_lock)
        {
            room.State = RoomState.Starting;
        }

        await BroadcastRoomUpdateAsync(room);

        _logger.LogInformation("Game starting: roomId={RoomId}", roomId);
        return new RoomOperationResult { Success = true, Message = "Game starting." };
    }

    public RoomInfo? GetRoom(string roomId)
        => _rooms.TryGetValue(roomId, out var r) ? r.ToInfo(_sessions) : null;

    public RoomInfo? GetRoomByConnectionId(string connectionId)
    {
        if (!_roomIdByConnectionId.TryGetValue(connectionId, out var roomId)) return null;
        return _rooms.TryGetValue(roomId, out var r) ? r.ToInfo(_sessions) : null;
    }

    public IReadOnlyList<RoomInfo> GetPublicRooms()
        => _rooms.Values
            .Where(r => !r.IsPrivate && r.State == RoomState.Lobby)
            .Select(r => r.ToInfo(_sessions))
            .ToList();

    private async Task BroadcastRoomUpdateAsync(NetworkRoom room)
    {
        var info = room.ToInfo(_sessions);
        await _hubContext.Clients.Group(room.Id).ReceiveRoomUpdate(info);
    }

    private static RoomOperationResult Fail(string message)
        => new() { Success = false, Message = message };
}
