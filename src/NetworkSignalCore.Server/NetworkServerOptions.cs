namespace NetworkSignalCore.Server;

public sealed class NetworkServerOptions
{
    public int      MaxPlayersPerRoom       { get; set; } = 16;
    public int      MatchmakingIntervalMs   { get; set; } = 2000;
    public int      SyncTickRateMs          { get; set; } = 50;
    public int      RateLimitCallsPerSecond { get; set; } = 30;
    public bool     RequireAuthForAllRpcs   { get; set; } = true;
    public TimeSpan MatchAcceptTimeout      { get; set; } = TimeSpan.FromSeconds(15);
    public int      MaxRooms               { get; set; } = 100;
    public int      EloDifferenceThreshold { get; set; } = 400;
}
