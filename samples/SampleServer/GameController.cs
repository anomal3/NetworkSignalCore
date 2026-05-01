using NetworkSignalCore.Core.Attributes;
using NetworkSignalCore.Core.Sync;
using NetworkSignalCore.Server.Rpc;

namespace SampleServer;

// Example: how a game developer uses the framework
public sealed class GameController : NetworkController
{
    // This SyncVar auto-broadcasts to all room members when changed
    [SyncVar]
    public SyncVar<int> GameTick = new(0);

    [ServerRpc(RequireAuth = true, RequireRoom = true)]
    public async Task Move(MoveData data)
    {
        // Server validates & echoes back to all in room
        var player = Caller!;

        // Anti-cheat: server could verify max speed here
        await SendToRoomAsync("OnPlayerMoved", new
        {
            PlayerId = player.PlayerId,
            data.X,
            data.Y,
        });
    }

    [ServerRpc(RequireAuth = true, RequireRoom = true)]
    public async Task Fire(FireData data)
    {
        await SendToRoomAsync("OnPlayerFired", new
        {
            PlayerId = Caller!.PlayerId,
            data.Direction,
        });
    }

    [ServerRpc(RequireAuth = true)]
    public async Task Chat(ChatMessage msg)
    {
        if (string.IsNullOrWhiteSpace(msg.Text)) return;

        var text = msg.Text[..Math.Min(msg.Text.Length, 200)]; // hard cap
        await SendToRoomAsync("OnChat", new
        {
            PlayerId  = Caller!.PlayerId,
            Name      = Caller.DisplayName,
            Text      = text,
        });
    }
}

public sealed record MoveData(float X, float Y);
public sealed record FireData(float Direction);
public sealed record ChatMessage(string Text);
