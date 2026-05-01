using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Server.Matchmaking;

public sealed class EloMatchmaker : IMatchmaker
{
    private const int BaseEloDelta = 400;

    public bool CanMatch(MatchRequest a, MatchRequest b)
    {
        if (a.GameMode != b.GameMode) return false;

        if (!RegionsCompatible(a.Region, b.Region)) return false;

        var eloDelta = ComputeEloDelta(a, b);
        if (Math.Abs(a.Elo - b.Elo) > eloDelta) return false;

        return true;
    }

    // FindMatchAsync is not used directly — MatchmakingManager drives the loop.
    // This stub satisfies the interface contract.
    public Task<MatchResult> FindMatchAsync(MatchRequest request, CancellationToken ct = default)
        => Task.FromResult(new MatchResult { Success = false, Message = "Use MatchmakingManager." });

    private static bool RegionsCompatible(string a, string b)
        => a == "any" || b == "any" || a == b;

    private static int ComputeEloDelta(MatchRequest a, MatchRequest b)
    {
        // Expand Elo tolerance based on how long each ticket has been waiting.
        var waitA = (DateTimeOffset.UtcNow - a.QueuedAt).TotalSeconds;
        var waitB = (DateTimeOffset.UtcNow - b.QueuedAt).TotalSeconds;
        var expansion = (int)(Math.Min(waitA, waitB) / 10) * 50;
        return BaseEloDelta + expansion;
    }
}
