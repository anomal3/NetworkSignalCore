using System.Collections.Concurrent;

namespace NetworkSignalCore.Server.AntiCheat;

public sealed class RateLimiter
{
    private sealed class WindowConfig
    {
        public int      MaxCalls { get; init; }
        public TimeSpan Window   { get; init; }
    }

    private readonly ConcurrentDictionary<string, WindowConfig> _configs = new();
    // key: connectionId:method → sorted list of call timestamps (ticks)
    private readonly ConcurrentDictionary<string, Queue<long>> _calls = new();

    public void Configure(string method, int maxCalls, TimeSpan window)
    {
        _configs[method] = new WindowConfig { MaxCalls = maxCalls, Window = window };
    }

    public bool TryConsume(string connectionId, string method)
    {
        if (!_configs.TryGetValue(method, out var cfg))
            return true; // no limit configured for this method

        var key   = $"{connectionId}:{method}";
        var queue = _calls.GetOrAdd(key, _ => new Queue<long>());
        var now   = DateTime.UtcNow.Ticks;
        var windowTicks = cfg.Window.Ticks;

        lock (queue)
        {
            // Evict old entries outside the window
            while (queue.Count > 0 && now - queue.Peek() > windowTicks)
                queue.Dequeue();

            if (queue.Count >= cfg.MaxCalls)
                return false;

            queue.Enqueue(now);
            return true;
        }
    }

    public void ClearConnection(string connectionId)
    {
        foreach (var key in _calls.Keys.Where(k => k.StartsWith(connectionId + ":")))
            _calls.TryRemove(key, out _);
    }
}
