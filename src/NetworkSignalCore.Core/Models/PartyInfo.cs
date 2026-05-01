namespace NetworkSignalCore.Core.Models;

public sealed class PartyInfo
{
    public string       PartyId   { get; set; } = string.Empty;
    public string       LeaderId  { get; set; } = string.Empty;
    public PlayerInfo[] Members   { get; set; } = Array.Empty<PlayerInfo>();
    public int          MaxSize   { get; set; } = 5;
    public string?      InviteCode { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
