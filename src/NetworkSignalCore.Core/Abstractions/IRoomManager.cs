using NetworkSignalCore.Core.Messages;
using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Core.Abstractions;

public interface IRoomManager
{
    Task<RoomOperationResult> CreateRoomAsync(string ownerConnectionId, CreateRoomRequest request);
    Task<RoomOperationResult> JoinRoomAsync(string connectionId, JoinRoomRequest request);
    Task<RoomOperationResult> LeaveRoomAsync(string connectionId);
    Task<RoomOperationResult> KickPlayerAsync(string requesterConnectionId, KickPlayerRequest request);
    Task<RoomOperationResult> StartGameAsync(string requesterConnectionId);
    RoomInfo?                 GetRoom(string roomId);
    RoomInfo?                 GetRoomByConnectionId(string connectionId);
    IReadOnlyList<RoomInfo>   GetPublicRooms();
}
