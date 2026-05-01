namespace NetworkSignalCore.Core.Sync;

/// <summary>
/// A wrapper that tracks dirty state for automatic network synchronization.
/// Assign a value via <see cref="Value"/> — the framework polls <see cref="IsDirty"/>
/// each server tick and broadcasts changes to clients.
/// </summary>
public sealed class SyncVar<T>
{
    private T _value;
    private bool _dirty;

    public SyncVar(T initial)
    {
        _value = initial;
    }

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;
            _value = value;
            _dirty = true;
        }
    }

    public bool IsDirty => _dirty;

    /// <summary>Called by the framework after the value has been broadcast.</summary>
    public void ClearDirty() => _dirty = false;

    public static implicit operator T(SyncVar<T> sv) => sv._value;

    public override string ToString() => _value?.ToString() ?? "null";
}
