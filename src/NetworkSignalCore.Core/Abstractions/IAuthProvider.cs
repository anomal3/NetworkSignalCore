using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Core.Abstractions;

public interface IAuthProvider
{
    Task<AuthResult> AuthenticateAsync(string token, CancellationToken ct = default);
    Task<string>     GenerateTokenAsync(PlayerInfo player, CancellationToken ct = default);
    Task             RevokeTokenAsync(string token, CancellationToken ct = default);
}

public sealed class AuthResult
{
    public bool        Success { get; set; }
    public PlayerInfo? Player  { get; set; }
    public string      Message { get; set; } = string.Empty;

    public static AuthResult Ok(PlayerInfo player)
        => new() { Success = true, Player = player };

    public static AuthResult Fail(string message)
        => new() { Success = false, Message = message };
}
