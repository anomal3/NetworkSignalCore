using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Server.Matchmaking;

public sealed class MatchmakingTicket
{
    public string PlayerId => Request.PlayerId;
    public MatchRequest Request { get; set; } = new();
    public string ConnectionId { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? PendingMatchId { get; set; }
}
