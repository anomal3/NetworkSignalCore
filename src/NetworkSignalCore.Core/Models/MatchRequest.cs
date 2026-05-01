namespace NetworkSignalCore.Core.Models;

public sealed class MatchRequest
{
    public string   PlayerId    { get; set; } = string.Empty;
    public string   GameMode    { get; set; } = string.Empty;
    public string   Region      { get; set; } = "any";
    public int      Elo         { get; set; }
    public int      PartySize   { get; set; } = 1;
    public string[] PartyMemberIds { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> Filters { get; set; } = new();
    public DateTimeOffset QueuedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class MatchResult
{
    public bool     Success   { get; set; }
    public string   RoomId    { get; set; } = string.Empty;
    public string[] PlayerIds { get; set; } = Array.Empty<string>();
    public string   Message   { get; set; } = string.Empty;
}
