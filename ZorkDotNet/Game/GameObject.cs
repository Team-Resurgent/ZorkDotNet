namespace ZorkDotNet.Game;

/// <summary>
/// Represents an object (MDL OBJECT: OID, ONAMES, ODESC1, ODESC2, ODESCO, OACTION, OCONTENTS, OCAN, OFLAGS, ...).
/// </summary>
public class GameObject
{
    public string Id { get; set; } = "";
    public List<string> Synonyms { get; set; } = new();
    /// <summary>Description when in room / not carried.</summary>
    public string DescriptionHere { get; set; } = "";
    /// <summary>Short description (e.g. "lamp", "sword").</summary>
    public string DescriptionShort { get; set; } = "";
    public string? DescriptionUntouched { get; set; }
    public Action<GameState, string, GameObject?>? ObjectAction { get; set; }
    public List<GameObject> Contents { get; } = new();
    public GameObject? Container { get; set; }
    public ObjectFlags Flags { get; set; }
    public bool Touched { get; set; }
    public int LightAmount { get; set; }
    public int FindScore { get; set; }
    public int TrophyScore { get; set; }
    public bool Open { get; set; }
    public int Size { get; set; } = 5;
    public int Capacity { get; set; }
    public Room? InRoom { get; set; }
    /// <summary>Adventurer carrying this (null if in room or in container).</summary>
    public Adventurer? Carrier { get; set; }

    public bool CanTake => (Flags & ObjectFlags.Take) != 0;
    public bool IsVisible => (Flags & ObjectFlags.Visible) != 0;
    public bool IsContainer => (Flags & ObjectFlags.Container) != 0;
    public bool IsTransparent => (Flags & ObjectFlags.Transparent) != 0;
    public bool IsOpenOrTransparent => Open || IsTransparent;
    public bool ProvidesLight => LightAmount > 0;
    /// <summary>MDL FIGHTBIT: in melee (takes damage each round).</summary>
    public bool Fighting => (Flags & ObjectFlags.Fighting) != 0;
    /// <summary>MDL VILLAIN: troll, thief, cyclops.</summary>
    public bool IsVillain => (Flags & ObjectFlags.Villain) != 0;
}

[Flags]
public enum ObjectFlags
{
    None = 0,
    Visible = 1 << 0,
    Readable = 1 << 1,
    Take = 1 << 2,
    Door = 1 << 3,
    Transparent = 1 << 4,
    Food = 1 << 5,
    NoDescribe = 1 << 6,
    Drinkable = 1 << 7,
    Container = 1 << 8,
    Light = 1 << 9,
    Burnable = 1 << 10,
    OnFire = 1 << 11,
    Tool = 1 << 12,
    Turnable = 1 << 13,
    Vehicle = 1 << 14,
    Weapon = 1 << 15,
    Sacred = 1 << 16,
    /// <summary>MDL FIGHTBIT: in melee.</summary>
    Fighting = 1 << 17,
    /// <summary>MDL VILLAIN: villain (troll, thief, cyclops).</summary>
    Villain = 1 << 18,
}
