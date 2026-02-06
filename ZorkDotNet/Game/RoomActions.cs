namespace ZorkDotNet.Game;

/// <summary>
/// Room-action handlers ported from act1.37, act1.38 (e.g. EAST-HOUSE, KITCHEN, LIVING-ROOM, BOOM-ROOM, BATS-ROOM, GLACIER-ROOM).
/// </summary>
public static class RoomActions
{
    public static void EastHouse(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        var windowOpen = state.GetFlag("KITCHEN-WINDOW!-FLAG");
        state.Output.WriteLine(
            "You are behind the white house.  In one corner of the house there is a small window which is " +
            (windowOpen ? "open." : "slightly ajar."));
    }

    public static void Kitchen(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        state.Output.WriteLine(
            "You are in the kitchen of the white house.  A table seems to have been used recently for the preparation of food.  A passage leads to the west and a dark staircase can be seen leading upward.  To the east is a small window which is " +
            (state.GetFlag("KITCHEN-WINDOW!-FLAG") ? "open." : "slightly ajar."));
    }

    public static void LivingRoom(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        var magic = state.GetFlag("MAGIC-FLAG!-FLAG");
        var trap = state.GetFlag("TRAP-DOOR!-FLAG");
        var rugMoved = state.GetFlag("RUG-MOVED!-FLAG");
        state.Output.Write(
            magic
                ? "You are in the living room.  There is a door to the east.  To the west is a cyclops-shaped hole in an old wooden door, above which is some strange gothic lettering "
                : "You are in the living room.  There is a door to the east, a wooden door with strange gothic lettering to the west, which appears to be nailed shut, ");
        if (rugMoved && trap)
            state.Output.WriteLine("and a rug lying beside an open trap-door.");
        else if (rugMoved)
            state.Output.WriteLine("and a closed trap-door at your feet.");
        else if (trap)
            state.Output.WriteLine("and an open trap-door at your feet.");
        else
            state.Output.WriteLine("and a large oriental rug in the center of the room.");
    }

    public static void BoomRoom(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        state.Output.WriteLine("You are in a small room which smells strongly of coal gas.");
    }

    /// <summary>On entering BOOM room: if carrying lit candle or lit torch, JIGS-UP.</summary>
    public static void OnEnterBoom(GameState state)
    {
        var torch = state.World?.FindObject("TORCH");
        var candl = state.World?.FindObject("CANDL");
        var lit = (torch != null && state.Winner.Inventory.Contains(torch) && torch.LightAmount > 0) ||
                  (candl != null && state.Winner.Inventory.Contains(candl) && candl.LightAmount > 0);
        if (!lit) return;
        var o = torch != null && state.Winner.Inventory.Contains(torch) ? torch : candl;
        state.Output.WriteLine("Oh dear.  It appears that the smell coming from this room was coal gas.  I would have thought twice about carrying a " + (o?.DescriptionShort ?? "light") + " in here.");
        state.Output.WriteLine("   BOOOOOOOOOOOM      ");
        state.Running = false;
    }

    /// <summary>On entering BATS room without garlic: bat flies player to random room.</summary>
    public static void OnEnterBats(GameState state)
    {
        if (state.Winner.Inventory.Any(o => o.Id == "GARLI")) return;
        var batDrops = new[] { "MINE1", "MINE2", "MINE3", "MINE4", "MINE5", "MINE6", "MINE7", "TLADD", "BLADD" };
        var pick = batDrops[Random.Shared.Next(batDrops.Length)];
        state.Output.WriteLine("A deranged giant vampire bat (a reject from WUMPUS) swoops down from his belfry and lifts you away....");
        var next = state.World?.FindRoom(pick);
        if (next != null)
        {
            state.Winner.CurrentRoom = next;
            state.Winner.Moves++;
            if (!next.Seen) state.Winner.Score += next.VisitScore;
            next.Seen = true;
        }
    }

