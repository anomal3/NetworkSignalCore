using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Server.Party;

public sealed class NetworkParty
{
    public string PartyId { get; set; } = string.Empty;
    public string LeaderConnectionId { get; set; } = string.Empty;
    public List<string> ConnectionIds { get; } = new();
    public int MaxSize { get; set; } = 5;
    public string? InviteCode { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();

    public PartyInfo ToInfo(Func<string, PlayerInfo?> getPlayer)
    {
        var members = ConnectionIds
            .Select(getPlayer)
            .Where(p => p is not null)
            .Cast<PlayerInfo>()
            .ToArray();

        var leader = getPlayer(LeaderConnectionId);

        return new PartyInfo
        {
            PartyId = PartyId,
            LeaderId = leader?.PlayerId ?? string.Empty,
            Members = members,
            MaxSize = MaxSize,
            InviteCode = InviteCode,
            Metadata = Metadata,
        };
    }
}
