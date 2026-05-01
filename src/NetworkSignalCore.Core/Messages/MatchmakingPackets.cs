namespace NetworkSignalCore.Core.Messages;

public sealed class JoinQueueRequest
{
    public string GameMode  { get; set; } = string.Empty;
    public string Region    { get; set; } = "any";
    public int    Elo       { get; set; }
    public int    PartySize { get; set; } = 1;
    public Dictionary<string, string> Filters { get; set; } = new();
}

public sealed class MatchFoundNotification
{
    public string   RoomId    { get; set; } = string.Empty;
    public string   RoomName  { get; set; } = string.Empty;
    public string[] PlayerIds { get; set; } = Array.Empty<string>();
    public int      AcceptTimeoutSeconds { get; set; } = 15;
}
