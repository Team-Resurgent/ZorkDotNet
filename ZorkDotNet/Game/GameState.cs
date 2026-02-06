namespace ZorkDotNet.Game;

/// <summary>Event to run when Moves >= TriggerMove.</summary>
public record ScheduledEvent(int TriggerMove, Action<GameState> Action);

/// <summary>
/// Global game state (MDL: WINNER, HERE, PRSVEC, MOVES, flags, etc.).
/// </summary>
public class GameState
{
    public Adventurer Winner { get; } = new();
    public Room Here => Winner.CurrentRoom;
    public int Moves => Winner.Moves;
    public Dictionary<string, bool> Flags { get; } = new();
    public List<Room> Rooms { get; } = new();
    public List<GameObject> AllObjects { get; } = new();
    public bool Running { get; set; } = true;
    public string? LastVerb { get; set; }
    public GameObject? LastDirectObject { get; set; }
    public GameObject? LastIndirectObject { get; set; }
    /// <summary>Orphan handling: after "Which X?" we store pending verb and slot so next input can disambiguate.</summary>
    public string? PendingVerb { get; set; }
    public bool PendingNeedsDirect { get; set; }
    public bool PendingNeedsIndirect { get; set; }
    public string? PendingAmbiguousWord { get; set; }
    /// <summary>When ResolveObject finds multiple matches (GWIM-style), list of candidates for "Which X - the A, the B, or the C?".</summary>
    public List<GameObject>? PendingAmbiguousCandidates { get; set; }
    public string[]? PendingWords { get; set; }
    /// <summary>Lamp burn: ticks remaining when lamp is on (original LAMP-TICKS 50). Decremented each move; at 0 lamp goes out.</summary>
    public int LampTicksRemaining { get; set; }
    /// <summary>Scheduled events (MATCH burnout, FUSE explode, etc.) fired when Moves >= TriggerMove.</summary>
    public List<ScheduledEvent> ScheduledEvents { get; } = new();
    /// <summary>MAINT room: move count when leak message fires (0 = not set).</summary>
    public int MaintLeakAtMove { get; set; }
    /// <summary>Sword glow state (0=none, 1=faint, 2=bright). MDL OTVAL on sword.</summary>
    public Dictionary<string, int> ObjectState { get; } = new();
    /// <summary>Villain health in melee (e.g. TROLL -> 2 hits to kill).</summary>
    public Dictionary<string, int> VillainStrength { get; } = new();
    public TextWriter Output { get; set; } = Console.Out;

    /// <summary>Run each move after parser (CEVENTs: lantern burn, match/fuse/candles, MAINT leak, etc.).</summary>
    public void ProcessClocks()
    {
        if (World == null) return;
        var lamp = World.FindObject("LAMP");
        // Only burn lamp when it has been turned on (LampTicksRemaining set by TURN ON); initial state has LightAmount=-1 but ticks=0
        if (lamp != null && Winner.Inventory.Contains(lamp) && lamp.LightAmount != 0 && LampTicksRemaining > 0)
        {
            LampTicksRemaining--;
            if (LampTicksRemaining <= 0)
            {
                lamp.LightAmount = 0;
                Output.WriteLine("The lamp has run out of power.");
            }
        }
        var toRun = ScheduledEvents.Where(e => Moves >= e.TriggerMove).ToList();
        foreach (var e in toRun) ScheduledEvents.Remove(e);
        foreach (var e in toRun) e.Action(this);
        if (Here?.Id == "MAINT")
        {
            if (MaintLeakAtMove == 0) MaintLeakAtMove = Moves + 3;
            if (MaintLeakAtMove > 0 && Moves >= MaintLeakAtMove && !GetFlag("MAINT-LEAK!-FLAG"))
            {
                SetFlag("MAINT-LEAK!-FLAG", true);
                Output.WriteLine("Water is leaking into the maintenance room.");
            }
        }
        if (Here?.Id == "CAVE2")
        {
            var candl = World.FindObject("CANDL");
            if (candl != null && Winner.Inventory.Contains(candl) && candl.LightAmount > 0 && Random.Shared.Next(2) == 0)
            {
                candl.LightAmount = 0;
                Output.WriteLine("The candles have been blown out by a gust of wind.");
            }
        }
        if (Here?.Id?.StartsWith("LEDG") == true && !GetFlag("VOLGNOME-SCHEDULED!-FLAG"))
        {
            SetFlag("VOLGNOME-SCHEDULED!-FLAG", true);
            ScheduledEvents.Add(new ScheduledEvent(Moves + 10, ObjectActions.Volgnome));
        }
        // Demons (SWORD-DEMON, FIGHT-DEMON, ROBBER-DEMON)
        Combat.SwordDemon(this);
        Combat.FightDemon(this);
        RunRobberDemon();
        // CEVENTs with no effect in original MDL (dung.56 CURIN, SPHIN)
        CureClock(this);
        SphereFunction(this);
    }

