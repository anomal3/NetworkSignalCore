using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Core.Abstractions;

public interface IAntiCheatValidator
{
    ValidationResult Validate(string connectionId, PlayerInfo player, string method, object? payload);
}

public sealed class ValidationResult
{
    public bool   IsValid { get; set; }
    public string Reason  { get; set; } = string.Empty;

    public static ValidationResult Ok()
        => new() { IsValid = true };

    public static ValidationResult Fail(string reason)
        => new() { IsValid = false, Reason = reason };
}
