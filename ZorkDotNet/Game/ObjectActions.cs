namespace ZorkDotNet.Game;

/// <summary>
/// Object-action handlers ported from act1.37, act2.27, act3.13 (e.g. WINDOW-FUNCTION, RUG, LANTERN, BOTTLE-FUNCTION).
/// </summary>
public static class ObjectActions
{
    public static void WindowFunction(GameState state, string verb, GameObject? _)
    {
        if (verb == "OPEN")
        {
            if (state.GetFlag("KITCHEN-WINDOW!-FLAG"))
                state.Output.WriteLine("It is already open.");
            else
            {
                state.Output.WriteLine("With great effort, you open the window far enough to allow entry.");
                state.SetFlag("KITCHEN-WINDOW!-FLAG", true);
            }
        }
        else if (verb == "CLOSE")
        {
            if (state.GetFlag("KITCHEN-WINDOW!-FLAG"))
            {
                state.Output.WriteLine("The window closes (more easily than it opened).");
                state.SetFlag("KITCHEN-WINDOW!-FLAG", false);
            }
            else
                state.Output.WriteLine("It is already closed.");
        }
    }

    public static void Rug(GameState state, string verb, GameObject? obj)
    {
        if (verb == "TAKE" && obj != null)
            state.Output.WriteLine("The rug is too heavy to move.");
        else if (verb == "LIFT" && obj != null)
            state.Output.WriteLine("The rug is too heavy to lift, but in trying to take it you have noticed an irregularity beneath it.");
        else if (verb == "MOVE" && obj != null)
        {
            if (state.GetFlag("RUG-MOVED!-FLAG"))
                state.Output.WriteLine("Having moved the carpet previously, you find it impossible to move it again.");
            else
            {
                state.Output.WriteLine("With a great effort, the rug is moved to one side of the room. With the rug moved, the dusty cover of a closed trap-door appears.");
                state.SetFlag("RUG-MOVED!-FLAG", true);
                var tdoor = state.World?.FindObject("TDOOR");
                if (tdoor != null) tdoor.Flags |= ObjectFlags.Visible;
                var door = state.World?.FindObject("DOOR");
                if (door != null) door.Flags |= ObjectFlags.Visible;
                // Ensure trap door in current room is visible (same ref as World, but belt-and-braces for OPEN DOOR resolution)
                if (state.Here != null)
                    foreach (var o in state.Here.Objects)
                        if (o.Id == "DOOR") o.Flags |= ObjectFlags.Visible;
            }
        }
    }

    public static void TrophyCase(GameState state, string verb, GameObject? _)
    {
        if (verb == "TAKE")
            state.Output.WriteLine("The trophy case is securely fastened to the wall (perhaps to foil any attempt by robbers to remove it).");
    }

    public static void Lantern(GameState state, string verb, GameObject? obj)
    {
        var lamp = state.World?.FindObject("LAMP");
        var blamp = state.World?.FindObject("BLAMP");
        if (verb == "THROW" && obj != null)
        {
            state.Output.WriteLine("The lamp has smashed into the floor and the light has gone out.");
            RemoveFromWhere(state, lamp!);
            if (blamp != null && state.Here != null)
            {
                blamp.InRoom = state.Here;
                blamp.Carrier = null;
                state.Here.Objects.Add(blamp);
            }
        }
        else if (verb == "TURNON" || verb == "TURN ON")
        {
            if (lamp != null)
            {
                lamp.LightAmount = -1;
                state.LampTicksRemaining = 100; // LAMP-TICKS: burn timer (100 so cellar→troll→carousel path stays lit for take/wave iron box)
                state.Output.WriteLine("The lamp is now on.");
            }
        }
        else if (verb == "TURNOFF" || verb == "TURN OFF")
        {
            if (lamp != null) { lamp.LightAmount = 0; state.Output.WriteLine("The lamp is now off."); }
        }
    }

    /// <summary>MATCH: BURN or TURN ON lights it; burns out after 5 moves (ProcessClocks).</summary>
    public static void MatchFunction(GameState state, string verb, GameObject? obj)
    {
        if (obj == null) return;
        if (verb == "BURN" || verb == "TURNON" || verb == "TURN ON")
        {
            if (obj.LightAmount > 0)
            {
                state.Output.WriteLine("The match is already lit.");
                return;
            }
            obj.LightAmount = 1;
            state.ScheduledEvents.Add(new ScheduledEvent(state.Moves + 5, _ =>
            {
                if (obj.LightAmount > 0)
                {
                    obj.LightAmount = 0;
                    _.Output.WriteLine("The match has burned out.");
                }
            }));
            state.Output.WriteLine("The match is now lit.");
        }
    }

