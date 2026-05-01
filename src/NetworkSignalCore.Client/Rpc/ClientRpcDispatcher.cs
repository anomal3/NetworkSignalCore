using Microsoft.Extensions.Logging;

namespace NetworkSignalCore.Client.Rpc;

public sealed class ClientRpcDispatcher
{
    private readonly ILogger<ClientRpcDispatcher> _logger;
    private readonly Dictionary<string, NetworkBehaviourBase> _handlers = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public ClientRpcDispatcher(ILogger<ClientRpcDispatcher> logger)
    {
        _logger = logger;
    }

    public void Register(string controllerName, NetworkBehaviourBase handler)
    {
        lock (_lock)
            _handlers[controllerName] = handler;
    }

    public void Unregister(string controllerName)
    {
        lock (_lock)
            _handlers.Remove(controllerName);
    }

    public void Dispatch(string controller, string method, string payloadJson)
    {
        NetworkBehaviourBase? handler;
        lock (_lock)
            _handlers.TryGetValue(controller, out handler);

        if (handler is null)
        {
            _logger.LogWarning("No handler registered for controller '{Controller}'.", controller);
            return;
        }

        handler.DispatchClientRpc(method, payloadJson);
    }
}