    /// <summary>CURE-CLOCK (CURIN). CEVENT in dung.56; no-op in original MDL — implemented as explicit no-op.</summary>
    public static void CureClock(GameState _) { }

    /// <summary>SPHERE-FUNCTION (SPHIN). CEVENT in dung.56; no-op in original MDL — implemented as explicit no-op.</summary>
    public static void SphereFunction(GameState _) { }

    /// <summary>ROBBER-DEMON (act1.37 ROBBER): thief in room can rob (ROB-ROOM + ROB-ADV); thief not in room can appear.</summary>
    private void RunRobberDemon()
    {
        var thief = World?.FindObject("THIEF");
        if (thief == null || Here == null) return;
        var treas = World?.FindRoom("TREAS");

        if (thief.InRoom == Here)
        {
            if (Random.Shared.Next(100) < 30)
            {
                var fromRoom = Combat.RobRoom(this, Here, 100);
                foreach (var o in fromRoom)
                {
                    Here.Objects.Remove(o);
                    o.InRoom = null;
                    o.Carrier = null;
                    o.Container = null;
                    if (treas != null) { o.InRoom = treas; treas.Objects.Add(o); }
                }
                var fromAdv = ObjectActions.RobAdventurer(this, printMessage: false);
                if (fromRoom.Count > 0 || fromAdv)
                {
                    Output.WriteLine("The other occupant just left, still carrying his large bag.  You may not have noticed that he robbed you blind first.");
                    Here.Objects.Remove(thief);
                    thief.InRoom = null;
                }
            }
        }
        else if (thief.InRoom == null && Here.Id != "TREAS" && Random.Shared.Next(100) < 30)
        {
            thief.InRoom = Here;
            Here.Objects.Add(thief);
            if (!GetFlag("THIEF-APPEARED!-FLAG"))
            {
                SetFlag("THIEF-APPEARED!-FLAG", true);
                Output.WriteLine("Someone carrying a large bag is casually leaning against one of the walls here.  He does not speak, but it is clear from his aspect that the bag will be taken only over his dead body.");
            }
        }
    }
    public World? World { get; set; }
    internal StreamWriter? ScriptFile { get; set; }

    public bool GetFlag(string name) => Flags.TryGetValue(name, out var v) && v;
    public void SetFlag(string name, bool value) => Flags[name] = value;

    /// <summary>Block a room (explosion debris / collapsed ledge).</summary>
    public void MungRoom(string? roomId, string message)
    {
        var r = World?.FindRoom(roomId ?? "");
        if (r != null) { r.Munged = true; r.MungedMessage = message; }
    }

    /// <summary>True if room has endogenous light or player/room has a light source (LIGHT-SOURCE).</summary>
    public static bool IsLit(GameState state, Room? room = null)
    {
        var r = room ?? state.Here;
        if (r == null) return false;
        if (r.HasLight) return true;
        return Combat.GetLightSource(state, r) != null;
    }
}
