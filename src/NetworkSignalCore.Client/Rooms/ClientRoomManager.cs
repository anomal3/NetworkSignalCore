using Microsoft.Extensions.Logging;
using NetworkSignalCore.Core.Messages;
using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Client.Rooms;

public sealed class ClientRoomManager
{
    private readonly ILogger<ClientRoomManager> _logger;
    private readonly Func<string, object?[], Task> _invoke;

    // Pending one-shot TCS for GetRoomListAsync callers.
    private TaskCompletionSource<RoomInfo[]>? _pendingRoomList;
    private readonly object _pendingLock = new();

    public event Action<RoomInfo>?   OnRoomUpdated;
    public event Action<RoomInfo[]>? OnRoomListReceived;

    public RoomInfo? CurrentRoom { get; private set; }

    public ClientRoomManager(
        Func<string, object?[], Task> invoke,
        ILogger<ClientRoomManager> logger)
    {
        _invoke  = invoke;
        _logger  = logger;
    }

    internal void HandleRoomUpdate(RoomInfo room)
    {
        CurrentRoom = room;
        OnRoomUpdated?.Invoke(room);
    }

    internal void HandleRoomList(RoomInfo[] rooms)
    {
        OnRoomListReceived?.Invoke(rooms);

        TaskCompletionSource<RoomInfo[]>? pending;
        lock (_pendingLock)
        {
            pending = _pendingRoomList;
            _pendingRoomList = null;
        }
        pending?.TrySetResult(rooms);
    }

    public Task CreateRoomAsync(CreateRoomRequest request)
        => _invoke("CreateRoom", new object?[] { request });

    public Task JoinRoomAsync(string roomId, string? password = null)
    {
        var request = new JoinRoomRequest { RoomId = roomId, Password = password };
        return _invoke("JoinRoom", new object?[] { request });
    }

    public Task LeaveRoomAsync()
        => _invoke("LeaveRoom", Array.Empty<object?>());

    public Task<RoomInfo[]> GetRoomListAsync()
    {
        TaskCompletionSource<RoomInfo[]> tcs;
        lock (_pendingLock)
        {
            tcs = new TaskCompletionSource<RoomInfo[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRoomList = tcs;
        }

        return _invoke("GetRoomList", Array.Empty<object?>())
            .ContinueWith(_ => tcs.Task, TaskContinuationOptions.ExecuteSynchronously)
            .Unwrap();
    }

    public Task KickPlayerAsync(string targetPlayerId, string reason = "")
    {
        var request = new KickPlayerRequest { TargetPlayerId = targetPlayerId, Reason = reason };
        return _invoke("KickPlayer", new object?[] { request });
    }

    public Task StartGameAsync()
        => _invoke("StartGame", Array.Empty<object?>());
}
