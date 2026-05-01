using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Server.Hubs;

namespace NetworkSignalCore.Server.Party;

public sealed class PartyManager
{
    private readonly ConcurrentDictionary<string, NetworkParty> _parties = new();
    private readonly ConcurrentDictionary<string, string> _partyIdByConnectionId = new();
    private readonly IHubContext<NetworkHub, INetworkClient> _hubContext;
    private readonly ISessionManager _sessions;
    private readonly ILogger<PartyManager> _logger;
    private readonly object _lock = new();

    public PartyManager(
        IHubContext<NetworkHub, INetworkClient> hubContext,
        ISessionManager sessions,
        ILogger<PartyManager> logger)
    {
        _hubContext = hubContext;
        _sessions = sessions;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> CreatePartyAsync(string connectionId)
    {
        if (!_sessions.IsAuthenticated(connectionId))
            return (false, "Not authenticated.");

        if (_partyIdByConnectionId.ContainsKey(connectionId))
            return (false, "Already in a party. Leave first.");

        var party = new NetworkParty
        {
            PartyId = Guid.NewGuid().ToString("N"),
            LeaderConnectionId = connectionId,
            InviteCode = GenerateInviteCode(),
        };

        lock (_lock)
        {
            party.ConnectionIds.Add(connectionId);
            _parties[party.PartyId] = party;
            _partyIdByConnectionId[connectionId] = party.PartyId;
        }

        var player = _sessions.GetPlayer(connectionId);
        if (player is not null)
            player.PartyId = party.PartyId;

        await BroadcastPartyUpdateAsync(party);
        _logger.LogInformation("Party created: partyId={PartyId} leader={ConnectionId}", party.PartyId, connectionId);
        return (true, party.PartyId);
    }

    public async Task<(bool Success, string Message)> JoinPartyAsync(string connectionId, string inviteCode)
    {
        if (!_sessions.IsAuthenticated(connectionId))
            return (false, "Not authenticated.");

        if (_partyIdByConnectionId.ContainsKey(connectionId))
            return (false, "Already in a party. Leave first.");

        var party = _parties.Values.FirstOrDefault(p => p.InviteCode == inviteCode);
        if (party is null)
            return (false, "Party not found.");

        bool full = false;
        lock (_lock)
        {
            if (party.ConnectionIds.Count >= party.MaxSize)
                full = true;
            else
            {
                party.ConnectionIds.Add(connectionId);
                _partyIdByConnectionId[connectionId] = party.PartyId;
            }
        }

        if (full) return (false, "Party is full.");

        var player = _sessions.GetPlayer(connectionId);
        if (player is not null)
            player.PartyId = party.PartyId;

        await BroadcastPartyUpdateAsync(party);
        _logger.LogInformation("Player joined party: partyId={PartyId} connectionId={ConnectionId}", party.PartyId, connectionId);
        return (true, party.PartyId);
    }

    public async Task<(bool Success, string Message)> LeavePartyAsync(string connectionId)
    {
        if (!_partyIdByConnectionId.TryGetValue(connectionId, out var partyId))
            return (false, "Not in a party.");

        if (!_parties.TryGetValue(partyId, out var party))
        {
            _partyIdByConnectionId.TryRemove(connectionId, out _);
            return (false, "Party not found.");
        }

        bool partyClosed = false;
        lock (_lock)
        {
            party.ConnectionIds.Remove(connectionId);
            _partyIdByConnectionId.TryRemove(connectionId, out _);

            var player = _sessions.GetPlayer(connectionId);
            if (player is not null)
                player.PartyId = null;

            if (party.ConnectionIds.Count == 0)
            {
                _parties.TryRemove(partyId, out _);
                partyClosed = true;
            }
            else if (party.LeaderConnectionId == connectionId)
            {
                party.LeaderConnectionId = party.ConnectionIds[0];
            }
        }

        if (!partyClosed)
            await BroadcastPartyUpdateAsync(party);

        return (true, "Left party.");
    }

    private async Task BroadcastPartyUpdateAsync(NetworkParty party)
    {
        var info = party.ToInfo(_sessions.GetPlayer);
        var tasks = party.ConnectionIds.Select(connId =>
            _hubContext.Clients.Client(connId).ReceivePartyUpdate(info));
        await Task.WhenAll(tasks);
    }

    private static string GenerateInviteCode()
        => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
}
