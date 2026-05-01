namespace NetworkSignalCore.Core.Abstractions;

public interface INetworkSerializer
{
    string Serialize<T>(T value);
    T?     Deserialize<T>(string data);
    object? Deserialize(string data, Type type);
}
