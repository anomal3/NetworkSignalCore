namespace NetworkSignalCore.Core.Messages;

public sealed class SyncPacket
{
    public string NetworkId { get; set; } = string.Empty;
    public string Field     { get; set; } = string.Empty;
    public string Value     { get; set; } = string.Empty;
}
