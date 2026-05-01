using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Server.Session;

public sealed class NetworkSession
{
    public string ConnectionId { get; set; } = string.Empty;
    public PlayerInfo Player { get; set; } = new();
    public DateTimeOffset ConnectedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsAuthenticated { get; set; }
}
