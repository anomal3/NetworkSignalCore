namespace NetworkSignalCore.Core.Attributes;

/// <summary>
/// Marks a method on a NetworkBehaviourBase as a receiver for server-sent RPCs.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ClientRpcAttribute : Attribute
{
    /// <summary>Override the method name the server broadcasts.</summary>
    public string? MethodName { get; set; }
}
