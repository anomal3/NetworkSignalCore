using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Messages;
using NetworkSignalCore.Core.Models;
using NetworkSignalCore.Server.Hubs;

namespace NetworkSignalCore.Server.Matchmaking;

public sealed class MatchmakingManager : IHostedService
{
    private readonly ConcurrentDictionary<string, MatchmakingTicket> _ticketByConnectionId = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _acceptedByMatchId = new();
    private readonly ConcurrentDictionary<string, List<string>> _pendingMatchPlayers = new();
    private readonly IMatchmaker _matchmaker;
    private readonly IRoomManager _roomManager;
    private readonly ISessionManager _sessions;
    private readonly IHubContext<NetworkHub, INetworkClient> _hubContext;
    private readonly ILogger<MatchmakingManager> _logger;
    private readonly NetworkServerOptions _options;
    private Timer? _timer;

    public MatchmakingManager(
        IMatchmaker matchmaker,
        IRoomManager roomManager,
        ISessionManager sessions,
        IHubContext<NetworkHub, INetworkClient> hubContext,
        ILogger<MatchmakingManager> logger,
        NetworkServerOptions options)
    {
        _matchmaker = matchmaker;
        _roomManager = roomManager;
        _sessions = sessions;
        _hubContext = hubContext;
        _logger = logger;
        _options = options;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(RunMatchmakingCycle, null,
            TimeSpan.FromMilliseconds(_options.MatchmakingIntervalMs),
            TimeSpan.FromMilliseconds(_options.MatchmakingIntervalMs));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    public void EnqueueAsync(string connectionId, JoinQueueRequest request)
    {
        if (!_sessions.IsAuthenticated(connectionId)) return;

        var player = _sessions.GetPlayer(connectionId);
        if (player is null) return;

        var ticket = new MatchmakingTicket
        {
            Request = new MatchRequest
            {
                PlayerId = player.PlayerId,
                GameMode = request.GameMode,
                Region = request.Region,
                Elo = request.Elo,
                PartySize = request.PartySize,
                Filters = request.Filters,
                QueuedAt = DateTimeOffset.UtcNow,
            },
            ConnectionId = connectionId,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
        };

        _ticketByConnectionId[connectionId] = ticket;
        _logger.LogInformation("Player queued: connectionId={ConnectionId} gameMode={GameMode}", connectionId, request.GameMode);
    }

    public void DequeueAsync(string connectionId)
    {
        _ticketByConnectionId.TryRemove(connectionId, out _);
        _logger.LogInformation("Player left queue: connectionId={ConnectionId}", connectionId);
    }

    public async Task AcceptMatchAsync(string connectionId, string matchId)
    {
        if (!_pendingMatchPlayers.TryGetValue(matchId, out var players)) return;

        var accepted = _acceptedByMatchId.GetOrAdd(matchId, _ => new HashSet<string>());
        lock (accepted)
        {
            accepted.Add(connectionId);
        }

        bool allAccepted;
        lock (accepted)
        {
            allAccepted = players.All(connId => accepted.Contains(connId));
        }

        if (allAccepted)
        {
            await FinalizeMatchAsync(matchId, players);
        }
    }

    public async Task DeclineMatchAsync(string connectionId, string matchId)
    {
        if (!_pendingMatchPlayers.TryGetValue(matchId, out var players)) return;

        _pendingMatchPlayers.TryRemove(matchId, out _);
        _acceptedByMatchId.TryRemove(matchId, out _);

        // Re-queue players who didn't decline
        foreach (var connId in players.Where(c => c != connectionId))
        {
            if (_ticketByConnectionId.TryGetValue(connId, out var ticket))
            {
                ticket.PendingMatchId = null;
                await _hubContext.Clients.Client(connId)
                    .ReceiveError("MATCH_DECLINED", "A player declined the match. Re-queuing.");
            }
        }

        _ticketByConnectionId.TryRemove(connectionId, out _);
    }

    private void RunMatchmakingCycle(object? state)
    {
        var available = _ticketByConnectionId.Values
            .Where(t => t.PendingMatchId is null && t.ExpiresAt > DateTimeOffset.UtcNow)
            .ToList();

        var matched = new HashSet<string>();

        for (int i = 0; i < available.Count; i++)
        {
            var a = available[i];
            if (matched.Contains(a.ConnectionId)) continue;

            for (int j = i + 1; j < available.Count; j++)
            {
                var b = available[j];
                if (matched.Contains(b.ConnectionId)) continue;

                if (_matchmaker.CanMatch(a.Request, b.Request))
                {
                    matched.Add(a.ConnectionId);
                    matched.Add(b.ConnectionId);

                    _ = NotifyMatchFoundAsync(new[] { a, b });
                    break;
                }
            }
        }
    }

    private async Task NotifyMatchFoundAsync(MatchmakingTicket[] tickets)
    {
        var matchId = Guid.NewGuid().ToString("N");
        var connectionIds = tickets.Select(t => t.ConnectionId).ToList();
        var playerIds = tickets.Select(t => t.PlayerId).ToArray();

        _pendingMatchPlayers[matchId] = connectionIds;
        _acceptedByMatchId[matchId] = new HashSet<string>();

        foreach (var ticket in tickets)
        {
            ticket.PendingMatchId = matchId;
        }

        var notification = new MatchFoundNotification
        {
            RoomId = matchId,
            RoomName = $"Match-{matchId[..6]}",
            PlayerIds = playerIds,
            AcceptTimeoutSeconds = (int)_options.MatchAcceptTimeout.TotalSeconds,
        };

        var tasks = connectionIds.Select(connId =>
            _hubContext.Clients.Client(connId).ReceiveMatchFound(notification));
        await Task.WhenAll(tasks);

        // Auto-cancel match after timeout if not all accepted
        _ = Task.Delay(_options.MatchAcceptTimeout).ContinueWith(async completedTask =>
        {
            if (_pendingMatchPlayers.ContainsKey(matchId))
            {
                _pendingMatchPlayers.TryRemove(matchId, out var _players);
                _acceptedByMatchId.TryRemove(matchId, out var _accepted);

                foreach (var ticket in tickets)
                {
                    ticket.PendingMatchId = null;
                    await _hubContext.Clients.Client(ticket.ConnectionId)
                        .ReceiveError("MATCH_TIMEOUT", "Match acceptance timed out. Re-queuing.");
                }
            }
        });
    }

    private async Task FinalizeMatchAsync(string matchId, List<string> connectionIds)
    {
        _pendingMatchPlayers.TryRemove(matchId, out _);
        _acceptedByMatchId.TryRemove(matchId, out _);

        var firstPlayer = connectionIds.FirstOrDefault();
        if (firstPlayer is null) return;

        var createRequest = new CreateRoomRequest
        {
            Name = $"Match-{matchId[..6]}",
            MaxPlayers = connectionIds.Count,
            IsPrivate = true,
        };

        var result = await _roomManager.CreateRoomAsync(firstPlayer, createRequest);
        if (!result.Success)
        {
            _logger.LogWarning("Failed to create match room: {Message}", result.Message);
            return;
        }

        var roomId = result.Message;

        foreach (var connId in connectionIds.Skip(1))
        {
            await _roomManager.JoinRoomAsync(connId, new JoinRoomRequest { RoomId = roomId });
            _ticketByConnectionId.TryRemove(connId, out _);
        }

        _ticketByConnectionId.TryRemove(firstPlayer, out _);

        _logger.LogInformation("Match finalized: matchId={MatchId} roomId={RoomId}", matchId, roomId);
    }
}
