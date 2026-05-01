namespace NetworkSignalCore.Core.Models;

public sealed class PlayerInfo
{
    public string PlayerId     { get; set; } = string.Empty;
    public string DisplayName  { get; set; } = string.Empty;
    public string? RoomId      { get; set; }
    public string? PartyId     { get; set; }
    public int     Elo         { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTimeOffset ConnectedAt { get; set; } = DateTimeOffset.UtcNow;
}
