namespace NetworkSignalCore.Client.Connection;

public sealed class NetworkClientOptions
{
    public string   ServerUrl            { get; set; } = "http://localhost:5000/network";
    public string?  AuthToken            { get; set; }
    public TimeSpan ReconnectDelay       { get; set; } = TimeSpan.FromSeconds(3);
    public int      MaxReconnectAttempts { get; set; } = 5;
    public bool     AutoReconnect        { get; set; } = true;
    public bool     EnableLogging        { get; set; } = true;
}