    /// <summary>On entering CELLA: if trap door was open, it slams shut.</summary>
    public static void OnEnterCellar(GameState state)
    {
        if (!state.GetFlag("TRAP-DOOR!-FLAG")) return;
        var door = state.World?.FindObject("TDOOR");
        if (door != null && door.Touched) return;
        state.SetFlag("TRAP-DOOR!-FLAG", false);
        if (door != null) door.Touched = true;
        state.Output.WriteLine("The trap door crashes shut, and you hear someone barring it.");
    }

    public static void BatsRoom(GameState state, string verb)
    {
        if (verb == "LOOK")
        {
            state.Output.WriteLine("You are in a small room which has only one door, to the east.");
            var hasGarlic = state.Winner.Inventory.Any(o => o.Id == "GARLI");
            if (hasGarlic)
                state.Output.WriteLine("In the corner of the room on the ceiling is a large vampire bat who is obviously deranged and holding his nose.");
        }
    }

    public static void GlacierRoom(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        var flag = state.GetFlag("GLACIER-FLAG!-FLAG");
        state.Output.WriteLine("You are in a large room, with giant icicles hanging from the walls and ceiling.  There are passages to the north and east." +
            (flag ? " There is a large passageway leading westward." : ""));
    }

    public static void MachineRoom(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        state.Output.WriteLine("You are in a large room which seems to be air-conditioned.  In one corner there is a machine (?) which is shaped somewhat like a clothes dryer.  On the 'panel' there is a switch which is labelled in a dialect of Swahili.  Fortunately, I know this dialect and the label translates to START.  The switch does not appear to be manipulable by any human hand (unless the fingers are about 1/16 by 1/4 inch).  On the front of the machine is a large lid.");
        var mach = state.World?.FindObject("MACHI");
        if (mach != null)
            state.Output.WriteLine(mach.Open ? "The lid on the machine is open." : "The lid on the machine is closed.");
    }

    /// <summary>CLEARING: LOOK with grating; leaves can be burned/moved to reveal grating.</summary>
    public static void Clearing(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        state.Output.WriteLine("You are in a clearing, with a forest surrounding you on the west and south.");
        if (state.GetFlag("KEY-FLAG!-FLAG"))
            state.Output.WriteLine("There is an open grating, descending into darkness.");
        else if (state.Here.RVars != 0)
            state.Output.WriteLine("There is a grating securely fastened into the ground.");
    }

    /// <summary>DAM: LOOK text (Flood Control Dam #3).</summary>
    public static void DamRoom(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        state.Output.WriteLine("You are standing on the top of the Flood Control Dam #3, which was quite a tourist attraction in times far distant.  There are paths to the north, south, east, and down.");
        var lowTide = state.GetFlag("LOW-TIDE!-FLAG");
        if (lowTide)
            state.Output.WriteLine("It appears that the dam has been opened since the water level behind it is low and the sluice gate has been opened.  Water is rushing downstream through the gates.");
        else
            state.Output.WriteLine("The sluice gates on the dam are closed.  Behind the dam, there can be seen a wide lake.  A small stream is formed by the runoff from the lake.");
        state.Output.WriteLine("There is a control panel here.  There is a large metal bolt on the panel. Above the bolt is a small green plastic bubble.");
        if (state.GetFlag("GATE-FLAG!-FLAG"))
            state.Output.WriteLine("The green bubble is glowing.");
    }

    /// <summary>LLD1 (Entrance to Hades): LOOK text.</summary>
    public static void LldRoom(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        state.Output.WriteLine("You are outside a large gateway, on which is inscribed \"Abandon every hope, all ye who enter here.\" The gate is open; through it you can see a desolation, with a pile of mangled corpses in one corner.  Thousands of voices, lamenting some hideous fate, can be heard.");
        if (!state.GetFlag("LLD-FLAG!-FLAG"))
            state.Output.WriteLine("The way through the gate is barred by evil spirits, who jeer at your attempts to pass.");
    }

    /// <summary>CAROUSEL (Round room): LOOK text; compass message when CAROUSEL-FLIP is false.</summary>
    public static void CarouselRoom(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        state.Output.WriteLine("You are in a circular room with passages off in eight directions.");
        if (!state.GetFlag("CAROUSEL-FLIP!-FLAG"))
            state.Output.WriteLine("Your compass needle spins wildly, and you can't get your bearings.");
    }

