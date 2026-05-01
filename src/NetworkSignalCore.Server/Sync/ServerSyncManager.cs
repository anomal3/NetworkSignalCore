using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Attributes;
using NetworkSignalCore.Core.Sync;
using NetworkSignalCore.Server.Hubs;
using NetworkSignalCore.Server.Rpc;

namespace NetworkSignalCore.Server.Sync;

public sealed class ServerSyncManager : IHostedService, IDisposable
{
    private readonly List<(NetworkController Controller, string NetworkId, SyncFieldInfo[] Fields)> _tracked = new();
    private readonly IHubContext<NetworkHub, INetworkClient> _hubContext;
    private readonly IRoomManager _rooms;
    private readonly INetworkSerializer _serializer;
    private readonly NetworkServerOptions _options;
    private readonly ILogger<ServerSyncManager> _logger;
    private Timer? _timer;

    public ServerSyncManager(
        IHubContext<NetworkHub, INetworkClient> hubContext,
        IRoomManager rooms,
        INetworkSerializer serializer,
        NetworkServerOptions options,
        ILogger<ServerSyncManager> logger)
    {
        _hubContext = hubContext;
        _rooms      = rooms;
        _serializer = serializer;
        _options    = options;
        _logger     = logger;
    }

    public void RegisterController(NetworkController controller)
        => Track(controller, controller.GetType().Name);

    public void Track(NetworkController controller, string networkId)
    {
        var fields = DiscoverSyncFields(controller);
        if (fields.Length > 0)
            _tracked.Add((controller, networkId, fields));
    }

    public Task StartAsync(CancellationToken ct)
    {
        _timer = new Timer(_ => _ = TickAsync(), null,
            TimeSpan.Zero, TimeSpan.FromMilliseconds(_options.SyncTickRateMs));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async Task TickAsync()
    {
        foreach (var (controller, networkId, fields) in _tracked)
        {
            foreach (var field in fields)
            {
                var syncVar = field.GetValue(controller);
                if (syncVar == null) continue;

                var isDirty = (bool)field.IsDirtyProp.GetValue(syncVar)!;
                if (!isDirty) continue;

                var innerValue = field.ValueProp.GetValue(syncVar);
                var json       = _serializer.Serialize(innerValue);

                field.ClearDirty.Invoke(syncVar, null);

                var room = _rooms.GetRoomByConnectionId(controller.InternalConnectionId);

                try
                {
                    if (field.Attr.RoomOnly && room != null)
                        await _hubContext.Clients.Group(room.RoomId).ReceiveSyncState(networkId, field.FieldName, json);
                    else
                        await _hubContext.Clients.All.ReceiveSyncState(networkId, field.FieldName, json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting SyncVar {Field}", field.FieldName);
                }
            }
        }
    }

    private static SyncFieldInfo[] DiscoverSyncFields(NetworkController controller)
    {
        var type   = controller.GetType();
        var result = new List<SyncFieldInfo>();

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var attr = field.GetCustomAttribute<SyncVarAttribute>();
            if (attr == null) continue;

            var fieldType = field.FieldType;
            if (!fieldType.IsGenericType || fieldType.GetGenericTypeDefinition() != typeof(SyncVar<>))
                continue;

            result.Add(new SyncFieldInfo
            {
                FieldName   = attr.FieldName ?? field.Name,
                Attr        = attr,
                GetValue    = c => field.GetValue(c),
                ValueProp   = fieldType.GetProperty("Value")!,
                IsDirtyProp = fieldType.GetProperty("IsDirty")!,
                ClearDirty  = fieldType.GetMethod("ClearDirty")!,
            });
        }

        return result.ToArray();
    }

    public void Dispose() => _timer?.Dispose();

    private sealed class SyncFieldInfo
    {
        public string         FieldName   { get; init; } = "";
        public SyncVarAttribute Attr      { get; init; } = null!;
        public Func<NetworkController, object?> GetValue { get; init; } = null!;
        public PropertyInfo   ValueProp   { get; init; } = null!;
        public PropertyInfo   IsDirtyProp { get; init; } = null!;
        public MethodInfo     ClearDirty  { get; init; } = null!;
    }
}