    /// <summary>CANDLES: TURN ON / TURN OFF. In CAVE2, 50% chance each move to blow out (ProcessClocks).</summary>
    public static void CandlesFunction(GameState state, string verb, GameObject? obj)
    {
        if (obj == null) return;
        if (verb == "TURNON" || verb == "TURN ON")
        {
            obj.LightAmount = 1;
            state.Output.WriteLine("The candles are now lit.");
        }
        else if (verb == "TURNOFF" || verb == "TURN OFF")
        {
            obj.LightAmount = 0;
            state.Output.WriteLine("The candles are now off.");
        }
    }

    /// <summary>FUSE: BURN lights it; after 2 moves explodes (same room = death; else SAFE-MUNG in 5).</summary>
    public static void FuseFunction(GameState state, string verb, GameObject? obj)
    {
        if (obj == null) return;
        if (verb != "BURN") return;
        if (obj.LightAmount > 0)
        {
            state.Output.WriteLine("The fuse is already lit.");
            return;
        }
        obj.LightAmount = 1;
        state.ScheduledEvents.Add(new ScheduledEvent(state.Moves + 2, _ =>
        {
            var fuse = _.World?.FindObject("FUSE");
            if (fuse?.Container?.Id != "BRICK")
            {
                _.Output.WriteLine("The fuse has burned to the end.");
                _.Output.WriteLine("The wire rapidly burns into nothingness.");
                if (fuse != null) fuse.LightAmount = 0;
                return;
            }
            _.Output.WriteLine("The fuse has burned to the end.");
            var brick = _.World?.FindObject("BRICK");
            string? brickRoomId = null;
            if (brick != null)
            {
                if (brick.InRoom != null) brickRoomId = brick.InRoom.Id;
                else if (brick.Carrier != null) brickRoomId = brick.Carrier.CurrentRoom?.Id;
            }
            brickRoomId ??= _.Here?.Id;
            if (brickRoomId != null && _.Here?.Id == brickRoomId)
            {
                _.Output.WriteLine("Now you've done it.  It seems that the brick has other properties than weight, namely the ability to blow you to smithereens.");
                _.Output.WriteLine("   BOOOOOOOOOOOM      ");
                _.Running = false;
                return;
            }
            _.Output.WriteLine("There is an explosion nearby.");
            var capturedRoom = brickRoomId;
            _.ScheduledEvents.Add(new ScheduledEvent(_.Moves + 5, s => SafeMung(s, capturedRoom)));
        }));
        state.Output.WriteLine("The fuse is lit.");
    }

    /// <summary>SAFE-MUNG (CEVENT): if player in munged room → death; else tell rumbling; if SAFE, schedule LEDGE-MUNG in 8; mung room.</summary>
    public static void SafeMung(GameState state, string? mungedRoomId)
    {
        var r = state.World?.FindRoom(mungedRoomId ?? "");
        if (r == null) return;
        if (state.Here?.Id == mungedRoomId)
        {
            state.Output.WriteLine(r.Id == "SAFE"
                ? "The house shakes, and the ceiling of the room you're in collapses, turning you into a pancake."
                : "The room trembles and 50,000 pounds of rock fall on you, turning you into a pancake.");
            state.Running = false;
            return;
        }
        state.Output.WriteLine("You may recall your recent explosion.  Well, probably as a result of that, you hear an ominous rumbling, as if one of the rooms in the dungeon had collapsed.");
        if (mungedRoomId == "SAFE")
        {
            state.SetFlag("SAFE-FLAG!-FLAG", true);
            state.ScheduledEvents.Add(new ScheduledEvent(state.Moves + 8, LedgeMung));
        }
        state.MungRoom(mungedRoomId, "The way is blocked by debris from an explosion.");
    }

    /// <summary>LEDGE-MUNG (CEVENT): if on LEDG4 with balloon tied → death; else message; mung LEDG4.</summary>
    public static void LedgeMung(GameState state)
    {
        if (state.Here?.Id == "LEDG4")
        {
            var inBalloon = state.Winner.Vehicle?.Id == "BALLO";
            if (inBalloon && state.GetFlag("BTIE!-FLAG"))
            {
                state.Output.WriteLine("The ledge collapses, probably as a result of the explosion.  A large chunk of it, which is attached to the hook, drags you down to the ground.  Fatally.");
                state.Running = false;
                return;
            }
            if (inBalloon)
                state.Output.WriteLine("The ledge collapses, leaving you with no place to land.");
            else
                state.Output.WriteLine("The force of the explosion has caused the ledge to collapse belatedly.");
        }
        else
            state.Output.WriteLine("The ledge collapses, giving you a narrow escape.");
        state.MungRoom("LEDG4", "The ledge has collapsed and cannot be landed on.");
    }

