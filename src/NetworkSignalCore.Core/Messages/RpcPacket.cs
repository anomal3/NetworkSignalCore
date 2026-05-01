namespace NetworkSignalCore.Core.Messages;

public sealed class RpcPacket
{
    public string Controller { get; set; } = string.Empty;
    public string Method     { get; set; } = string.Empty;
    public string Payload    { get; set; } = string.Empty;
}
