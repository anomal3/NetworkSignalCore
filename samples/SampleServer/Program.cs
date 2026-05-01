using NetworkSignalCore.Core.Attributes;
using NetworkSignalCore.Core.Sync;
using NetworkSignalCore.Server.Extensions;
using NetworkSignalCore.Server.Rpc;
using SampleServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNetworkSignalCore(
    configureServer: opt =>
    {
        opt.MaxPlayersPerRoom       = 8;
        opt.RateLimitCallsPerSecond = 60;
    },
    configureAuth: opt =>
    {
        opt.SecretKey = "super-secret-key-at-least-32-chars!!";
        opt.Issuer    = "SampleServer";
        opt.Audience  = "SampleClients";
    });

builder.Services.AddNetworkController<GameController>();

var app = builder.Build();

app.UseNetworkSignalCore("/network");

// Utility endpoint: generate a test JWT token
app.MapGet("/token/{name}", async (string name, NetworkSignalCore.Core.Abstractions.IAuthProvider auth) =>
{
    var player = new NetworkSignalCore.Core.Models.PlayerInfo
    {
        PlayerId    = Guid.NewGuid().ToString("N"),
        DisplayName = name,
        Elo         = 1000,
    };
    var token = await auth.GenerateTokenAsync(player);
    return Results.Ok(new { token, playerId = player.PlayerId });
});

app.Run();
