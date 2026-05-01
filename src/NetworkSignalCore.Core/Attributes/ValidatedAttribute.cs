using NetworkSignalCore.Core.Abstractions;

namespace NetworkSignalCore.Core.Attributes;

/// <summary>
/// Attaches a validator to a [ServerRpc] method.
/// The validator runs before the method body and can reject the call.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ValidatedAttribute : Attribute
{
    public Type ValidatorType { get; }

    public ValidatedAttribute(Type validatorType)
    {
        if (!typeof(IAntiCheatValidator).IsAssignableFrom(validatorType))
            throw new ArgumentException(
                $"{validatorType.Name} must implement IAntiCheatValidator",
                nameof(validatorType));

        ValidatorType = validatorType;
    }
}
