namespace NetworkSignalCore.Core.Messages;

public sealed class CreateRoomRequest
{
    public string  Name       { get; set; } = string.Empty;
    public int     MaxPlayers { get; set; } = 4;
    public bool    IsPrivate  { get; set; }
    public string? Password   { get; set; }
    public string? GameMode   { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public sealed class JoinRoomRequest
{
    public string  RoomId   { get; set; } = string.Empty;
    public string? Password { get; set; }
}

public sealed class KickPlayerRequest
{
    public string TargetPlayerId { get; set; } = string.Empty;
    public string Reason         { get; set; } = string.Empty;
}

public sealed class RoomOperationResult
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
