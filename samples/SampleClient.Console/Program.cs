using System.Net.Http.Json;
using NetworkSignalCore.Client;
using NetworkSignalCore.Client.Connection;
using NetworkSignalCore.Client.Rpc;
using NetworkSignalCore.Core.Attributes;
using NetworkSignalCore.Core.Messages;

// ─── Connect ─────────────────────────────────────────────────────────────────

var client = new NetworkClient(opt =>
{
    opt.ServerUrl    = "http://localhost:5000/network";
    opt.AutoReconnect = true;
});

client.OnError += (code, msg) => Console.WriteLine($"[Error] {code}: {msg}");

await client.ConnectAsync();
Console.WriteLine("Connected.");

// ─── Auth ────────────────────────────────────────────────────────────────────

// In production, get a real token from your auth server.
// The sample server exposes GET /token/{name} for testing.
using var http  = new System.Net.Http.HttpClient();
var tokenResp   = await http.GetFromJsonAsync<TokenResponse>("http://localhost:5000/token/Player1");
var token       = tokenResp?.Token ?? throw new Exception("Could not get token");

var authenticated = await client.AuthenticateAsync(token);
Console.WriteLine($"Authenticated: {authenticated}, PlayerId: {client.LocalPlayer?.PlayerId}");

// ─── Register game behaviour ──────────────────────────────────────────────────

var game = new GameBehaviour(client);

// ─── Create / join a room ─────────────────────────────────────────────────────

client.Rooms.OnRoomUpdated += room =>
    Console.WriteLine($"Room updated: {room.Name} ({room.PlayerCount}/{room.MaxPlayers}) state={room.State}");

await client.Rooms.CreateRoomAsync(new CreateRoomRequest
{
    Name       = "TestRoom",
    MaxPlayers = 4,
    IsPrivate  = false,
    GameMode   = "Deathmatch",
});

// ─── Send game RPCs ───────────────────────────────────────────────────────────

Console.WriteLine("Press Enter to send a Move RPC, Q to quit.");
while (true)
{
    var line = Console.ReadLine();
    if (line?.ToUpperInvariant() == "Q") break;

    await game.MoveAsync(UnityEngine.Vector3.zero);
    Console.WriteLine("Sent Move RPC.");
}

client.Dispose();

// ─── Behaviour ───────────────────────────────────────────────────────────────

sealed class GameBehaviour : NetworkBehaviourBase
{
    public GameBehaviour(NetworkClient client) : base(client) { }

    // Called by the server when any player moves
    [ClientRpc]
    private void OnPlayerMoved(PlayerMovedData data)
        => Console.WriteLine($"[RPC] Player {data.PlayerId} moved to ({data.X}, {data.Y})");

    [ClientRpc]
    private void OnChat(ChatData data)
        => Console.WriteLine($"[Chat] {data.Name}: {data.Text}");

    public Task MoveAsync(UnityEngine.Vector3 pos)
        => ServerRpcAsync("Move", new { X = pos.x, Y = pos.z });
}

record PlayerMovedData(string PlayerId, float X, float Y);
record ChatData(string PlayerId, string Name, string Text);
record TokenResponse(string Token, string PlayerId);

// Stub for the sample — in real Unity code these come from UnityEngine.dll
namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public static readonly Vector3 zero = new();
    }
}
