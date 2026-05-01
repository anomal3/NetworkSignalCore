namespace NetworkSignalCore.Core.Messages;

public enum PacketType : byte
{
    // System
    Ping          = 2,
    Pong          = 3,
    Disconnect    = 4,
    Error         = 5,

    // Auth
    AuthRequest   = 10,
    AuthResponse  = 11,

    // RPC
    ServerRpc     = 20,
    ClientRpc     = 21,
    TargetRpc     = 22,

    // Sync
    SyncState     = 30,
    SyncDelta     = 31,

    // Room
    CreateRoom    = 40,
    JoinRoom      = 41,
    LeaveRoom     = 42,
    KickPlayer    = 43,
    RoomUpdate    = 44,
    RoomList      = 45,

    // Matchmaking
    JoinQueue     = 50,
    LeaveQueue    = 51,
    MatchFound    = 52,
    MatchAccepted = 53,
    MatchDeclined = 54,

    // Party
    CreateParty   = 60,
    JoinParty     = 61,
    LeaveParty    = 62,
    PartyUpdate   = 63,
    InviteToParty = 64,
}
