namespace ZorkDotNet.Game;

/// <summary>
/// The player (MDL ADV: AROOM, AOBJS, ASCORE, AVEHICLE, AOBJ, AACTION, ASTRENGTH, AFLAGS).
/// </summary>
public class Adventurer
{
    public Room CurrentRoom { get; set; } = null!;
    public List<GameObject> Inventory { get; } = new();
    public int Score { get; set; }
    public GameObject? Vehicle { get; set; }
    public int Strength { get; set; } = 100;
    public int Moves { get; set; }
    /// <summary>MDL ASTAGGERED: can't fight this turn.</summary>
    public bool Staggered { get; set; }
    public bool BriefMode { get; set; }
    public bool SuperBriefMode { get; set; }

    public int CarryingSize => Inventory.Sum(o => o.Size);
    public const int MaxLoad = 100; // LOAD-MAX 100 in defs.63
}
