namespace ZorkDotNet.Game;

/// <summary>
/// Represents a room in the dungeon (MDL ROOM: RID, RDESC1, RDESC2, RSEEN?, RLIGHT?, REXITS, ROBJS, RACTION, RVAL, RBITS).
/// </summary>
public class Room
{
    public string Id { get; set; } = "";
    /// <summary>Long description (shown on first look).</summary>
    public string DescriptionLong { get; set; } = "";
    /// <summary>Short description (brief mode).</summary>
    public string DescriptionShort { get; set; } = "";
    public bool Seen { get; set; }
    public bool HasLight { get; set; }
    /// <summary>Direction -> (target room id or blocked message).</summary>
    public Dictionary<string, ExitTarget> Exits { get; } = new();
    public List<GameObject> Objects { get; } = new();
    /// <summary>Optional room-action (e.g. EAST-HOUSE, KITCHEN).</summary>
    public Action<GameState, string>? RoomAction { get; set; }
    /// <summary>Optional action when player enters room (e.g. BOOM, BATS, CELLAR door).</summary>
    public Action<GameState>? OnEnter { get; set; }
    public int VisitScore { get; set; }
    /// <summary>Room-specific var (e.g. CLEAR RVARS for grating revealed).</summary>
    public int RVars { get; set; }
    /// <summary>When true, entering this room is blocked (explosion debris, collapsed ledge).</summary>
    public bool Munged { get; set; }
    /// <summary>Message shown when trying to enter a munged room.</summary>
    public string? MungedMessage { get; set; }
}

public readonly struct ExitTarget
{
    public string? RoomId { get; }
    public string? BlockedMessage { get; }
    public string? ConditionFlag { get; }
    public bool IsBlocked => RoomId == null && BlockedMessage != null;
    public bool IsConditional => ConditionFlag != null;

    public ExitTarget(string roomId)
    {
        RoomId = roomId;
        BlockedMessage = null;
        ConditionFlag = null;
    }

    public ExitTarget(string blockedMessage, bool _ = false)
    {
        RoomId = null;
        BlockedMessage = blockedMessage;
        ConditionFlag = null;
    }

    public ExitTarget(string conditionFlag, string roomId, string? blockedMessage)
    {
        ConditionFlag = conditionFlag;
        RoomId = roomId;
        BlockedMessage = blockedMessage;
    }
}
