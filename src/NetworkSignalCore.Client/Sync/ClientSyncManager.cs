using Microsoft.Extensions.Logging;

namespace NetworkSignalCore.Client.Sync;

public sealed class ClientSyncManager
{
    private readonly ILogger<ClientSyncManager> _logger;
    private readonly Dictionary<string, ISyncStateReceiver> _receivers = new();
    private readonly object _lock = new();

    public ClientSyncManager(ILogger<ClientSyncManager> logger)
    {
        _logger = logger;
    }

    public void Register(string networkId, ISyncStateReceiver receiver)
    {
        lock (_lock)
            _receivers[networkId] = receiver;
    }

    public void Unregister(string networkId)
    {
        lock (_lock)
            _receivers.Remove(networkId);
    }

    public void ApplySync(string networkId, string field, string valueJson)
    {
        ISyncStateReceiver? receiver;
        lock (_lock)
            _receivers.TryGetValue(networkId, out receiver);

        if (receiver is null)
        {
            _logger.LogWarning("No sync receiver registered for networkId '{NetworkId}'.", networkId);
            return;
        }

        try
        {
            receiver.OnSyncStateReceived(field, valueJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching sync state for networkId '{NetworkId}', field '{Field}'.", networkId, field);
        }
    }
}
