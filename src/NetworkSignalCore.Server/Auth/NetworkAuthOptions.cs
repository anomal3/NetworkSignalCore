namespace NetworkSignalCore.Server.Auth;

public sealed class NetworkAuthOptions
{
    public string SecretKey      { get; set; } = "change-me-in-production-min-32-chars!!";
    public string Issuer         { get; set; } = "NetworkSignalCore";
    public string Audience       { get; set; } = "NetworkSignalCore.Clients";
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(24);
}