    /// <summary>BRICK: BURN → game over (blow you to smithereens).</summary>
    public static void BrickFunction(GameState state, string verb, GameObject? _)
    {
        if (verb != "BURN") return;
        state.Output.WriteLine("Now you've done it.  It seems that the brick has other properties than weight, namely the ability to blow you to smithereens.");
        state.Output.WriteLine("   BOOOOOOOOOOOM      ");
        state.Running = false;
    }

    /// <summary>GNOME-FUNCTION (CEVENT): GIVE valuable → door appears; clock tick → gnome leaves.</summary>
    public static void GnomeFunction(GameState state, string verb, GameObject? obj)
    {
        if (verb == "GIVE" || verb == "THROW")
        {
            if (obj != null && obj.TrophyScore > 0)
            {
                state.Output.WriteLine("Thank you very much for the " + obj.DescriptionShort + ".  I don't believe I've ever seen one as beautiful. 'Follow me', he says, and a door appears on the west end of the ledge.  Through the door, you can see a narrow chimney sloping steeply downward.");
                state.SetFlag("GNOME-DOOR!-FLAG", true);
            }
            else if (obj != null)
            {
                state.Output.WriteLine("'That wasn't quite what I had in mind', he says, crunching the " + obj.DescriptionShort + " in his rock-hard hands.");
                RemoveFromWhere(state, obj);
            }
            return;
        }
        if (verb == "C-INT" || verb == "TICK")
        {
            state.Output.WriteLine("The gnome glances at his watch.  'Oops.  I'm late for an appointment!' He disappears, leaving you alone on the ledge.");
            var gnome = state.World?.FindObject("GNOME");
            if (gnome != null) RemoveFromWhere(state, gnome);
            return;
        }
        state.Output.WriteLine("The gnome appears increasingly nervous.");
        state.SetFlag("GNOME-FLAG!-FLAG", true);
    }

    /// <summary>VOLGNOME (CEVENT): when in LEDG room, gnome appears after 10 moves.</summary>
    public static void Volgnome(GameState state)
    {
        if (state.Here?.Id?.StartsWith("LEDG") != true) return;
        state.Output.WriteLine("A volcano gnome seems to walk straight out of the wall and says 'I have a very busy appointment schedule and little time to waste on tresspassers, but for a small fee, I'll show you the way out.'  You notice the gnome nervously glancing at his watch.");
        var gnome = state.World?.FindObject("GNOME");
        if (gnome != null && state.Here != null)
        {
            gnome.Container?.Contents.Remove(gnome);
            gnome.InRoom?.Objects.Remove(gnome);
            gnome.Carrier?.Inventory.Remove(gnome);
            gnome.InRoom = state.Here;
            gnome.Carrier = null;
            gnome.Container = null;
            state.Here.Objects.Add(gnome);
            state.ScheduledEvents.Add(new ScheduledEvent(state.Moves + 5, GnomeLeave));
        }
    }

    /// <summary>Gnome leaves after 5 moves (GNOIN clock).</summary>
    public static void GnomeLeave(GameState state)
    {
        state.Output.WriteLine("The gnome glances at his watch.  'Oops.  I'm late for an appointment!' He disappears, leaving you alone on the ledge.");
        var gnome = state.World?.FindObject("GNOME");
        if (gnome != null) RemoveFromWhere(state, gnome);
    }

    /// <summary>BUCKET (CEVENT): scheduled tick toggles bucket between TWELL and BWELL. Schedule when water put in bucket in BWELL.</summary>
    public static void BucketTick(GameState state)
    {
        var bucket = state.World?.FindObject("BUCKE");
        if (bucket == null) return;
        if (state.GetFlag("BUCKET-TOP!-FLAG"))
        {
            state.Output.WriteLine("The bucket descends and comes to a stop.");
            state.SetFlag("BUCKET-TOP!-FLAG", false);
            var bwell = state.World?.FindRoom("BWELL");
            if (bwell != null && bucket.InRoom != null) { bucket.InRoom.Objects.Remove(bucket); bucket.InRoom = bwell; bwell.Objects.Add(bucket); }
        }
        else
        {
            state.Output.WriteLine("The bucket rises and comes to a stop.");
            state.SetFlag("BUCKET-TOP!-FLAG", true);
            var twell = state.World?.FindRoom("TWELL");
            if (twell != null && bucket.InRoom != null) { bucket.InRoom.Objects.Remove(bucket); bucket.InRoom = twell; twell.Objects.Add(bucket); }
            state.ScheduledEvents.Add(new ScheduledEvent(state.Moves + 100, BucketTick));
        }
    }

    /// <summary>BURNUP (original CEVENT): when the object burning in RECEP burns out – remove it, message, clear BINF!-FLAG.</summary>
    public static void Burnup(GameState state)
    {
        var recep = state.World?.FindObject("RECEP");
        if (recep == null || recep.Contents.Count == 0)
        {
            state.SetFlag("BINF!-FLAG", false);
            return;
        }
        var obj = recep.Contents[0];
        recep.Contents.RemoveAt(0);
        obj.Container = null;
        obj.InRoom = null;
        obj.Carrier = null;
        obj.LightAmount = 0;
        state.Output.WriteLine("It seems that the " + obj.DescriptionShort + " has burned out, and the cloth bag starts to collapse.");
        state.SetFlag("BINF!-FLAG", false);
    }

