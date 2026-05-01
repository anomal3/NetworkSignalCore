using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Server.Rooms;

public sealed class NetworkRoom
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public bool IsPrivate { get; set; }
    public string? Password { get; set; }
    public string? GameMode { get; set; }
    public RoomState State { get; set; } = RoomState.Lobby;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<string> ConnectionIds { get; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public RoomInfo ToInfo(ISessionManager sessions)
    {
        var players = ConnectionIds
            .Select(sessions.GetPlayer)
            .Where(p => p is not null)
            .Cast<PlayerInfo>()
            .ToArray();

        return new RoomInfo
        {
            RoomId = Id,
            Name = Name,
            MaxPlayers = MaxPlayers,
            PlayerCount = ConnectionIds.Count,
            IsPrivate = IsPrivate,
            HasPassword = Password is not null,
            GameMode = GameMode,
            State = State,
            OwnerId = sessions.GetPlayer(OwnerId)?.PlayerId ?? OwnerId,
            Players = players,
            Metadata = Metadata,
            CreatedAt = CreatedAt,
        };
    }
}
