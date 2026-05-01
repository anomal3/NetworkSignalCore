using System.Reflection;
using Microsoft.Extensions.Logging;
using NetworkSignalCore.Client;
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Attributes;
using NetworkSignalCore.Core.Serialization;

namespace NetworkSignalCore.Client.Rpc;

public abstract class NetworkBehaviourBase
{
    // Cached per concrete type: rpc name -> (MethodInfo, first-parameter type or null).
    private static readonly Dictionary<Type, Dictionary<string, (MethodInfo Method, Type? ParamType)>> MethodCache
        = new();
    private static readonly object CacheLock = new();

    private readonly INetworkSerializer _serializer;
    private readonly ILogger? _logger;
    private Func<string, string, object?, Task>? _serverRpcInvoker;

    protected NetworkBehaviourBase(NetworkClient client, ILogger? logger = null)
        : this(client, new JsonNetworkSerializer(), logger) { }

    // Allows tests or advanced users to inject a custom serializer.
    protected NetworkBehaviourBase(NetworkClient client, INetworkSerializer serializer, ILogger? logger = null)
    {
        _serializer = serializer;
        _logger     = logger;
        EnsureMethodsCached();
        client.RegisterBehaviour(ControllerName, this);
    }

    protected virtual string ControllerName => GetType().Name;

    // Called by NetworkClient during RegisterBehaviour.
    internal void Init(Func<string, string, object?, Task> serverRpcInvoker)
        => _serverRpcInvoker = serverRpcInvoker;

    protected Task ServerRpcAsync(string method, object? payload = null)
    {
        if (_serverRpcInvoker is null)
            throw new InvalidOperationException("NetworkBehaviourBase has not been registered with a NetworkClient.");
        return _serverRpcInvoker(ControllerName, method, payload);
    }

    protected virtual void OnConnected() { }
    protected virtual void OnDisconnected() { }

    internal void NotifyConnected()    => OnConnected();
    internal void NotifyDisconnected() => OnDisconnected();

    internal void DispatchClientRpc(string method, string payloadJson)
    {
        var map = GetMethodMap(GetType());

        if (!map.TryGetValue(method, out var entry))
        {
            _logger?.LogWarning("No [ClientRpc] method '{Method}' on '{Type}'.", method, GetType().Name);
            return;
        }

        try
        {
            if (entry.ParamType is null)
            {
                entry.Method.Invoke(this, null);
            }
            else
            {
                var arg = _serializer.Deserialize(payloadJson, entry.ParamType);
                entry.Method.Invoke(this, new[] { arg });
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error invoking [ClientRpc] '{Method}' on '{Type}'.", method, GetType().Name);
        }
    }

    private void EnsureMethodsCached()
    {
        var type = GetType();
        lock (CacheLock)
        {
            if (!MethodCache.ContainsKey(type))
                BuildMethodMap(type);
        }
    }

    private static void BuildMethodMap(Type type)
    {
        var map = new Dictionary<string, (MethodInfo, Type?)>(StringComparer.Ordinal);

        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var attr = method.GetCustomAttribute<ClientRpcAttribute>();
            if (attr is null) continue;

            var key        = attr.MethodName ?? method.Name;
            var parameters = method.GetParameters();
            var paramType  = parameters.Length > 0 ? parameters[0].ParameterType : (Type?)null;

            map[key] = (method, paramType);
        }

        MethodCache[type] = map;
    }

    private static Dictionary<string, (MethodInfo Method, Type? ParamType)> GetMethodMap(Type type)
    {
        lock (CacheLock)
        {
            if (!MethodCache.TryGetValue(type, out var map))
            {
                BuildMethodMap(type);
                map = MethodCache[type];
            }
            return map;
        }
    }
}