    /// <summary>When player BURNs something that is inside RECEP (balloon receptacle): light it, schedule BURNUP.</summary>
    public static void BurnInReceptacle(GameState state, GameObject obj)
    {
        if (obj.LightAmount > 0)
        {
            state.Output.WriteLine("The " + obj.DescriptionShort + " is already burning.");
            return;
        }
        state.Output.WriteLine("The " + obj.DescriptionShort + " burns inside the receptacle.");
        obj.LightAmount = 1;
        var ticks = Math.Max(1, obj.Size * 20);
        state.ScheduledEvents.Add(new ScheduledEvent(state.Moves + ticks, _ => Burnup(_)));
        if (!state.GetFlag("BINF!-FLAG"))
        {
            state.Output.WriteLine("The cloth bag inflates as it fills with hot air.");
            state.SetFlag("BINF!-FLAG", true);
        }
    }

    public static void BottleFunction(GameState state, string verb, GameObject? obj)
    {
        if (verb == "THROW" && obj != null)
        {
            state.Output.WriteLine("The bottle hits the far wall and is decimated.");
            RemoveFromWhere(state, obj);
        }
        else if (verb == "MUNG" || verb == "BREAK")
        {
            if (obj?.Carrier == state.Winner)
            {
                state.Winner.Inventory.Remove(obj);
                obj.Carrier = null;
                state.Output.WriteLine("You have destroyed the bottle.  Well done.");
            }
            else if (obj?.InRoom == state.Here)
            {
                state.Here.Objects.Remove(obj);
                obj.InRoom = null;
                state.Output.WriteLine("A brilliant maneuver destroys the bottle.");
            }
        }
    }

    public static void WaterFunction(GameState state, string verb, GameObject? obj)
    {
        if (obj == null) return;
        var bottle = state.World?.FindObject("BOTTL");
        if (verb == "THROW")
        {
            state.Output.WriteLine("The water splashes on the walls, and evaporates immediately.");
            RemoveFromWhere(state, obj);
        }
        else if (verb == "PUT")
        {
            var cont = state.LastIndirectObject;
            if (cont == bottle && bottle != null && state.Winner.Inventory.Contains(bottle))
            {
                if (bottle.Contents.Count > 0)
                    state.Output.WriteLine("The bottle is already full.");
                else if (!bottle.Open)
                    state.Output.WriteLine("The bottle is closed.");
                else
                {
                    obj.Container?.Contents.Remove(obj);
                    obj.Container = bottle;
                    bottle.Contents.Add(obj);
                    obj.Carrier = null;
                    state.Winner.Inventory.Remove(obj);
                    state.Output.WriteLine("The bottle is now full of water.");
                }
            }
            else if (cont == (state.World?.FindObject("BUCKE")) && cont != null)
            {
                obj.Container?.Contents.Remove(obj);
                if (obj.InRoom != null) { obj.InRoom.Objects.Remove(obj); obj.InRoom = null; }
                obj.Carrier?.Inventory.Remove(obj);
                obj.Container = cont;
                obj.Carrier = null;
                cont.Contents.Add(obj);
                state.Output.WriteLine("The bucket is now full of water.");
                if (state.Here?.Id == "BWELL")
                    state.ScheduledEvents.Add(new ScheduledEvent(state.Moves + 100, BucketTick));
            }
            else if (cont != null && cont != bottle)
            {
                state.Output.WriteLine("The water leaks out of the " + cont.DescriptionShort + " and evaporates immediately.");
                RemoveFromWhere(state, obj);
            }
            else
                state.Output.WriteLine("The water slips through your fingers.");
        }
        else if (verb == "DROP" || verb == "POUR")
        {
            state.Winner.Inventory.Remove(obj);
            obj.Carrier = null;
            obj.Container = null;
            if (state.Here != null) { obj.InRoom = state.Here; state.Here.Objects.Add(obj); }
            state.Output.WriteLine("The water spills to the floor and evaporates immediately.");
        }
    }

