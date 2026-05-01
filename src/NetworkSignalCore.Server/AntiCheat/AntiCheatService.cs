using Microsoft.Extensions.Logging;
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Server.AntiCheat;

public sealed class AntiCheatService
{
    private readonly List<IAntiCheatValidator> _validators = new();
    private readonly RateLimiter _rateLimiter;
    private readonly NetworkServerOptions _options;
    private readonly ILogger<AntiCheatService> _logger;

    public AntiCheatService(
        RateLimiter rateLimiter,
        NetworkServerOptions options,
        ILogger<AntiCheatService> logger)
    {
        _rateLimiter = rateLimiter;
        _options     = options;
        _logger      = logger;

        // Default global rate limit for all methods
        _rateLimiter.Configure("*", _options.RateLimitCallsPerSecond, TimeSpan.FromSeconds(1));
    }

    public void AddValidator(IAntiCheatValidator validator)
        => _validators.Add(validator);

    public ValidationResult Validate(string connectionId, PlayerInfo? player, string method, object? payload)
    {
        if (!_rateLimiter.TryConsume(connectionId, method))
        {
            _logger.LogWarning("Rate limit exceeded: {ConnectionId} → {Method}", connectionId, method);
            return ValidationResult.Fail("Rate limit exceeded.");
        }

        if (player == null) return ValidationResult.Ok();

        foreach (var validator in _validators)
        {
            var result = validator.Validate(connectionId, player, method, payload);
            if (!result.IsValid)
                return result;
        }

        return ValidationResult.Ok();
    }
}