    /// <summary>SAFE (Dusty Room): LOOK text.</summary>
    public static void SafeRoom(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        state.Output.WriteLine("You are in a dusty old room which is virtually featureless, except for an exit on the north side.");
        state.Output.WriteLine(state.GetFlag("SAFE-FLAG!-FLAG")
            ? "On the far wall is a rusty box, whose door has been blown off."
            : "Imbedded in the far wall, there is a rusty old box.  It appears that the box is somewhat damaged, since an oblong hole has been chipped out of the front of it.");
    }

    /// <summary>Mirror room (MIRR1/MIRR2): LOOK text; MIRROR-MUNG message when broken.</summary>
    public static void MirrorRoom(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        state.Output.WriteLine("You are in a large square room with tall ceilings.  On the south wall is an enormous mirror which fills the entire wall.  There are exits on the other three sides of the room.");
        if (state.GetFlag("MIRROR-MUNG!-FLAG"))
            state.Output.WriteLine("Unfortunately, you have managed to destroy it by your reckless actions.");
    }

    /// <summary>Cyclops room (CYCLO): LOOK text with cyclops state (RVARS, CYCLOPS-FLAG, MAGIC-FLAG).</summary>
    public static void CyclopsRoom(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        state.Output.WriteLine("You are in a room with an exit on the west side, and a staircase leading up.");
        var here = state.Here;
        var cyclops = state.World?.FindObject("CYCLO");
        var inRoom = cyclops != null && here != null && here.Objects.Contains(cyclops);
        if (state.GetFlag("CYCLOPS-FLAG!-FLAG") && !state.GetFlag("MAGIC-FLAG!-FLAG"))
            state.Output.WriteLine("The cyclops, perhaps affected by a drug in your drink, is sleeping blissfully at the foot of the stairs.");
        else if (state.GetFlag("MAGIC-FLAG!-FLAG"))
            state.Output.WriteLine("On the north of the room is a wall which used to be solid, but which now has a cyclops-sized hole in it.");
        else if (inRoom)
        {
            var v = here!.RVars;
            if (v == 0)
                state.Output.WriteLine("A cyclops, who looks prepared to eat horses (much less mere adventurers), blocks the staircase.  From his state of health, and the bloodstains on the walls, you gather that he is not very friendly, though he likes people.");
            else if (v > 0)
                state.Output.WriteLine("The cyclops is standing in the corner, eyeing you closely.  I don't think he likes you very much.  He looks extremely hungry even for a cyclops.");
            else
                state.Output.WriteLine("The cyclops, having eaten the hot peppers, appears to be gasping. His enflamed tongue protrudes from his man-sized mouth.");
        }
    }

    /// <summary>FALLS (Aragain Falls): LOOK text; RAINBOW flag shows solid rainbow.</summary>
    public static void FallsRoom(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        state.Output.WriteLine("You are at the top of Aragain Falls, an enormous waterfall with a drop of about 450 feet.  The only path here is on the north end.");
        state.Output.WriteLine(state.GetFlag("RAINBOW!-FLAG")
            ? "A solid rainbow spans the falls."
            : "A beautiful rainbow can be seen over the falls and to the east.");
    }

    /// <summary>LLD2 (Land of the Living Dead): LOOK text.</summary>
    public static void Lld2Room(GameState state, string verb)
    {
        if (verb != "LOOK") return;
        state.Output.WriteLine("You have entered the Land of the Living Dead, a large desolate room. Although it is apparently uninhabited, you can hear the sounds of thousands of lost souls weeping and moaning.  In the east corner are stacked the remains of dozens of previous adventurers who were less fortunate than yourself.  To the east is an ornate passage, apparently recently constructed.");
        if (state.GetFlag("ON-POLE!-FLAG"))
            state.Output.WriteLine("Amid the desolation, you spot what appears to be your head, at the end of a long pole.");
    }
}