    public static void RopeFunction(GameState state, string verb, GameObject? obj)
    {
        var dome = state.World?.FindRoom("DOME");
        var raili = state.World?.FindObject("RAILI");
        if (state.Here != dome)
        {
            state.SetFlag("DOME-FLAG!-FLAG", false);
            if (verb == "TIE") state.Output.WriteLine("There is nothing it can be tied to.");
            else state.Output.WriteLine("It is not tied to anything.");
            return;
        }
        if (verb == "TIE" && state.LastIndirectObject == raili)
        {
            if (state.GetFlag("DOME-FLAG!-FLAG"))
                state.Output.WriteLine("The rope is already attached.");
            else
            {
                state.Output.WriteLine("The rope drops over the side and comes within ten feet of the floor.");
                state.SetFlag("DOME-FLAG!-FLAG", true);
                if (obj != null && obj.Carrier == state.Winner)
                {
                    state.Winner.Inventory.Remove(obj);
                    obj.Carrier = null;
                    obj.InRoom = dome;
                    dome.Objects.Add(obj);
                }
            }
        }
        else if (verb == "UNTIE")
        {
            if (state.GetFlag("DOME-FLAG!-FLAG"))
            {
                state.SetFlag("DOME-FLAG!-FLAG", false);
                state.Output.WriteLine("Although you tied it incorrectly, the rope becomes free.");
            }
            else
                state.Output.WriteLine("It is not tied to anything.");
        }
        else if (verb == "TIE")
            state.Output.WriteLine("There is nothing it can be tied to.");
    }

    public static void Skeleton(GameState state, string verb, GameObject? _)
    {
        if (verb == "TAKE")
            state.Output.WriteLine("The skeleton is too heavy to carry.");
    }

    public static void LeafPile(GameState state, string verb, GameObject? obj)
    {
        if (verb == "BURN" && obj != null)
        {
            if (obj.InRoom != null)
            {
                state.Output.WriteLine("The leaves burn and the neighbors start to complain.");
                state.Here.Objects.Remove(obj);
            }
            else
            {
                state.Output.WriteLine("The sight of someone carrying a pile of burning leaves so offends the neighbors that they come over and put you out.");
                state.Running = false;
            }
        }
        else if (verb == "MOVE")
            state.Output.WriteLine("Done.");
    }

    public static void GunkFunction(GameState state, string verb, GameObject? obj)
    {
        if (obj?.Container != null)
        {
            obj.Container.Contents.Remove(obj);
            obj.Container = null;
            state.Output.WriteLine("The slag turns out to be rather insubstantial, and crumbles into dust at your touch.  It must not have been very valuable.");
        }
    }

    public static void MachineFunction(GameState state, string verb, GameObject? obj)
    {
        var mach = state.World!.FindObject("MACHI");
        if (mach == null) return;
        if (verb == "OPEN")
        {
            if (mach.Open)
                state.Output.WriteLine("It is already open.");
            else
            {
                state.Output.WriteLine("The lid opens.");
                mach.Open = true;
            }
        }
        else if (verb == "CLOSE")
        {
            if (mach.Open)
            {
                state.Output.WriteLine("The lid closes.");
                mach.Open = false;
            }
            else
                state.Output.WriteLine("It is already closed.");
        }
    }

    public static void Glacier(GameState state, string verb, GameObject? _)
    {
        // Handled in Parser DoThrow: throw torch at ice in ICY.
    }

    public static void MSwitchFunction(GameState state, string verb, GameObject? _)
    {
        if (verb != "TURN") return;
        var screw = state.World!.FindObject("SCREW");
        var coal = state.World.FindObject("COAL");
        var mach = state.World.FindObject("MACHI");
        if (state.LastDirectObject?.Id != "SCREW" && state.LastDirectObject?.Id != "MSWIT") return;
        if (mach?.Open == true)
            state.Output.WriteLine("The machine doesn't seem to want to do anything.");
        else
        {
            state.Output.WriteLine("The machine comes to life (figuratively) with a dazzling display of colored lights and bizarre noises.  After a few moments, the excitement abates.");
            if (coal != null && mach != null && mach.Contents.Contains(coal))
            {
                mach.Contents.Remove(coal);
                var diamo = state.World.FindObject("DIAMO");
                if (diamo != null)
                {
                    diamo.Container = mach;
                    mach.Contents.Add(diamo);
                }
            }
        }
    }

    public static void Dumbwaiter(GameState state, string verb, GameObject? _)
    {
        var top = state.World?.FindRoom("TSHAF");
        var bot = state.World?.FindRoom("BSHAF");
        var tbask = state.World?.FindObject("TBASK");
        var fbask = state.World?.FindObject("FBASK");
        if (top == null || bot == null || tbask == null || fbask == null) return;
        var ct = state.GetFlag("CAGE-TOP!-FLAG");
        if (verb == "RAISE")
        {
            if (ct) state.Output.WriteLine("It's already at the top.");
            else
            {
                RemoveFromWhere(state, tbask); RemoveFromWhere(state, fbask);
                tbask.InRoom = top; top.Objects.Add(tbask); tbask.Carrier = null;
                fbask.InRoom = bot; bot.Objects.Add(fbask); fbask.Carrier = null;
                state.Output.WriteLine("The basket is raised to the top of the shaft.");
                state.SetFlag("CAGE-TOP!-FLAG", true);
            }
        }
        else if (verb == "LOWER")
        {
            if (!ct) state.Output.WriteLine("It's already at the bottom.");
            else
            {
                RemoveFromWhere(state, tbask); RemoveFromWhere(state, fbask);
                tbask.InRoom = bot; bot.Objects.Add(tbask);
                fbask.InRoom = top; top.Objects.Add(fbask);
                state.Output.WriteLine("The basket is lowered to the bottom of the shaft.");
                state.SetFlag("CAGE-TOP!-FLAG", false);
            }
        }
        else if (verb == "TAKE")
        {
            if ((ct && state.Here == top) || (!ct && state.Here == bot))
                state.Output.WriteLine("The cage is securely fastened to the iron chain.");
            else
                state.Output.WriteLine("I can't see that here.");
        }
    }

