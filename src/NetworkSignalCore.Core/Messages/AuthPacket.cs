namespace NetworkSignalCore.Core.Messages;

public sealed class AuthRequest
{
    public string Token { get; set; } = string.Empty;
}

public sealed class AuthResponse
{
    public bool   Success  { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public string Message  { get; set; } = string.Empty;
}
