using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NetworkSignalCore.Core.Abstractions;
using NetworkSignalCore.Core.Serialization;
using NetworkSignalCore.Server.AntiCheat;
using NetworkSignalCore.Server.Auth;
using NetworkSignalCore.Server.Hubs;
using NetworkSignalCore.Server.Matchmaking;
using NetworkSignalCore.Server.Rooms;
using NetworkSignalCore.Server.Rpc;
using NetworkSignalCore.Server.Session;
using NetworkSignalCore.Server.Sync;

namespace NetworkSignalCore.Server.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all NetworkSignalCore server services.
    /// Call UseNetworkSignalCore on IApplicationBuilder to map the hub.
    /// </summary>
    public static IServiceCollection AddNetworkSignalCore(
        this IServiceCollection services,
        Action<NetworkServerOptions>? configureServer = null,
        Action<NetworkAuthOptions>? configureAuth = null)
    {
        var serverOptions = new NetworkServerOptions();
        configureServer?.Invoke(serverOptions);
        services.AddSingleton(serverOptions);

        services.Configure<NetworkAuthOptions>(opt =>
        {
            var tmp = new NetworkAuthOptions();
            configureAuth?.Invoke(tmp);
            opt.SecretKey      = tmp.SecretKey;
            opt.Issuer         = tmp.Issuer;
            opt.Audience       = tmp.Audience;
            opt.TokenLifetime  = tmp.TokenLifetime;
        });

        services.AddSignalR();

        services.AddSingleton<INetworkSerializer, JsonNetworkSerializer>();
        services.AddSingleton<ISessionManager,   SessionManager>();
        services.AddSingleton<IRoomManager,      RoomManager>();
        services.AddSingleton<IMatchmaker,       EloMatchmaker>();
        services.AddSingleton<IAuthProvider,     JwtAuthService>();

        services.AddSingleton<RateLimiter>();
        services.AddSingleton<AntiCheatService>();

        services.AddSingleton<IRpcDispatcher,    RpcDispatcher>();

        services.AddSingleton<MatchmakingManager>();
        services.AddHostedService(sp => sp.GetRequiredService<MatchmakingManager>());

        services.AddSingleton<ServerSyncManager>();
        services.AddHostedService(sp => sp.GetRequiredService<ServerSyncManager>());

        return services;
    }

    /// <summary>
    /// Registers a NetworkController and makes its [ServerRpc] methods available.
    /// Must be called after AddNetworkSignalCore.
    /// </summary>
    public static IServiceCollection AddNetworkController<T>(
        this IServiceCollection services)
        where T : NetworkController
    {
        services.AddSingleton<T>();

        // Register after DI container is built via IStartupFilter or hosted service startup
        // The actual registration happens lazily on first use via the InitializerService
        services.AddSingleton<INetworkControllerRegistration>(sp =>
            new NetworkControllerRegistration<T>(sp));

        return services;
    }

    /// <summary>Maps NetworkHub at the specified path (default: /network).</summary>
    public static IApplicationBuilder UseNetworkSignalCore(
        this IApplicationBuilder app,
        string hubPath = "/network")
    {
        var sp = app.ApplicationServices;
        var registrations = sp.GetServices<INetworkControllerRegistration>();
        var dispatcher = sp.GetRequiredService<IRpcDispatcher>();
        var syncManager = sp.GetService<ServerSyncManager>();

        foreach (var reg in registrations)
            reg.Register(dispatcher, syncManager);

        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapHub<NetworkHub>(hubPath));

        return app;
    }
}

internal interface INetworkControllerRegistration
{
    void Register(IRpcDispatcher dispatcher, ServerSyncManager? syncManager);
}

internal sealed class NetworkControllerRegistration<T> : INetworkControllerRegistration
    where T : NetworkController
{
    private readonly IServiceProvider _sp;
    public NetworkControllerRegistration(IServiceProvider sp) => _sp = sp;

    public void Register(IRpcDispatcher dispatcher, ServerSyncManager? syncManager)
    {
        var controller = _sp.GetRequiredService<T>();
        dispatcher.RegisterController(controller);
        syncManager?.RegisterController(controller);
    }
}
