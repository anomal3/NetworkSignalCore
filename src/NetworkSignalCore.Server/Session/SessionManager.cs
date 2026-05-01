using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Models;

namespace NetworkSignalCore.Server.Session;

public sealed class SessionManager : ISessionManager
{
    private readonly ConcurrentDictionary<string, NetworkSession> _byConnectionId = new();
    private readonly ConcurrentDictionary<string, string> _connectionIdByPlayerId = new();
    private readonly ILogger<SessionManager> _logger;

    public SessionManager(ILogger<SessionManager> logger)
    {
        _logger = logger;
    }

    public void AddSession(string connectionId, PlayerInfo player)
    {
        var session = new NetworkSession
        {
            ConnectionId = connectionId,
            Player = player,
            ConnectedAt = DateTimeOffset.UtcNow,
            IsAuthenticated = true,
        };

        _byConnectionId[connectionId] = session;
        _connectionIdByPlayerId[player.PlayerId] = connectionId;

        _logger.LogInformation("Session added: connectionId={ConnectionId} playerId={PlayerId}", connectionId, player.PlayerId);
    }

    public void RemoveSession(string connectionId)
    {
        if (_byConnectionId.TryRemove(connectionId, out var session))
        {
            _connectionIdByPlayerId.TryRemove(session.Player.PlayerId, out _);
            _logger.LogInformation("Session removed: connectionId={ConnectionId} playerId={PlayerId}", connectionId, session.Player.PlayerId);
        }
    }

    public PlayerInfo? GetPlayer(string connectionId)
        => _byConnectionId.TryGetValue(connectionId, out var s) ? s.Player : null;

    public PlayerInfo? GetPlayerById(string playerId)
    {
        if (!_connectionIdByPlayerId.TryGetValue(playerId, out var connId)) return null;
        return _byConnectionId.TryGetValue(connId, out var s) ? s.Player : null;
    }

    public string? GetConnectionId(string playerId)
        => _connectionIdByPlayerId.TryGetValue(playerId, out var connId) ? connId : null;

    public IReadOnlyList<PlayerInfo> GetAllPlayers()
        => _byConnectionId.Values.Select(s => s.Player).ToList();

    public bool IsAuthenticated(string connectionId)
        => _byConnectionId.TryGetValue(connectionId, out var s) && s.IsAuthenticated;
}
