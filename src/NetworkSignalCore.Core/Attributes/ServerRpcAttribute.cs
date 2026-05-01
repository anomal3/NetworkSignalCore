namespace NetworkSignalCore.Core.Attributes;

/// <summary>
/// Marks a method on a NetworkController as callable by clients.
/// The method will be dispatched by the framework upon client invocation.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ServerRpcAttribute : Attribute
{
    /// <summary>Override the method name exposed to clients.</summary>
    public string? MethodName { get; set; }

    /// <summary>Require caller to be authenticated.</summary>
    public bool RequireAuth { get; set; } = true;

    /// <summary>Require caller to be inside a room.</summary>
    public bool RequireRoom { get; set; }
}