    private static void RemoveFromWhere(GameState state, GameObject obj)
    {
        if (obj.InRoom != null) { obj.InRoom.Objects.Remove(obj); obj.InRoom = null; }
        if (obj.Carrier != null) { obj.Carrier.Inventory.Remove(obj); obj.Carrier = null; }
    }

    public static void TrapDoor(GameState state, string verb, GameObject? _)
    {
        if (state.Here.Id == "LROOM")
        {
            if (verb == "OPEN")
            {
                if (state.GetFlag("TRAP-DOOR!-FLAG"))
                    state.Output.WriteLine("It's open.");
                else
                {
                    state.Output.WriteLine("The door reluctantly opens to reveal a rickety staircase descending into darkness.");
                    state.SetFlag("TRAP-DOOR!-FLAG", true);
                }
            }
            else if (verb == "CLOSE")
            {
                if (state.GetFlag("TRAP-DOOR!-FLAG"))
                {
                    state.Output.WriteLine("The door swings shut and closes.");
                    state.SetFlag("TRAP-DOOR!-FLAG", false);
                }
                else
                    state.Output.WriteLine("It's closed.");
            }
        }
        else if (state.Here.Id == "CELLA")
        {
            if (verb == "OPEN")
                state.Output.WriteLine("The door is locked from above.");
            else
                state.Output.WriteLine("It's closed.");
        }
    }

    public static void ReadObject(GameState state, GameObject? obj)
    {
        if (obj == null) return;
        if ((obj.Flags & ObjectFlags.Readable) == 0) { state.Output.WriteLine("You can't read that."); return; }
        if (obj.Id == "PAPER")
            state.Output.WriteLine("\"ZORK I: The Great Underground Empire\"\nCopyright (c) 1981, 1982, 1983 Infocom, Inc. All rights reserved.\nZork is a registered trademark of Infocom, Inc.");
        else
            state.Output.WriteLine("Nothing interesting.");
    }

    public static void EatObject(GameState state, GameObject? obj)
    {
        if (obj == null) return;
        if ((obj.Flags & ObjectFlags.Food) == 0) { state.Output.WriteLine("You can't eat that."); return; }
        state.Winner.Inventory.Remove(obj);
        obj.Carrier = null;
        state.Output.WriteLine("Thank you, that was delicious.");
    }

    public static void DrinkObject(GameState state, GameObject? obj)
    {
        if (obj == null) return;
        if ((obj.Flags & ObjectFlags.Drinkable) == 0) { state.Output.WriteLine("You can't drink that."); return; }
        state.Output.WriteLine("You have drunk the water.  The bottle is now empty.");
        if (obj.Container != null) obj.Container.Contents.Remove(obj);
        obj.Container = null;
        state.Winner.Inventory.Remove(obj);
        obj.Carrier = null;
        if (state.Here != null) { obj.InRoom = state.Here; state.Here.Objects.Add(obj); }
    }

    public static void Diagnose(GameState state)
    {
        state.Output.WriteLine("You are in fine health.  Your strength is " + state.Winner.Strength + ".");
    }

    /// <summary>Thief (CHALI): ATTACK/TAKE messages; ROB-ADV steals from player.</summary>
    public static void ThiefFunction(GameState state, string verb, GameObject? _)
    {
        if (verb == "ATTACK" || verb == "ATTAC")
        {
            state.Output.WriteLine("You missed.  The thief makes no attempt to take the knife, though it would be a fine addition to the collection in his bag.  He does seem angered by your attempt.");
            return;
        }
        if (verb == "TAKE")
        {
            state.Output.WriteLine("Once you got him, what would you do with him?");
            return;
        }
    }

