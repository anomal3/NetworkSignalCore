using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Core.Abstractions;

public interface ISessionManager
{
    void          AddSession(string connectionId, PlayerInfo player);
    void          RemoveSession(string connectionId);
    PlayerInfo?   GetPlayer(string connectionId);
    PlayerInfo?   GetPlayerById(string playerId);
    string?       GetConnectionId(string playerId);
    IReadOnlyList<PlayerInfo> GetAllPlayers();
    bool          IsAuthenticated(string connectionId);
}
