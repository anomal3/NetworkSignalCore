using System.Text.Json;
using System.Text.Json.Serialization;
using NetworkSignalCore.Core.Abstractions;

namespace NetworkSignalCore.Core.Serialization;

public sealed class JsonNetworkSerializer : INetworkSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling              = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
    };

    public string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, Options);

    public T? Deserialize<T>(string data)
        => JsonSerializer.Deserialize<T>(data, Options);

    public object? Deserialize(string data, Type type)
        => JsonSerializer.Deserialize(data, type, Options);
}
