using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Server.Auth;

public sealed class JwtAuthService : IAuthProvider
{
    private readonly NetworkAuthOptions _options;
    private readonly HashSet<string> _revokedTokens = new();
    private readonly object _lock = new();

    public JwtAuthService(IOptions<NetworkAuthOptions> options)
    {
        _options = options.Value;
    }

    public Task<AuthResult> AuthenticateAsync(string token, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_revokedTokens.Contains(token))
                return Task.FromResult(AuthResult.Fail("Token has been revoked."));
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));

            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey        = key,
                ValidateIssuer          = true,
                ValidIssuer             = _options.Issuer,
                ValidateAudience        = true,
                ValidAudience           = _options.Audience,
                ValidateLifetime        = true,
                ClockSkew               = TimeSpan.FromSeconds(30),
            }, out var validatedToken);

            var jwt    = (JwtSecurityToken)validatedToken;
            var player = ClaimsToPlayer(jwt.Claims);

            return Task.FromResult(AuthResult.Ok(player));
        }
        catch (SecurityTokenExpiredException)
        {
            return Task.FromResult(AuthResult.Fail("Token has expired."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(AuthResult.Fail($"Invalid token: {ex.Message}"));
        }
    }

    public Task<string> GenerateTokenAsync(PlayerInfo player, CancellationToken ct = default)
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,  player.PlayerId),
            new Claim(JwtRegisteredClaimNames.Name, player.DisplayName),
            new Claim("elo",  player.Elo.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
        };

        var jwt = new JwtSecurityToken(
            issuer:             _options.Issuer,
            audience:           _options.Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.Add(_options.TokenLifetime),
            signingCredentials: credentials);

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(jwt));
    }

    public Task RevokeTokenAsync(string token, CancellationToken ct = default)
    {
        lock (_lock)
            _revokedTokens.Add(token);
        return Task.CompletedTask;
    }

    private static PlayerInfo ClaimsToPlayer(IEnumerable<Claim> claims)
    {
        var dict = claims.ToDictionary(c => c.Type, c => c.Value);
        return new PlayerInfo
        {
            PlayerId    = dict.GetValueOrDefault(JwtRegisteredClaimNames.Sub, ""),
            DisplayName = dict.GetValueOrDefault(JwtRegisteredClaimNames.Name, ""),
            Elo         = int.TryParse(dict.GetValueOrDefault("elo", "0"), out var elo) ? elo : 0,
        };
    }
}

