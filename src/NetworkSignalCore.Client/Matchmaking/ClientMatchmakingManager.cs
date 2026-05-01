using Microsoft.Extensions.Logging;
using NetworkSignalCore.Core.Messages;

namespace NetworkSignalCore.Client.Matchmaking;

public sealed class ClientMatchmakingManager
{
    private readonly ILogger<ClientMatchmakingManager> _logger;
    private readonly Func<string, object?[], Task> _invoke;

    public event Action<MatchFoundNotification>? OnMatchFound;

    public bool IsInQueue { get; private set; }

    public ClientMatchmakingManager(
        Func<string, object?[], Task> invoke,
        ILogger<ClientMatchmakingManager> logger)
    {
        _invoke = invoke;
        _logger = logger;
    }

    internal void HandleMatchFound(MatchFoundNotification notification)
    {
        IsInQueue = false;
        OnMatchFound?.Invoke(notification);
    }

    public async Task JoinQueueAsync(JoinQueueRequest request)
    {
        try
        {
            await _invoke("JoinMatchmakingQueue", new object?[] { request }).ConfigureAwait(false);
            IsInQueue = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JoinQueueAsync failed.");
        }
    }

    public async Task LeaveQueueAsync()
    {
        try
        {
            await _invoke("LeaveMatchmakingQueue", Array.Empty<object?>()).ConfigureAwait(false);
            IsInQueue = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LeaveQueueAsync failed.");
        }
    }

    public Task AcceptMatchAsync(string matchId)
        => _invoke("AcceptMatch", new object?[] { matchId });

    public Task DeclineMatchAsync(string matchId)
        => _invoke("DeclineMatch", new object?[] { matchId });
}