    /// <summary>ROB-ADV: remove a random valuable from player and put in thief room (TREAS). Returns true if something was stolen.</summary>
    public static bool RobAdventurer(GameState state, bool printMessage = true)
    {
        var inv = state.Winner.Inventory.Where(o => (o.Flags & ObjectFlags.Take) != 0 && o.FindScore > 0).ToList();
        if (inv.Count == 0) return false;
        var stolen = inv[Random.Shared.Next(inv.Count)];
        state.Winner.Inventory.Remove(stolen);
        stolen.Carrier = null;
        stolen.Container = null;
        var treas = state.World?.FindRoom("TREAS");
        if (treas != null)
        {
            stolen.InRoom = treas;
            treas.Objects.Add(stolen);
        }
        if (printMessage)
            state.Output.WriteLine("A seedy-looking individual with a large bag just wandered through the room.  On the way through, he quietly abstracted all valuables from the room and from your possession, mumbling something about \"Doing unto others before..\"");
        return true;
    }

    /// <summary>Iron box (IRBOX) in carousel: WAVE sets CAROUSEL-FLIP!-FLAG so directions work.</summary>
    public static void IronBoxFunction(GameState state, string verb, GameObject? _)
    {
        if (verb != "WAVE" && verb != "WAVING") return;
        if (state.Here?.Id != "CAROU") return;
        state.SetFlag("CAROUSEL-FLIP!-FLAG", true);
        state.Output.WriteLine("The room spins and you get your bearings.");
    }

    /// <summary>Mirror (REFL1/REFL2): RUB swaps MIRR1/MIRR2 contents and moves player; BREAK/THROW sets MIRROR-MUNG!-FLAG.</summary>
    public static void MirrorFunction(GameState state, string verb, GameObject? _)
    {
        var here = state.Here;
        if (here == null || state.World == null) return;
        var m1 = state.World.FindRoom("MIRR1");
        var m2 = state.World.FindRoom("MIRR2");
        if (m1 == null || m2 == null) return;
        if (verb == "RUB" || verb == "RUBB")
        {
            if (state.GetFlag("MIRROR-MUNG!-FLAG"))
            {
                state.Output.WriteLine("Haven't you done enough already?");
                return;
            }
            var from = here.Id == "MIRR1" ? m1 : m2;
            var to = here.Id == "MIRR1" ? m2 : m1;
            var objsFrom = from.Objects.ToList();
            var objsTo = to.Objects.ToList();
            from.Objects.Clear();
            to.Objects.Clear();
            foreach (var o in objsTo) { o.InRoom = from; from.Objects.Add(o); }
            foreach (var o in objsFrom) { o.InRoom = to; to.Objects.Add(o); }
            state.Winner.CurrentRoom = to;
            state.Output.WriteLine("There is a rumble from deep within the earth and the room shakes.");
            return;
        }
        if (verb == "MUNG" || verb == "BREAK" || verb == "THROW")
        {
            if (state.GetFlag("MIRROR-MUNG!-FLAG"))
            {
                state.Output.WriteLine("Haven't you done enough already?");
                return;
            }
            state.SetFlag("MIRROR-MUNG!-FLAG", true);
            state.Output.WriteLine("You have broken the mirror.  I hope you have a seven years supply of good luck handy.");
        }
        if (verb == "TAKE")
            state.Output.WriteLine("Nobody but a greedy surgeon would allow you to attempt that trick.");
        if (verb == "LOOK" || verb == "EXAMI")
            state.Output.WriteLine(state.GetFlag("MIRROR-MUNG!-FLAG") ? "The mirror is broken into many pieces." : "There is an ugly person staring at you.");
    }

    /// <summary>BALLO (balloon): LOOK shows inflated/draped; BOARD/DISEMBARK handled in parser.</summary>
    public static void BalloonFunction(GameState state, string verb, GameObject? _)
    {
        if (verb != "LOOK" && verb != "EXAMI") return;
        var recep = state.World?.FindObject("RECEP");
        var burning = recep?.Contents.FirstOrDefault(o => o.LightAmount > 0);
        if (state.GetFlag("BINF!-FLAG") && burning != null)
            state.Output.WriteLine("The cloth bag is inflated and there is a " + burning.DescriptionShort + " burning in the receptacle.");
        else
            state.Output.WriteLine("The cloth bag is draped over the basket.");
    }

    /// <summary>STICK (magic): WAVE in FALLS toggles RAINBOW; WAVE in RAINB → rainbow collapses, death.</summary>
    public static void StickFunction(GameState state, string verb, GameObject? _)
    {
        if (verb != "WAVE") return;
        var here = state.Here?.Id;
        if (here == "FALLS")
        {
            if (!state.GetFlag("RAINBOW!-FLAG"))
            {
                state.SetFlag("RAINBOW!-FLAG", true);
                state.Output.WriteLine("Suddenly, the rainbow appears to become solid and, I venture, walkable (I think the giveaway was the stairs and bannister).");
            }
            else
            {
                state.SetFlag("RAINBOW!-FLAG", false);
                state.Output.WriteLine("The rainbow seems to have become somewhat run-of-the-mill.");
            }
        }
        else if (here == "RAINB")
        {
            state.SetFlag("RAINBOW!-FLAG", false);
            state.Output.WriteLine("The structural integrity of the rainbow seems to have left it, leaving you about 450 feet in the air, supported by water vapor.");
            state.Running = false;
        }
        else
            state.Output.WriteLine("Very good.");
    }

