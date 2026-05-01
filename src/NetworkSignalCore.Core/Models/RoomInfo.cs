namespace NetworkSignalCore.Core.Models;

public sealed class RoomInfo
{
    public string       RoomId      { get; set; } = string.Empty;
    public string       Name        { get; set; } = string.Empty;
    public int          MaxPlayers  { get; set; }
    public int          PlayerCount { get; set; }
    public bool         IsPrivate   { get; set; }
    public bool         HasPassword { get; set; }
    public string?      GameMode    { get; set; }
    public RoomState    State       { get; set; } = RoomState.Lobby;
    public string       OwnerId     { get; set; } = string.Empty;
    public PlayerInfo[] Players     { get; set; } = Array.Empty<PlayerInfo>();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum RoomState
{
    Lobby,
    Starting,
    InGame,
    Finished,
}
