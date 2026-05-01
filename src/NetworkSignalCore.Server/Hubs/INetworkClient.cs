using NetworkSignalCore.Core.Messages;
using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Server.Hubs;

public interface INetworkClient
{
    Task ReceiveClientRpc(string controller, string method, string payloadJson);
    Task ReceiveSyncState(string networkId, string field, string valueJson);
    Task ReceiveRoomUpdate(RoomInfo room);
    Task ReceiveRoomList(RoomInfo[] rooms);
    Task ReceiveMatchFound(MatchFoundNotification notification);
    Task ReceiveError(string code, string message);
    Task ReceiveAuthResult(bool success, string playerId, string message);
    Task ReceivePartyUpdate(PartyInfo party);
    Task ReceivePong(long timestamp);
}