    /// <summary>Ghost (GHOST) in LLD1: ATTACK/TAKE → can't affect spirits.</summary>
    public static void GhostFunction(GameState state, string verb, GameObject? _)
    {
        if (verb == "ATTACK" || verb == "ATTAC" || verb == "KILL" || verb == "FIGHT")
            state.Output.WriteLine("How can you attack a spirit with material objects?");
        else if (verb == "TAKE")
            state.Output.WriteLine("You seem unable to affect these spirits.");
    }

    /// <summary>Cyclops (CYCLO): GIVE FOOD → peppers/drink message; GIVE WATER → asleep (CYCLOPS-FLAG); ATTACK/wake; RVARS = state.</summary>
    public static void CyclopsFunction(GameState state, string verb, GameObject? indirect)
    {
        var here = state.Here;
        if (here?.Id != "CYCLO" || state.World == null) return;
        var cyclops = state.World.FindObject("CYCLO");
        if (cyclops == null) return;
        var food = state.World.FindObject("FOOD");
        var water = state.World.FindObject("WATER");
        var count = here.RVars;

        if (state.GetFlag("CYCLOPS-FLAG!-FLAG")) // asleep
        {
            if (verb == "ATTACK" || verb == "ATTAC" || verb == "BURN" || verb == "MUNG" || verb == "BREAK")
            {
                state.Output.WriteLine("The cyclops yawns and stares at the thing that woke him up.");
                state.SetFlag("CYCLOPS-FLAG!-FLAG", false);
                cyclops.Flags |= ObjectFlags.Fighting;
                here.RVars = Math.Abs(count);
                return;
            }
            if (count > 5)
            {
                state.Output.WriteLine("The cyclops, tired of all of your games and trickery, eats you.\nThe cyclops says 'Mmm.  Just like mom used to make 'em.'");
                state.Running = false;
                return;
            }
            if (verb == "GIVE" && indirect != null)
            {
                if (indirect == food)
                {
                    if (count >= 0)
                    {
                        state.Winner.Inventory.Remove(food!);
                        if (food.InRoom != null) food.InRoom.Objects.Remove(food);
                        food.InRoom = null;
                        food.Carrier = null;
                        state.Output.WriteLine("The cyclops says 'Mmm Mmm.  I love hot peppers!  But oh, could I use a drink.  Perhaps I could drink the blood of that thing'.  From the gleam in his eye, it could be surmised that you are 'that thing'.");
                        here.RVars = Math.Max(-1, count - 1);
                    }
                    return;
                }
                if (indirect == water)
                {
                    if (count < 0)
                    {
                        state.Winner.Inventory.Remove(water!);
                        if (water.InRoom != null) water.InRoom.Objects.Remove(water);
                        water.InRoom = null;
                        water.Carrier = null;
                        state.Output.WriteLine("The cyclops looks tired and quickly falls fast asleep (what did you put in that drink, anyway?).");
                        state.SetFlag("CYCLOPS-FLAG!-FLAG", true);
                    }
                    else
                        state.Output.WriteLine("The cyclops apparently was not thirsty at the time and refuses your generous gesture.");
                    return;
                }
                if (indirect == state.World.FindObject("GARLI"))
                {
                    state.Output.WriteLine("The cyclops may be hungry, but there is a limit.");
                    here.RVars = count >= 0 ? count + 1 : Math.Max(-6, count - 1);
                    return;
                }
                state.Output.WriteLine("The cyclops is not so stupid as to eat THAT!");
                here.RVars = count >= 0 ? count + 1 : Math.Max(-6, count - 1);
                return;
            }
            if (verb == "TAKE")
                state.Output.WriteLine("The cyclops is rather heavy and doesn't take kindly to being grabbed.");
            if (verb == "TIE")
                state.Output.WriteLine("You cannot tie the cyclops, although he is fit to be tied.");
            return;
        }

        // Awake
        if (verb == "THROW" || verb == "MUNG" || verb == "BREAK")
        {
            state.Output.WriteLine(Random.Shared.Next(2) == 0
                ? "Your actions don't appear to be doing much harm to the cyclops, but they do not exactly lower your insurance premiums, either."
                : "The cyclops ignores all injury to his body with a shrug.");
            return;
        }
        if (verb == "TAKE")
            state.Output.WriteLine("The cyclops is rather heavy and doesn't take kindly to being grabbed.");
        if (verb == "TIE")
            state.Output.WriteLine("You cannot tie the cyclops, although he is fit to be tied.");
    }
}
