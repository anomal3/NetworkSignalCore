using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Core.Abstractions;

public interface IMatchmaker
{
    Task<MatchResult> FindMatchAsync(MatchRequest request, CancellationToken ct = default);
    bool CanMatch(MatchRequest a, MatchRequest b);
}
