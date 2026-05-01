namespace NetworkSignalCore.Core.Attributes;

/// <summary>
/// Marks a field or property on a NetworkController as automatically
/// synchronized to all clients whenever its value changes.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class SyncVarAttribute : Attribute
{
    /// <summary>Custom name used in sync packets. Defaults to member name.</summary>
    public string? FieldName { get; set; }

    /// <summary>Sync only to players in the same room.</summary>
    public bool RoomOnly { get; set; } = true;
}
