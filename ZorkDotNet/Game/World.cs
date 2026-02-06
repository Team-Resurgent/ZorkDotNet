namespace ZorkDotNet.Game;

/// <summary>
/// Builds and holds the game world (rooms, objects). Data ported from dung.56 and defs.63.
/// </summary>
public class World
{
    private readonly Dictionary<string, Room> _rooms = new();
    private readonly Dictionary<string, GameObject> _objects = new();
    private readonly GameState _state;

    public World(GameState state)
    {
        _state = state;
        BuildWorld();
    }

    public Room? FindRoom(string id) => _rooms.TryGetValue(NormId(id), out var r) ? r : null;
    public GameObject? FindObject(string id) => _objects.TryGetValue(NormId(id), out var o) ? o : null;
    public IEnumerable<Room> Rooms => _rooms.Values;
    public IEnumerable<GameObject> Objects => _objects.Values;

    /// <summary>Optional exit functions (e.g. CAROUSEL-EXIT). When conditional exit is blocked, run this instead.</summary>
    private readonly Dictionary<(string roomId, string direction), Action<GameState, string>> _exitActions = new();
    /// <summary>Runs before evaluating conditional exit (e.g. COFFIN-CURE sets EGYPT-FLAG from inventory).</summary>
    private readonly Dictionary<(string roomId, string direction), Action<GameState>> _beforeExitActions = new();

    public bool TryGetBeforeExitAction(string roomId, string direction, out Action<GameState>? action)
    {
        var key = (NormId(roomId), direction);
        if (_beforeExitActions.TryGetValue(key, out var act))
        {
            action = act;
            return true;
        }
        action = null;
        return false;
    }

    public bool TryGetExitAction(string roomId, string direction, out Action<GameState, string>? action)
    {
        var key = (NormId(roomId), direction);
        if (_exitActions.TryGetValue(key, out var act))
        {
            action = act;
            return true;
        }
        action = null;
        return false;
    }

    private static string NormId(string id) => id.Length > 5 ? id[..5] : id;

    private void BuildWorld()
    {
        // ---- Vocabulary / globals (from dung.56) ----
        _state.SetFlag("KITCHEN-WINDOW!-FLAG", false);
        _state.SetFlag("TRAP-DOOR!-FLAG", false);
        _state.SetFlag("MAGIC-FLAG!-FLAG", false);
        _state.SetFlag("TROLL-FLAG!-FLAG", false);
        _state.SetFlag("KEY-FLAG!-FLAG", false);
        _state.SetFlag("BRIEF!-FLAG", false);
        _state.SetFlag("SUPER-BRIEF!-FLAG", false);
        _state.SetFlag("GLACIER-FLAG!-FLAG", false);
        _state.SetFlag("CAGE-TOP!-FLAG", true); // basket at top of shaft
        _state.SetFlag("RUG-MOVED!-FLAG", false);
        _state.SetFlag("DOME-FLAG!-FLAG", false);
        _state.SetFlag("LLD-FLAG!-FLAG", false);
        _state.SetFlag("LOW-TIDE!-FLAG", false);
        _state.SetFlag("GATE-FLAG!-FLAG", false);
        _state.SetFlag("ON-POLE!-FLAG", false);
        _state.SetFlag("CAROUSEL-FLIP!-FLAG", false);
        _state.SetFlag("EGYPT-FLAG!-FLAG", true);
        _state.SetFlag("MIRROR-MUNG!-FLAG", false);
        _state.SetFlag("CYCLOPS-FLAG!-FLAG", false);
        _state.SetFlag("RAINBOW!-FLAG", false);
        _state.SetFlag("BINF!-FLAG", false);
        _state.SetFlag("IN-BALLOON!-FLAG", false);

        // ---- Rooms (from dung.56 #ROOM) ----
        AddRoom("WHOUS",
            "You are in an open field west of a big white house, with a boarded front door.",
            "West of House",
            true,
            exits: new Dictionary<string, ExitTarget>
            {
                ["NORTH"] = new("NHOUS"),
                ["SOUTH"] = new("SHOUS"),
                ["WEST"] = new("FORE1"),
                ["EAST"] = new ExitTarget("The door is locked, and there is evidently no key.")
            },
            objectIds: new[] { "FDOOR", "MAILB" });

        AddRoom("NHOUS",
            "You are facing the north side of a white house.  There is no door here, and all the windows are barred.",
            "North of House",
            true,
            new Dictionary<string, ExitTarget>
            {
                ["WEST"] = new("WHOUS"),
                ["EAST"] = new("EHOUS"),
                ["NORTH"] = new("FORE3"),
                ["SOUTH"] = new ExitTarget("The windows are all barred.")
            });

        AddRoom("SHOUS",
            "You are facing the south side of a white house. There is no door here, and all the windows are barred.",
            "South of House",
            true,
            new Dictionary<string, ExitTarget>
            {
                ["WEST"] = new("WHOUS"),
                ["EAST"] = new("EHOUS"),
                ["SOUTH"] = new("FORE2"),
                ["NORTH"] = new ExitTarget("The windows are all barred.")
            });

        AddRoom("EHOUS",
            "",
            "Behind House",
            true,
            new Dictionary<string, ExitTarget>
            {
                ["NORTH"] = new("NHOUS"),
                ["SOUTH"] = new("SHOUS"),
                ["EAST"] = new("CLEAR"),
                ["WEST"] = new ExitTarget("KITCHEN-WINDOW!-FLAG", "KITCH", "The kitchen window is closed."),
                ["ENTER"] = new ExitTarget("KITCHEN-WINDOW!-FLAG", "KITCH", "The kitchen window is closed.")
            },
            objectIds: new[] { "WIND1" },
            roomAction: RoomActions.EastHouse);

        AddRoom("KITCH",
            "",
            "Kitchen",
            true,
            new Dictionary<string, ExitTarget>
            {
                ["EAST"] = new ExitTarget("KITCHEN-WINDOW!-FLAG", "EHOUS", null),
                ["EXIT"] = new ExitTarget("KITCHEN-WINDOW!-FLAG", "EHOUS", null),
                ["WEST"] = new("LROOM"),
                ["UP"] = new("ATTIC"),
                ["DOWN"] = new ExitTarget("Only Santa Claus climbs down chimneys.")
            },
            objectIds: new[] { "WIND2", "SBAG", "BOTTL" },
            roomAction: RoomActions.Kitchen,
            visitScore: 10);

        AddRoom("ATTIC",
            "You are in the attic.  The only exit is stairs that lead down.",
            "Attic",
            false,
            new Dictionary<string, ExitTarget> { ["DOWN"] = new("KITCH") },
            objectIds: new[] { "BRICK", "ROPE", "KNIFE" });

        AddRoom("LROOM",
            "",
            "Living Room",
            true,
            new Dictionary<string, ExitTarget>
            {
                ["EAST"] = new("KITCH"),
                ["WEST"] = new ExitTarget("MAGIC-FLAG!-FLAG", "BLROO", "The door is nailed shut."),
                ["DOWN"] = new ExitTarget("TRAP-DOOR!-FLAG", "CELLA", "The trap door is closed.")
            },
            objectIds: new[] { "WDOOR", "DOOR", "TCASE", "LAMP", "RUG", "PAPER", "SWORD" },
            roomAction: RoomActions.LivingRoom);

        AddRoom("CELLA",
            "",
            "Cellar",
            false,
            new Dictionary<string, ExitTarget>
            {
                ["EAST"] = new("MTROL"),
                ["SOUTH"] = new("CHAS2"),
                ["UP"] = new ExitTarget("TRAP-DOOR!-FLAG", "LROOM", "The trap door has been barred from the other side."),
                ["WEST"] = new ExitTarget("You try to ascend the ramp, but it is impossible, and you slide back down.")
            },
            objectIds: new[] { "TDOOR" },
            visitScore: 25,
            onEnter: RoomActions.OnEnterCellar);

        const string mazeDesc = "You are in a maze of twisty little passages, all alike.";
        const string deadEnd = "Dead End";
        AddRoom("MTROL",
            "You are in a small room with passages off in all directions. Bloodstains and deep scratches (perhaps made by an axe) mar the walls.",
            "The Troll Room",
            false,
            new Dictionary<string, ExitTarget>
            {
                ["WEST"] = new("CELLA"),
                ["EAST"] = new ExitTarget("TROLL-FLAG!-FLAG", "CRAW4", "The troll fends you off with a menacing gesture."),
                ["NORTH"] = new ExitTarget("TROLL-FLAG!-FLAG", "PASS1", "The troll fends you off with a menacing gesture."),
                ["SOUTH"] = new ExitTarget("TROLL-FLAG!-FLAG", "MAZE1", "The troll fends you off with a menacing gesture.")
            },
            objectIds: new[] { "TROLL" });

        AddRoom("PASS1",
            "You are in a narrow east-west passageway.  There is a narrow stairway leading down at the north end of the room.",
            "East-West Passage",
            false,
            new Dictionary<string, ExitTarget>
            {
                ["EAST"] = new("CAROU"),
                ["WEST"] = new("MTROL"),
                ["DOWN"] = new("RAVI1"),
                ["NORTH"] = new("RAVI1")
            },
            visitScore: 5);

        AddRoom("MAZE1", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("MTROL"), ["NORTH"] = new("MAZE1"), ["SOUTH"] = new("MAZE2"), ["EAST"] = new("MAZE4") });
        AddRoom("MAZE2", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("MAZE1"), ["NORTH"] = new("MAZE4"), ["EAST"] = new("MAZE3") });
        AddRoom("MAZE3", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("MAZE2"), ["NORTH"] = new("MAZE4"), ["UP"] = new("MAZE5") });
        AddRoom("MAZE4", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("MAZE3"), ["NORTH"] = new("MAZE1"), ["EAST"] = new("DEAD1") });
        AddRoom("DEAD1", deadEnd, deadEnd, false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("MAZE4") });
        AddRoom("MAZE5", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("DEAD2"), ["NORTH"] = new("MAZE3"), ["SW"] = new("MAZE6") },
            objectIds: new[] { "BONES", "BAGCO", "KEYS", "BLANT", "RKNIF" });
        AddRoom("DEAD2", deadEnd, deadEnd, false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("MAZE5") });

        const string forestDesc = "You are in a forest, with trees in all directions around you.";
        AddRoom("FORE1", forestDesc, "Forest", true,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("FORE1"), ["EAST"] = new("FORE3"), ["SOUTH"] = new("FORE2"), ["WEST"] = new("FORE1") });
        AddRoom("FORE2", "You are in a dimly lit forest, with large trees all around.  To the east, there appears to be sunlight.", "Forest", true,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("SHOUS"), ["EAST"] = new("CLEAR"), ["SOUTH"] = new("FORE4"), ["WEST"] = new("FORE1") });
        AddRoom("FORE3", "You are in a dimly lit forest, with large trees all around.  To the east, there appears to be sunlight.", "Forest", true,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("FORE2"), ["EAST"] = new("CLEAR"), ["SOUTH"] = new("CLEAR"), ["WEST"] = new("NHOUS") });
        AddRoom("FORE4", "You are in a large forest, with trees obstructing all views except to the east, where a small clearing may be seen through the trees.", "Forest", true,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("CLTOP"), ["NORTH"] = new("FORE5"), ["SOUTH"] = new("FORE4"), ["WEST"] = new("FORE2") });
        AddRoom("FORE5", forestDesc, "Forest", true,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("FORE5"), ["SE"] = new("CLTOP"), ["SOUTH"] = new("FORE4"), ["WEST"] = new("FORE2") });
        AddRoom("CLEAR", "", "Clearing", true,
            new Dictionary<string, ExitTarget>
            {
                ["SW"] = new("EHOUS"),
                ["SE"] = new("FORE5"),
                ["NORTH"] = new("CLEAR"),
                ["EAST"] = new("CLEAR"),
                ["WEST"] = new("FORE3"),
                ["SOUTH"] = new("FORE2"),
                ["DOWN"] = new ExitTarget("KEY-FLAG!-FLAG", "MGRAT", "The grating is locked")
            },
            objectIds: new[] { "GRAT1", "LEAVE" },
            roomAction: RoomActions.Clearing);

        AddRoom("RAVI1",
            "You are in a deep ravine at a crossing with an east-west crawlway. Some stone steps are at the south of the ravine and a steep staircase descends.",
            "Deep Ravine",
            false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("PASS1"), ["DOWN"] = new("RESES"), ["EAST"] = new("CHAS1"), ["WEST"] = new("CRAW1") });

        AddRoom("CHAS2",
            "You are on the west edge of a chasm, the bottom of which cannot be seen. The east side is sheer rock, providing no exits.  A narrow passage goes west, and the path you are on continues to the north and south.",
            "West of Chasm",
            false,
            new Dictionary<string, ExitTarget>
            {
                ["WEST"] = new("CELLA"),
                ["NORTH"] = new("CRAW4"),
                ["SOUTH"] = new("GALLE"),
                ["DOWN"] = new ExitTarget("The chasm probably leads straight to the infernal regions.")
            });

        AddRoom("CRAW4",
            "You are in a north-south crawlway; a passage goes to the east also. There is a hole above, but it provides no opportunities for climbing.",
            "North-South Crawlway",
            false,
            new Dictionary<string, ExitTarget>
            {
                ["NORTH"] = new("CHAS2"),
                ["SOUTH"] = new("STUDI"),
                ["EAST"] = new("MTROL"),
                ["UP"] = new ExitTarget("Not even a human fly could get up it.")
            });

        AddRoom("CAROU", "", "Round room", false,
            new Dictionary<string, ExitTarget>
            {
                ["NORTH"] = new ExitTarget("CAROUSEL-FLIP!-FLAG", "CAVE4", null),
                ["SOUTH"] = new ExitTarget("CAROUSEL-FLIP!-FLAG", "CAVE4", null),
                ["EAST"] = new ExitTarget("CAROUSEL-FLIP!-FLAG", "MGRAI", null),
                ["WEST"] = new ExitTarget("CAROUSEL-FLIP!-FLAG", "PASS1", null),
                ["NW"] = new ExitTarget("CAROUSEL-FLIP!-FLAG", "CANY1", null),
                ["NE"] = new ExitTarget("CAROUSEL-FLIP!-FLAG", "PASS5", null),
                ["SE"] = new ExitTarget("CAROUSEL-FLIP!-FLAG", "PASS4", null),
                ["SW"] = new ExitTarget("CAROUSEL-FLIP!-FLAG", "MAZE1", null)
            },
            objectIds: new[] { "IRBOX" },
            roomAction: RoomActions.CarouselRoom);
        RegisterCarouselExitActions();
        RegisterCoffinCure();

        AddRoom("CANY1",
            "You are on the south edge of a deep canyon.  Passages lead off to the east, south, and northwest.  You can hear the sound of flowing water below.",
            "Deep Canyon",
            false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("FALLS"), ["NW"] = new("RESES"), ["EAST"] = new("DAM"), ["SOUTH"] = new("CAROU") });
        AddRoom("FALLS", "", "Aragain Falls", false,
            new Dictionary<string, ExitTarget>
            {
                ["NORTH"] = new("FANTE"),
                ["SOUTH"] = new("CANY1"),
                ["EAST"] = new ExitTarget("RAINBOW!-FLAG", "RAINB", "The rainbow is too insubstantial to walk on."),
                ["UP"] = new ExitTarget("RAINBOW!-FLAG", "RAINB", "The rainbow is too insubstantial to walk on."),
                ["DOWN"] = new("FCHMP"),
                ["ENTER"] = new("BARRE"),
                ["DOWN"] = new ExitTarget("Oh dear, you seem to have gone over Aragain Falls.  Not a very smart thing to do, apparently."),
                ["NORTH"] = new ExitTarget("A narrow path leads north along the falls.")
            },
            roomAction: RoomActions.FallsRoom);
        AddRoom("RAINB",
            "You are on top of a rainbow (I bet you never thought you would walk on a rainbow), with a magnificent view of the Falls.  The rainbow travels east-west here.",
            "Rainbow Room",
            false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("POG"), ["WEST"] = new("FALLS") });

        AddRoom("MGRAT", "", "Grating Room", false,
            new Dictionary<string, ExitTarget>
            {
                ["SW"] = new("MAZ11"),
                ["UP"] = new ExitTarget("KEY-FLAG!-FLAG", "CLEAR", "The grating is locked")
            },
            objectIds: new[] { "GRAT2" });

        AddRoom("MAZ11", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["NE"] = new("MGRAT"), ["DOWN"] = new("MAZ10"), ["NW"] = new("MAZ13"), ["SW"] = new("MAZ12") });
        AddRoom("MAZ10", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("MAZE9"), ["WEST"] = new("MAZ13"), ["UP"] = new("MAZ11") });
        AddRoom("MAZ12", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("MAZE5"), ["SW"] = new("MAZ11"), ["EAST"] = new("MAZ13"), ["UP"] = new("MAZE9"), ["NORTH"] = new("DEAD4") });
        AddRoom("DEAD4", deadEnd, deadEnd, false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("MAZ12") });
        AddRoom("MAZ13", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("MAZE9"), ["DOWN"] = new("MAZ12"), ["SOUTH"] = new("MAZ10"), ["WEST"] = new("MAZ11") });
        AddRoom("MAZE6", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["DOWN"] = new("MAZE5"), ["EAST"] = new("MAZE7"), ["WEST"] = new("MAZE6"), ["UP"] = new("MAZE9") });
        AddRoom("MAZE7", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["UP"] = new("MAZ14"), ["WEST"] = new("MAZE6"), ["NE"] = new("DEAD1"), ["EAST"] = new("MAZE8"), ["SOUTH"] = new("MAZ15") });
        AddRoom("MAZE8", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["NE"] = new("MAZE7"), ["WEST"] = new("MAZE8"), ["SE"] = new("DEAD3") });
        AddRoom("DEAD3", deadEnd, deadEnd, false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("MAZE8") });
        AddRoom("MAZE9", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("MAZE6"), ["EAST"] = new("MAZ11"), ["DOWN"] = new("MAZ10"), ["SOUTH"] = new("MAZ13"), ["WEST"] = new("MAZ12"), ["NW"] = new("MAZE9") });
        AddRoom("MAZ14", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("MAZ15"), ["NW"] = new("MAZ14"), ["NE"] = new("MAZE7"), ["SOUTH"] = new("MAZE7") });
        AddRoom("MAZ15", mazeDesc, mazeDesc, false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("MAZ14"), ["SOUTH"] = new("MAZE7"), ["NE"] = new("CYCLO") });

        AddRoom("RESES", "", "Reservoir South", false,
            new Dictionary<string, ExitTarget>
            {
                ["WEST"] = new("STREA"),
                ["SOUTH"] = new ExitTarget("EGYPT-FLAG!-FLAG", "RAVI1", "The coffin will not fit through this passage."),
                ["UP"] = new ExitTarget("EGYPT-FLAG!-FLAG", "CANY1", "The stairs are too steep for carrying the coffin."),
                ["NORTH"] = new ExitTarget("LOW-TIDE!-FLAG", "RESEN", "You are not equipped for swimming."),
                ["CROSS"] = new ExitTarget("LOW-TIDE!-FLAG", "RESEN", "You are not equipped for swimming.")
            },
            objectIds: new[] { "TRUNK" });
        AddRoom("STREA",
            "You are standing on a path beside a flowing stream.  The path travels to the north and the east.",
            "Stream",
            false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("RESES"), ["NORTH"] = new("ICY") },
            objectIds: new[] { "FUSE" });
        AddRoom("EGYPT",
            "You are in a room which looks like an Egyptian tomb.  There is an ascending staircase in the room as well as doors, east and south.",
            "Egyptian Room",
            false,
            new Dictionary<string, ExitTarget>
            {
                ["UP"] = new("ICY"),
                ["SOUTH"] = new("LEDG3"),
                ["EAST"] = new ExitTarget("EGYPT-FLAG!-FLAG", "CRAW1", "The passage is too narrow to accomodate coffins.")
            },
            objectIds: new[] { "COFFI" });
        AddRoom("ICY", "", "Glacier Room", false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("STREA"), ["EAST"] = new("EGYPT"), ["WEST"] = new ExitTarget("GLACIER-FLAG!-FLAG", "RUBYR", "The glacier blocks your way.") },
            objectIds: new[] { "ICE" },
            roomAction: RoomActions.GlacierRoom);
        AddRoom("RUBYR",
            "You are in a small chamber behind the remains of the Great Glacier. To the south and west are small passageways.",
            "Ruby Room",
            false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("LAVA"), ["SOUTH"] = new("ICY") },
            objectIds: new[] { "RUBY" });
        AddRoom("CHAS1", "", "East of Chasm", false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("RAVI1") });
        AddRoom("CRAW1",
            "You are in a crawlway with a three-foot high ceiling.  Your footing is very unsure here due to the assortment of rocks underfoot. Passages can be seen in the east, west, and northwest corners of the passage.",
            "Rocky Crawl",
            false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("RAVI1"), ["EAST"] = new("DOME"), ["NW"] = new("EGYPT") });
        AddRoom("DOME", "", "Dome Room", false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("CRAW1") },
            objectIds: new[] { "RAILI" });
        AddRoom("PASS5", "You are in a cold and damp corridor where a long east-west passageway intersects with a northward path.", "Cold Passage", false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("MIRR2"), ["WEST"] = new("SLIDE"), ["NORTH"] = new("CRAW2") });
        AddRoom("SLIDE",
            "You are in a small chamber, which appears to have been part of a coal mine. On the south wall of the chamber the letters \"Granite Wall\" are etched in the rock. To the east is a long passage and there is a steep metal slide twisting downward. From the appearance of the slide, an attempt to climb up it would be impossible.  To the north is a small opening.",
            "Slide Room",
            false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("PASS3"), ["DOWN"] = new("CELLA"), ["NORTH"] = new("ENTRA") });
        AddRoom("PASS3",
            "You are in a cold and damp corridor where a long east-west passageway intersects with a northward path.",
            "Cold Passage",
            false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("MIRR1"), ["WEST"] = new("SLIDE"), ["NORTH"] = new("CRAW2") });
        AddRoom("ENTRA",
            "You are standing at the entrance of what might have been a coal mine. To the northeast and the northwest are entrances to the mine, and there is another exit on the south end of the room.",
            "Mine Entrance",
            false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("SLIDE"), ["NW"] = new("SQUEE"), ["NE"] = new("TSHAF") });
        AddRoom("SQUEE",
            "You are a small room.  Strange squeaky sounds may be heard coming from the passage at the west end.  You may also escape to the south.",
            "Squeaky Room",
            false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("BATS"), ["SOUTH"] = new("ENTRA") });
        AddRoom("TSHAF",
            "You are in a large room, in the middle of which is a small shaft descending through the floor into darkness below.  To the west and the north are exits from this room.  Constructed over the top of the shaft is a metal framework to which a heavy iron chain is attached.",
            "Shaft Room",
            false,
            new Dictionary<string, ExitTarget>
            {
                ["DOWN"] = new ExitTarget("You wouldn't fit and would die if you could."),
                ["WEST"] = new("ENTRA"),
                ["NORTH"] = new("TUNNE")
            },
            objectIds: new[] { "TBASK" });
        AddRoom("TUNNE",
            "You are in a narrow tunnel with large wooden beams running across the ceiling and around the walls.  A path from the south splits into paths running west and northeast.",
            "Wooden Tunnel",
            false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("TSHAF"), ["WEST"] = new("SMELL"), ["NE"] = new("MINE1") });
        AddRoom("SMELL",
            "You are in a small non-descript room.  However, from the direction of a small descending staircase a foul odor can be detected.  To the east is a narrow path.",
            "Smelly Room",
            false,
            new Dictionary<string, ExitTarget> { ["DOWN"] = new("BOOM"), ["EAST"] = new("TUNNE") });
        AddRoom("BOOM",
            "You are in a small room which smells strongly of coal gas.",
            "Gas Room",
            false,
            new Dictionary<string, ExitTarget> { ["UP"] = new("SMELL") },
            objectIds: new[] { "BRACE" },
            roomAction: RoomActions.BoomRoom,
            onEnter: RoomActions.OnEnterBoom);
        AddRoom("BATS", "", "Bat Room", false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("SQUEE") },
            objectIds: new[] { "JADE", "BAT" },
            roomAction: RoomActions.BatsRoom,
            onEnter: RoomActions.OnEnterBats);
        AddRoom("MIRR1", "", "Mirror Room", false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("PASS3"), ["NORTH"] = new("CRAW2"), ["EAST"] = new("CAVE1") },
            objectIds: new[] { "REFL1" },
            roomAction: RoomActions.MirrorRoom);
        AddRoom("MIRR2", "", "Mirror Room", true,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("PASS4"), ["NORTH"] = new("CRAW3"), ["EAST"] = new("CAVE2") },
            objectIds: new[] { "REFL2" },
            roomAction: RoomActions.MirrorRoom);
        AddRoom("CAVE1",
            "You are in a small cave with an entrance to the north and a stairway leading down.",
            "Cave",
            false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("MIRR1"), ["DOWN"] = new("ATLAN") });
        AddRoom("CAVE2",
            "You are in a tiny cave with entrances west and north, and a dark, forbidding staircase leading down.",
            "Cave",
            false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("CRAW3"), ["WEST"] = new("MIRR2"), ["DOWN"] = new("LLD1") });
        AddRoom("CRAW2",
            "You are in a steep and narrow crawlway.  There are two exits nearby to the south and southwest.",
            "Steep Crawlway",
            false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("MIRR1"), ["SW"] = new("PASS3") });
        AddRoom("CRAW3",
            "You are in a narrow crawlway.  The crawlway leads from north to south. However the south passage divides to the south and southwest.",
            "Narrow Crawlway",
            false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("CAVE2"), ["SW"] = new("MIRR2"), ["NORTH"] = new("MGRAI") });
        AddRoom("PASS4",
            "You are in a winding passage.  It seems that there is only an exit on the east end although the whirring from the round room can be heard faintly to the north.",
            "Winding Passage",
            false,
            new Dictionary<string, ExitTarget>
            {
                ["EAST"] = new("MIRR2"),
                ["NORTH"] = new ExitTarget("You hear the whir of the carousel room but can find no entrance.")
            });
        AddRoom("CAVE4", "", "Cave", false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("CAROU") });
        AddRoom("MGRAI", "You are standing in a small circular room with a pedestal.  A set of stairs leads up, and passages leave to the east and west.", "Grail Room", false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("CRAW3"), ["WEST"] = new("CAROU"), ["EAST"] = new("CRAW3"), ["UP"] = new("TEMP1") });
        AddRoom("DAM", "", "Dam", false,
            new Dictionary<string, ExitTarget>
            {
                ["SOUTH"] = new("CANY1"),
                ["DOWN"] = new("DOCK"),
                ["EAST"] = new("CAVE3"),
                ["NORTH"] = new("LOBBY")
            },
            roomAction: RoomActions.DamRoom);
        AddRoom("LOBBY",
            "This room appears to have been the waiting room for groups touring the dam.  There are exits here to the north and east marked 'Private', though the doors are open, and an exit to the south.",
            "Dam Lobby",
            false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("DAM"), ["NORTH"] = new("MAINT"), ["EAST"] = new("MAINT") });
        AddRoom("MAINT",
            "You are in what appears to have been the maintenance room for Flood Control Dam #3, judging by the assortment of tool chests around the room.  Apparently, this room has been ransacked recently, for most of the valuable equipment is gone. On the wall in front of you is a panel of buttons, which are labelled in EBCDIC. However, they are of different colors:  Blue, Yellow, Brown, and Red. The doors to this room are in the west and south ends.",
            "Maintenance Room",
            false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("LOBBY"), ["WEST"] = new("LOBBY") });
        AddRoom("DOCK",
            "You are at the base of Flood Control Dam #3, which looms above you and to the north.  The river Frigid is flowing by here.  Across the river are the White Cliffs which seem to form a giant wall stretching from north to south along the east shore of the river as it winds its way downstream.",
            "Dam Base",
            false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("DAM"), ["UP"] = new("DAM"), ["SOUTH"] = new("RIVR1") });
        AddRoom("TEMP1",
            "You are in the west end of a large temple.  On the south wall is an ancient inscription, probably a prayer in a long-forgotten language. The north wall is solid granite.  The entrance at the west end of the room is through huge marble pillars.",
            "Temple",
            false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("MGRAI"), ["EAST"] = new("TEMP2") });
        AddRoom("TEMP2",
            "You are in the east end of a large temple.  In front of you is what appears to be an altar.",
            "Altar",
            false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("TEMP1") });
        AddRoom("LAVA",
            "You are in a small room, whose walls are formed by an old lava flow. There are exits here to the west and the south.",
            "Lava Room",
            false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("VLBOT"), ["WEST"] = new("RUBYR") });
        AddRoom("VLBOT",
            "You are at the bottom of a large dormant volcano.  High above you light may be seen entering from the cone of the volcano.  The only exit here is to the north.",
            "Volcano Bottom",
            false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("LAVA") });
        AddRoom("LEDG3",
            "You are on a ledge in the middle of a large volcano.  Below you the volcano bottom can be seen and above is the rim of the volcano. A couple of ledges can be seen on the other side of the volcano; it appears that this ledge is intermediate in elevation between those on the other side.  The exit from this room is to the east.",
            "Volcano View",
            false,
            new Dictionary<string, ExitTarget>
            {
                ["EAST"] = new("EGYPT"),
                ["DOWN"] = new ExitTarget("I wouldn't try that."),
                ["CROSS"] = new ExitTarget("It is impossible to cross this distance.")
            });
        AddRoom("LEDG4", "", "Wide Ledge", false,
            new Dictionary<string, ExitTarget>
            {
                ["SOUTH"] = new("SAFE"),
                ["UP"] = new ExitTarget("IN-BALLOON!-FLAG", "VAIR1", "You need to be in the balloon to go up."),
                ["DOWN"] = new ExitTarget("It's a long way down."),
                ["WEST"] = new ExitTarget("GNOME-DOOR!-FLAG", "VLBOT", "The gnome blocks your way.")
            });
        AddRoom("VAIR1",
            "You are in the basket of a hot-air balloon.  The balloon is in the air above the ledge.",
            "Balloon (in air)",
            false,
            new Dictionary<string, ExitTarget> { ["DOWN"] = new("LEDG4"), ["UP"] = new("VAIR2") });
        AddRoom("SAFE", "", "Dusty Room", true,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("LEDG4") },
            roomAction: RoomActions.SafeRoom);
        AddRoom("STUDI", "", "Studio", false, new Dictionary<string, ExitTarget> { ["NORTH"] = new("CRAW4") });
        AddRoom("GALLE", "", "Gallery", false, new Dictionary<string, ExitTarget> { ["NORTH"] = new("CHAS2") });
        AddRoom("CYCLO", "", "Cyclops", false,
            new Dictionary<string, ExitTarget>
            {
                ["WEST"] = new("MAZ15"),
                ["NORTH"] = new ExitTarget("MAGIC-FLAG!-FLAG", "BLROO", "The north wall is solid rock."),
                ["UP"] = new ExitTarget("CYCLOPS-FLAG!-FLAG", "TREAS", "The cyclops doesn't look like he'll let you past.")
            },
            objectIds: new[] { "CYCLO" },
            roomAction: RoomActions.CyclopsRoom);
        AddRoom("BLROO", "You are in a long passage.  To the south is one entrance.  On the east there is an old wooden door, with a large hole in it (about cyclops sized).", "Strange Passage", false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("CYCLO"), ["EAST"] = new("LROOM") });
        AddRoom("CLTOP",
            "You are at the top of the Great Canyon on its south wall.  From here there is a marvelous view of the Canyon and parts of the Frigid River upstream.  Across the canyon, the walls of the White Cliffs still appear to loom far above.  Following the Canyon upstream (north and northwest), Aragain Falls may be seen, complete with rainbow.  Fortunately, my vision is better than average and I can discern the top of the Flood Control Dam #3 far to the distant north.  To the west and south can be seen an immense forest, stretching for miles around.  It is possible to climb down into the canyon from here.",
            "Canyon View",
            false,
            new Dictionary<string, ExitTarget> { ["DOWN"] = new("CLMID"), ["CLIMB"] = new("CLMID"), ["SOUTH"] = new("FORE4"), ["WEST"] = new("FORE5") });
        AddRoom("CLMID",
            "You are on a ledge about halfway up the wall of the river canyon.  You can see from here that the main flow from Aragain Falls twists along a passage which it is impossible to enter.  Below you is the canyon bottom.  Above you is more cliff, which still appears climbable.",
            "Rocky Ledge",
            false,
            new Dictionary<string, ExitTarget> { ["UP"] = new("CLTOP"), ["CLIMB"] = new("CLTOP"), ["DOWN"] = new("CLBOT") });
        AddRoom("CLBOT",
            "You are beneath the walls of the river canyon which may be climbable here.  There is a small stream here, which is the lesser part of the runoff of Aragain Falls.  To the north is a narrow path.",
            "Canyon Bottom",
            false,
            new Dictionary<string, ExitTarget> { ["UP"] = new("CLMID"), ["CLIMB"] = new("CLMID"), ["NORTH"] = new("POG") });
        AddRoom("POG",
            "You are on a small beach on the continuation of the Frigid River past the Falls.  The beach is narrow due to the presence of the White Cliffs.  The river canyon opens here and sunlight shines in from above.  A rainbow crosses over the falls to the west and a narrow path continues to the southeast.",
            "End of Rainbow",
            false,
            new Dictionary<string, ExitTarget> { ["SE"] = new("CLBOT"), ["NORTH"] = new("FANTE") });
        AddRoom("FANTE",
            "You are on the shore of the River.  The river here seems somewhat treacherous.  A path travels from north to south here, the south end quickly turning around a sharp corner.",
            "Shore",
            false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("BEACH"), ["SOUTH"] = new("FALLS") });
        AddRoom("BEACH",
            "You are on a large sandy beach at the shore of the river, which is flowing quickly by.  A path runs beside the river to the south here.",
            "Sandy Beach",
            false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("FANTE") });
        AddRoom("VAIR2", "You are in the basket of a hot-air balloon.  The balloon is in the air.", "Balloon (in air)", false,
            new Dictionary<string, ExitTarget> { ["UP"] = new("VAIR3"), ["DOWN"] = new("VAIR1") });
        AddRoom("VAIR3", "You are in the basket of a hot-air balloon.  The balloon is in the air.", "Balloon (in air)", false,
            new Dictionary<string, ExitTarget> { ["UP"] = new("VAIR4"), ["DOWN"] = new("VAIR2") });
        AddRoom("VAIR4", "You are in the basket of a hot-air balloon.  The balloon is in the air near the rim of the volcano.", "Balloon (in air)", false,
            new Dictionary<string, ExitTarget> { ["DOWN"] = new("VAIR3") });
        AddRoom("LEDG2",
            "You are on a narrow ledge overlooking the inside of an old dormant volcano.  This ledge appears to be about in the middle between the floor below and the rim above.  There is an exit here to the south.",
            "Narrow Ledge",
            false,
            new Dictionary<string, ExitTarget>
            {
                ["SOUTH"] = new("LIBRA"),
                ["WEST"] = new ExitTarget("GNOME-DOOR!-FLAG", "VLBOT", "The gnome blocks your way."),
                ["UP"] = new ExitTarget("IN-BALLOON!-FLAG", "VAIR2", "You need to be in the balloon to go up.")
            });
        AddRoom("LIBRA",
            "You are in a room which must have been a large library, probably for the royal family.  All of the shelves appear to have been gnawed to pieces by unfriendly gnomes.  To the north is an exit.",
            "Library",
            false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("LEDG2"), ["OUT"] = new("LEDG2") });
        AddRoom("MAGNE",
            "You are in a low room.  Exits lead in many directions.",
            "Low Room",
            false,
            new Dictionary<string, ExitTarget>
            {
                ["NORTH"] = new("CMACH"), ["SOUTH"] = new("CMACH"), ["EAST"] = new("CMACH"), ["WEST"] = new("CMACH"),
                ["NE"] = new("CMACH"), ["NW"] = new("ALICE"), ["SW"] = new("ALICE"), ["SE"] = new("ALICE")
            });
        AddRoom("CAGED", "You are trapped inside an iron cage.", "Cage", false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new ExitTarget("The cage door is closed.") });
        AddRoom("ALISM",
            "You are in an enormous room, in the center of which are four wooden posts delineating a rectangular area, above which is what appears to be a wooden roof.  In fact, all objects in this room appear to be abnormally large.  To the east is a passageway.  There is a large chasm on the west and the northwest.",
            "Posts Room",
            false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("ALITR") });
        AddRoom("ALITR",
            "You are in a large room, one half of which is depressed.  There is a large leak in the ceiling through which brown colored goop is falling.  The only exit to this room is to the west.",
            "Pool Room",
            false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("ALISM") });
        AddRoom("RIVR1", "You are on the River Frigid in the vicinity of the Dam.  The river flows quietly here.  There is a landing on the west shore.", "Frigid River", false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("DOCK"), ["DOWN"] = new("RIVR2") });
        AddRoom("RIVR2", "The River turns a corner here making it impossible to see the Dam.  The White Cliffs loom on the east bank and large rocks prevent landing on the west.", "Frigid River", false,
            new Dictionary<string, ExitTarget> { ["DOWN"] = new("RIVR3"), ["UP"] = new("RIVR1") });
        AddRoom("RIVR3", "The river descends here into a valley.  There is a narrow beach on the east below the cliffs and there is some shore on the west which may be suitable.  In the distance a faint rumbling can be heard.", "Frigid River", false,
            new Dictionary<string, ExitTarget> { ["DOWN"] = new("RIVR4"), ["UP"] = new("RIVR2"), ["EAST"] = new("WCLF1"), ["WEST"] = new("RCAVE") });
        AddRoom("RIVR4", "The river is running faster here and the sound ahead appears to be that of rushing water.  On the west shore is a sandy beach.  A small area of beach can also be seen below the Cliffs.", "Frigid River", false,
            new Dictionary<string, ExitTarget> { ["DOWN"] = new("RIVR5"), ["UP"] = new("RIVR3"), ["EAST"] = new("WCLF2"), ["WEST"] = new("BEACH") });
        AddRoom("RIVR5", "The sound of rushing water is nearly unbearable here.  On the west shore is a large landing area.", "Frigid River", false,
            new Dictionary<string, ExitTarget> { ["UP"] = new("RIVR4"), ["WEST"] = new("FANTE") });
        AddRoom("WCLF1", "You are on a narrow strip of beach which runs along the base of the White Cliffs.  The only path here is a narrow one, heading south along the Cliffs.", "White Cliffs Beach", false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("WCLF2"), ["WEST"] = new("RIVR3") });
        AddRoom("WCLF2", "You are on a rocky, narrow strip of beach beside the Cliffs.  A narrow path leads north along the shore.", "White Cliffs Beach", false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("WCLF1"), ["WEST"] = new("RIVR4") });
        AddRoom("FCHMP", "You have gone over Aragain Falls.  Not a very smart thing to do, apparently.", "Moby lossage", false,
            new Dictionary<string, ExitTarget>());
        AddRoom("RCAVE", "You are on the west shore of the river.  An entrance to a cave is to the northwest.  The shore is very rocky here.", "Rocky Shore", false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("RIVR3"), ["NW"] = new("TCAVE") });
        AddRoom("BARRE", "You are in a barrel.  Congratulations.  Etched into the side of the barrel is the word 'Geronimo!'", "Barrel", false,
            new Dictionary<string, ExitTarget> { ["EXIT"] = new("FALLS"), ["OUT"] = new("FALLS") });
        AddRoom("ATLAN", "You are in an ancient room, long buried by the Reservoir.  There are exits here to the southeast and upward.", "Atlantis Room", false,
            new Dictionary<string, ExitTarget> { ["SE"] = new("RESEN"), ["UP"] = new("CAVE1") },
            objectIds: new[] { "TRIDE" });
        AddRoom("RESEN", "", "Reservoir North", false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("ATLAN") },
            objectIds: new[] { "PUMP" });
        AddRoom("LLD1", "", "Entrance to Hades", false,
            new Dictionary<string, ExitTarget>
            {
                ["UP"] = new("CAVE2"),
                ["EAST"] = new ExitTarget("LLD-FLAG!-FLAG", "LLD2", "Some invisible force prevents you from passing through the gate."),
                ["ENTER"] = new ExitTarget("LLD-FLAG!-FLAG", "LLD2", "Some invisible force prevents you from passing through the gate.")
            },
            roomAction: RoomActions.LldRoom);
        AddRoom("LLD2", "", "Land of the Living Dead", false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("TOMB"), ["WEST"] = new("LLD1") },
            roomAction: RoomActions.Lld2Room);
        AddRoom("TOMB",
            "You are in the Tomb of the Unknown Implementer. A hollow voice says:  \"That's not a bug, it's a feature!\"",
            "Tomb of the Unknown Implementer",
            false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("LLD2") });
        AddRoom("TREAS", "", "Treasury", false, new Dictionary<string, ExitTarget>());
        AddRoom("MPEAR",
            "This is a former broom closet.  The exits are to the east and west.",
            "Pearl Room",
            false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("BWELL"), ["WEST"] = new("RIDDL") });
        AddRoom("RIDDL",
            "This is a room which is bare on all sides.  There is an exit down.  To the east is a great door made of stone.  Above the stone, the following words are written: 'No man shall enter this room without solving this riddle: What is tall as a house, round as a cup, and all the king's horses can't draw it up?'",
            "Riddle Room",
            false,
            new Dictionary<string, ExitTarget> { ["DOWN"] = new("BWELL"), ["EAST"] = new ExitTarget("The stone door is locked."), ["WEST"] = new("MPEAR") });
        AddRoom("TWELL",
            "You are at the top of the well.  Well done.  There are etchings on the side of the well. There is a small crack across the floor at the entrance to a room on the east, but it can be crossed easily.",
            "Top of Well",
            false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("ALICE"), ["DOWN"] = new ExitTarget("It's a long way down!") });
        AddRoom("BWELL",
            "You are in a damp circular room, whose walls are made of brick and mortar.  The roof of this room is not visible, but there appear to be some etchings on the walls.  There is a passageway to the west.",
            "Circular Room",
            false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("MPEAR"), ["UP"] = new("RIDDL") });
        AddRoom("ALICE",
            "You are in a small square room, in the center of which is a large oblong table, no doubt set for afternoon tea.  It is clear from the objects on the table that the users were indeed mad.  In the eastern corner of the room is a small hole (no more than four inches high).  There are passageways leading away to the west and the northwest.",
            "Tea Room",
            false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("TWELL"), ["NW"] = new("MAGNE"), ["EAST"] = new ExitTarget("Only a mouse could get in there.") });
        AddRoom("CMACH", "", "Control Machine", false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("CAGER"), ["WEST"] = new("MAGNE") });
        AddRoom("CAGER",
            "You are in a dingy closet adjacent to the machine room.  On one wall is a small sticker which says: Protected by FROBOZZ Magic Alarm Company (Hello, footpad!)",
            "Dingy Closet",
            false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("CMACH") });
        AddRoom("TLADD", "You are in a very small room.  In the corner is a rickety wooden ladder, leading downward.  It might be safe to descend.  There is also a staircase leading upward.", "Ladder Top", false,
            new Dictionary<string, ExitTarget> { ["DOWN"] = new("BLADD"), ["UP"] = new("MINE7") });
        AddRoom("BLADD", "You are in a rather wide room.  On one side is the bottom of a narrow wooden ladder.  To the northeast and the south are passages leaving the room.", "Ladder Bottom", false,
            new Dictionary<string, ExitTarget> { ["NE"] = new("DEAD7"), ["SOUTH"] = new("TIMBE"), ["UP"] = new("TLADD") });
        AddRoom("DEAD7", "Dead End", "Dead End", false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("BLADD") },
            objectIds: new[] { "COAL" });
        AddRoom("TIMBE", "You are in a long and narrow passage, which is cluttered with broken timbers.  A wide passage comes from the north and turns at the southwest corner of the room into a very narrow passageway.", "Timber Room", false,
            new Dictionary<string, ExitTarget> { ["NORTH"] = new("BLADD"), ["SW"] = new("BSHAF") });
        AddRoom("BSHAF", "You are in a small square room which is at the bottom of a long shaft. To the east is a passageway and to the northeast a very narrow passage. In the shaft can be seen a heavy iron chain.", "Lower Shaft", false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("MACHI"), ["OUT"] = new("TIMBE"), ["NE"] = new("TIMBE") },
            objectIds: new[] { "FBASK" });
        AddRoom("MACHI", "", "Machine Room", false,
            new Dictionary<string, ExitTarget> { ["NW"] = new("BSHAF") },
            objectIds: new[] { "MSWIT", "MACHI" },
            roomAction: RoomActions.MachineRoom);
        AddRoom("MTORC", "", "Torch Room", false,
            new Dictionary<string, ExitTarget> { ["WEST"] = new("MTORC"), ["DOWN"] = new("CRAW4") },
            objectIds: new[] { "TORCH" });
        AddRoom("ECHO", "You are in a large room with a ceiling which cannot be detected from the ground. There is a narrow passage from east to west and a stone stairway leading upward.  The room is extremely noisy.", "Loud Room", false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("CHAS3"), ["WEST"] = new("PASS5"), ["UP"] = new("CAVE3") },
            objectIds: new[] { "BAR" });
        AddRoom("DEAD5", "Dead end", "Dead end", false,
            new Dictionary<string, ExitTarget> { ["SW"] = new("CHAS3") });
        AddRoom("DEAD6", "Dead end", "Dead end", false,
            new Dictionary<string, ExitTarget> { ["EAST"] = new("CHAS3") });
        AddRoom("TCAVE",
            "You are in a small cave whose exits are on the south and northwest.",
            "Small Cave",
            false,
            new Dictionary<string, ExitTarget> { ["NW"] = new("CHAS3"), ["SOUTH"] = new("RCAVE") });
        AddRoom("CHAS3",
            "A chasm, evidently produced by an ancient river, runs through the cave here.  Passages lead off in all directions.",
            "Ancient Chasm",
            false,
            new Dictionary<string, ExitTarget> { ["SOUTH"] = new("ECHO"), ["EAST"] = new("TCAVE"), ["NORTH"] = new("DEAD5"), ["WEST"] = new("DEAD6") });
        AddRoom("CAVE3",
            "You are in a cave.  Passages exit to the south and to the east, but the cave narrows to a crack to the west.  The earth is particularly damp here.",
            "Damp Cave",
            false,
            new Dictionary<string, ExitTarget>
            {
                ["SOUTH"] = new("ECHO"),
                ["EAST"] = new("DAM"),
                ["WEST"] = new ExitTarget("It is too narrow for most insects.")
            });
        const string mindesc = "You are in a non-descript part of a coal mine.";
        AddRoom("MINE1", mindesc, mindesc, false, new Dictionary<string, ExitTarget> { ["NORTH"] = new("MINE4"), ["SW"] = new("MINE2"), ["EAST"] = new("TUNNE") });
        AddRoom("MINE2", mindesc, mindesc, false, new Dictionary<string, ExitTarget> { ["SOUTH"] = new("MINE1"), ["WEST"] = new("MINE5"), ["UP"] = new("MINE3"), ["NE"] = new("MINE4") });
        AddRoom("MINE3", mindesc, mindesc, false, new Dictionary<string, ExitTarget> { ["WEST"] = new("MINE2"), ["NE"] = new("MINE5"), ["EAST"] = new("MINE5") });
        AddRoom("MINE4", mindesc, mindesc, false, new Dictionary<string, ExitTarget> { ["UP"] = new("MINE5"), ["NE"] = new("MINE6"), ["SOUTH"] = new("MINE1"), ["WEST"] = new("MINE2") });
        AddRoom("MINE5", mindesc, mindesc, false, new Dictionary<string, ExitTarget> { ["DOWN"] = new("MINE6"), ["NORTH"] = new("MINE7"), ["WEST"] = new("MINE2"), ["SOUTH"] = new("MINE3"), ["UP"] = new("MINE3"), ["EAST"] = new("MINE4") });
        AddRoom("MINE6", mindesc, mindesc, false, new Dictionary<string, ExitTarget> { ["SE"] = new("MINE4"), ["UP"] = new("MINE5"), ["NW"] = new("MINE7") });
        AddRoom("MINE7", mindesc, mindesc, false, new Dictionary<string, ExitTarget> { ["EAST"] = new("MINE1"), ["WEST"] = new("MINE5"), ["DOWN"] = new("TLADD"), ["SOUTH"] = new("MINE6") });

        // ---- Objects (from dung.56 #OBJECT / ADD-OBJECT) ----
        AddObject("FDOOR", "The door is closed.", "door", null, null, ObjectFlags.Visible, 0, 0, 0, 100, 0);
        AddObject("MAILB", "The small mailbox is here.", "mailbox", "The small mailbox is here.", null, ObjectFlags.Visible | ObjectFlags.Take, 0, 0, 0, 5, 0);
        AddObject("WIND1", "", "window", null, ObjectActions.WindowFunction, ObjectFlags.Visible, 0, 0, 0, 100, 0);
        AddObject("WIND2", "", "window", null, null, ObjectFlags.Visible, 0, 0, 0, 5, 0);
        AddObject("SBAG", "A sandwich bag is here.", "sandwich bag", "On the table is an elongated brown sack, smelling of hot peppers.", null, ObjectFlags.Container | ObjectFlags.Visible | ObjectFlags.Take | ObjectFlags.Burnable, 0, 0, 0, 3, 15,
            synonyms: new[] { "BAG", "SACK", "BAGGI" });
        AddObject("GARLI", "There is a clove of garlic here.", "clove of garlic", null, null, ObjectFlags.Take | ObjectFlags.Food | ObjectFlags.Visible, 0, 0, 0, 5, 0, new[] { "CLOVE" });
        AddObject("FOOD", "A hot pepper sandwich is here.", "lunch", null, null, ObjectFlags.Food | ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 5, 0, new[] { "SANDW", "LUNCH", "PEPPE", "DINNE", "SNACK" });
        AddObject("BOTTL", "A clear glass bottle is here.", "glass bottle", "A bottle is sitting on the table.", ObjectActions.BottleFunction, ObjectFlags.Container | ObjectFlags.Transparent | ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 5, 4, new[] { "CONTA", "PITCH" });
        AddObject("WATER", "Water", "quantity of water", "There is some water here", ObjectActions.WaterFunction, ObjectFlags.Drinkable | ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 4, 0, new[] { "LIQUI", "H2O" });
        AddObject("ROPE", "There is a large coil of rope here.", "rope", "A large coil of rope is lying in the corner.", ObjectActions.RopeFunction, ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 10, 0, new[] { "HEMP", "COIL" });
        AddObject("KNIFE", "There is a nasty-looking knife lying here.", "knife", "On a table is a nasty-looking knife.", null, ObjectFlags.Take | ObjectFlags.Visible | ObjectFlags.Weapon, 0, 0, 0, 5, 0, new[] { "BLADE" });
        AddObject("BRICK", "A pile of bricks is here.", "pile of bricks", null, ObjectActions.BrickFunction, ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 20, 0);
        AddObject("SWORD", "There is an elvish sword here.", "sword", "On hooks above the mantelpiece hangs an elvish sword of great antiquity.", null, ObjectFlags.Visible | ObjectFlags.Take | ObjectFlags.Weapon, 0, 0, 0, 30, 0, new[] { "ORCRI", "GLAMD", "BLADE" });
        AddObject("LAMP", "There is a brass lantern (battery-powered) here.", "lamp", "A battery-powered brass lantern is on the trophy case.", ObjectActions.Lantern, ObjectFlags.Take | ObjectFlags.Visible, -1, 0, 0, 15, 0, new[] { "LANTE" });
        AddObject("BLAMP", "There is a broken brass lantern here.", "broken lamp", null, null, ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 15, 0, new[] { "LAMP", "LANTE" });
        AddObject("MATCH", "A match is here.", "match", null, ObjectActions.MatchFunction, ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 1, 0);
        AddObject("CANDL", "Some candles are here.", "candles", null, ObjectActions.CandlesFunction, ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 5, 0, new[] { "CANDLE" });
        AddObject("RUG", "", "carpet", null, ObjectActions.Rug, ObjectFlags.Visible | ObjectFlags.NoDescribe, 0, 0, 0, 100, 0, new[] { "CARPE" });
        AddObject("TCASE", "There is a trophy case here.", "trophy case", null, ObjectActions.TrophyCase, ObjectFlags.Container | ObjectFlags.Transparent | ObjectFlags.Visible, 0, 0, 0, 100, 100, new[] { "CASE" });
        AddObject("PAPER", "There is a leaflet here.", "leaflet", null, null, ObjectFlags.Take | ObjectFlags.Visible | ObjectFlags.Readable, 0, 0, 0, 1, 0);
        AddObject("WDOOR", "", "wooden door", null, null, ObjectFlags.Door | ObjectFlags.Visible, 0, 0, 0, 100, 0);
        AddObject("DOOR", "The trap door is closed.", "trap door", null, ObjectActions.TrapDoor, ObjectFlags.Door, 0, 0, 0, 100, 0, new[] { "TRAP" }); // visible when rug moved; "TRAP" so OPEN TRAP is unambiguous
        AddObject("TDOOR", "The trap door is open.", "trap door", null, ObjectActions.TrapDoor, ObjectFlags.Visible, 0, 0, 0, 100, 0, new[] { "TRAP" });
        AddObject("TROLL", "A nasty-looking troll, brandishing a bloody axe, blocks all passages out of the room.", "troll", null, null, ObjectFlags.Visible, 0, 0, 0, 100, 2);
        AddObject("AXE", "There is a bloody axe here.", "bloody axe", null, null, ObjectFlags.Visible | ObjectFlags.Weapon, 0, 0, 0, 25, 0);
        AddObject("RKNIF", "There is a rusty knife here.", "rusty knife", "Beside the skeleton is a rusty knife.", null, ObjectFlags.Visible | ObjectFlags.Take | ObjectFlags.Weapon, 0, 0, 0, 20, 0, new[] { "KNIFE" });
        AddObject("BLANT", "There is a burned-out lantern here.", "burned-out lantern", "The deceased adventurer's useless lantern is here.", null, ObjectFlags.Visible | ObjectFlags.Take, 0, 0, 0, 20, 0, new[] { "LANTE", "LAMP" });
        AddObject("KEYS", "There is a set of skeleton keys here.", "set of skeleton keys", null, null, ObjectFlags.Tool | ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 10, 0);
        AddObject("BONES", "A skeleton, probably the remains of a luckless adventurer, lies here.", "", null, ObjectActions.Skeleton, ObjectFlags.Visible, 0, 0, 0, 100, 0, new[] { "SKELE", "BODY" });
        AddObject("BAGCO", "An old leather bag, bulging with coins, is here.", "bag of coins", null, null, ObjectFlags.Take | ObjectFlags.Visible, 0, 10, 5, 15, 0, new[] { "BAG", "COINS" });
        AddObject("BAR", "There is a large platinum bar here.", "platinum bar", null, null, ObjectFlags.Sacred | ObjectFlags.Take | ObjectFlags.Visible, 0, 12, 10, 20, 0, new[] { "PLATI" });
        AddObject("GRAT1", "A grating is here.", "grating", null, null, ObjectFlags.None, 0, 0, 0, 100, 0); // visible when leaves moved/burned
        AddObject("GRAT2", "A grating is here.", "grating", null, null, ObjectFlags.Visible, 0, 0, 0, 100, 0);
        AddObject("LEAVE", "There is a pile of leaves on the ground.", "pile of leaves", null, ObjectActions.LeafPile, ObjectFlags.Burnable | ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 25, 0, new[] { "LEAF", "PILE" });
        AddObject("JADE", "There is an exquisite jade figurine here.", "jade figurine", null, null, ObjectFlags.Take | ObjectFlags.Visible, 0, 5, 5, 10, 0, new[] { "FIGUR" });
        AddObject("BAT", "", "vampire bat", null, null, ObjectFlags.Visible, 0, 0, 0, 5, 0);
        AddObject("BRACE", "There is a sapphire-encrusted bracelet here.", "sapphire bracelet", null, null, ObjectFlags.Take | ObjectFlags.Visible, 0, 5, 3, 10, 0, new[] { "JEWEL" });
        AddObject("COAL", "There is a small heap of coal here.", "small pile of coal", null, null, ObjectFlags.Burnable | ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 20, 0, new[] { "HEAP", "CHARC" });
        AddObject("MACHI", "", "machine", null, ObjectActions.MachineFunction, ObjectFlags.Container | ObjectFlags.Visible, 0, 0, 0, 100, 50, new[] { "PDP10", "DRYER", "LID" });
        AddObject("DIAMO", "There is an enormous diamond (perfectly cut) here.", "huge diamond", null, null, ObjectFlags.Take | ObjectFlags.Visible, 0, 10, 6, 5, 0, new[] { "PERFE" });
        AddObject("GUNK", "There is a small piece of vitreous slag here.", "piece of vitreous slag", null, ObjectActions.GunkFunction, ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 10, 0, new[] { "MESS", "SLAG" });
        AddObject("ICE", "A mass of ice fills the western half of the room.", "glacier", null, ObjectActions.Glacier, ObjectFlags.Visible, 0, 0, 0, 100, 0, new[] { "GLACI" });
        AddObject("RUBY", "There is a moby ruby lying here.", "ruby", "On the floor lies a moby ruby.", null, ObjectFlags.Take | ObjectFlags.Visible, 0, 15, 8, 5, 0);
        AddObject("TORCH", "There is an ivory torch here.", "torch", "Sitting on the pedestal is a flaming torch, made of ivory.", null, ObjectFlags.Tool | ObjectFlags.OnFire | ObjectFlags.Take | ObjectFlags.Visible, 1, 14, 6, 20, 0);
        AddObject("REFL1", "", "mirror", null, ObjectActions.MirrorFunction, ObjectFlags.Visible, 0, 0, 0, 100, 0, new[] { "MIRRO" });
        AddObject("REFL2", "", "mirror", null, ObjectActions.MirrorFunction, ObjectFlags.Visible, 0, 0, 0, 100, 0, new[] { "MIRRO" });
        AddObject("IRBOX", "There is a dented iron box here.", "iron box", null, ObjectActions.IronBoxFunction, ObjectFlags.Visible | ObjectFlags.Take | ObjectFlags.Container, 0, 0, 0, 40, 20, new[] { "BOX", "IRON", "DENTE" });
        AddObject("CYCLO", "", "cyclops", null, ObjectActions.CyclopsFunction, ObjectFlags.Visible | ObjectFlags.Villain, 0, 0, 0, 100, 0, new[] { "ONE-E", "MONST" });
        AddObject("TRUNK", "A trunk is here.", "trunk", null, null, ObjectFlags.Container | ObjectFlags.Visible, 0, 0, 0, 30, 30);
        AddObject("FUSE", "A fuse is here.", "fuse", null, ObjectActions.FuseFunction, ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 5, 0);
        AddObject("COFFI", "There is a solid-gold coffin, used for the burial of Ramses II, here.", "gold coffin", null, null, ObjectFlags.Container | ObjectFlags.Sacred | ObjectFlags.Take | ObjectFlags.Visible, 0, 3, 7, 55, 35, new[] { "CASKE" });
        AddObject("TRIDE", "Neptune's own crystal trident is here.", "crystal trident", "On the shore lies Neptune's own crystal trident.", null, ObjectFlags.Take | ObjectFlags.Visible, 0, 4, 11, 20, 0, new[] { "FORK" });
        AddObject("TBASK", "At the end of the chain is a basket.", "basket", null, ObjectActions.Dumbwaiter, ObjectFlags.Container | ObjectFlags.Visible | ObjectFlags.Transparent, 0, 0, 0, 100, 50, new[] { "CAGE", "DUMBW", "BASKE" });
        AddObject("FBASK", "", "", null, ObjectActions.Dumbwaiter, ObjectFlags.Visible, 0, 0, 0, 100, 0, new[] { "CAGE", "DUMBW", "BASKE" });
        AddObject("RAILI", "A railing is here.", "railing", null, null, ObjectFlags.Visible, 0, 0, 0, 100, 0);
        AddObject("PUMP", "A pump is here.", "pump", null, null, ObjectFlags.Visible, 0, 0, 0, 100, 0);
        AddObject("MSWIT", "A screw is here.", "screw", null, ObjectActions.MSwitchFunction, ObjectFlags.Visible | ObjectFlags.Tool, 0, 0, 0, 1, 0);
        AddObject("SCREW", "A tiny screw is here.", "screw", null, null, ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 1, 0);
        AddObject("THIEF", "A seedy-looking individual with a large bag is here.", "thief", null, ObjectActions.ThiefFunction, ObjectFlags.Visible | ObjectFlags.Villain, 0, 0, 0, 100, 0, new[] { "ROBBE", "SEEDY" });
        AddObject("RECEP", "", "receptacle", null, null, ObjectFlags.Container | ObjectFlags.Visible, 0, 0, 0, 6, 0);
        AddObject("BALLO", "A cloth balloon is here.", "cloth balloon", null, ObjectActions.BalloonFunction, ObjectFlags.Container | ObjectFlags.Visible | ObjectFlags.Take | ObjectFlags.Vehicle, 0, 0, 0, 100, 100, new[] { "BALLOON" });
        AddObject("GNOME", "", "volcano gnome", null, ObjectActions.GnomeFunction, ObjectFlags.Visible, 0, 0, 0, 100, 0);
        AddObject("BUCKE", "A bucket is here.", "bucket", null, null, ObjectFlags.Container | ObjectFlags.Visible | ObjectFlags.Take, 0, 0, 0, 100, 100);
        // LLD (Hades): exorcism
        AddObject("GHOST", "", "evil spirits", null, ObjectActions.GhostFunction, ObjectFlags.Visible, 0, 0, 0, 100, 0, new[] { "SPIRI", "FIEND" });
        AddObject("GATES", "", "gate", null, null, ObjectFlags.Visible, 0, 0, 0, 100, 0, new[] { "GATE" });
        AddObject("CORPS", "A pile of mangled corpses lies in one corner.", "pile of corpses", null, null, ObjectFlags.Visible, 0, 0, 0, 100, 0, new[] { "MANGL" });
        AddObject("BELL", "There is a small brass bell here.", "small brass bell", null, null, ObjectFlags.Take | ObjectFlags.Visible, 0, 0, 0, 5, 0);
        AddObject("BOOK", "There is a large black book here.", "large black book", null, null, ObjectFlags.Take | ObjectFlags.Visible | ObjectFlags.Readable, 0, 0, 0, 10, 0, new[] { "BIBLE", "BLACK" });
        AddObject("STICK", "There is a small wooden stick here.", "small wooden stick", null, ObjectActions.StickFunction, ObjectFlags.Take | ObjectFlags.Visible | ObjectFlags.Tool, 0, 0, 0, 5, 0, new[] { "WAND", "MAGIC" });
        AddObject("GRAIL", "The Holy Grail is here.", "Holy Grail", "The Holy Grail is on the pedestal.", null, ObjectFlags.Sacred | ObjectFlags.Take | ObjectFlags.Visible, 0, 15, 15, 5, 0, new[] { "CUP" });
        AddObject("PEARL", "A large, glowing pearl is here.", "large pearl", null, null, ObjectFlags.Take | ObjectFlags.Visible, 0, 9, 9, 5, 0);
        AddObject("ZORKM", "There is an engraved zorkmid coin here.", "priceless zorkmid", "On the floor is a gold zorkmid coin (a valuable collector's item).", null, ObjectFlags.Take | ObjectFlags.Visible | ObjectFlags.Readable, 0, 10, 12, 10, 0, new[] { "COIN", "GOLD" });
        AddObject("HOOK1", "There is a small hook attached to the rock here.", "hook", null, null, ObjectFlags.Visible, 0, 0, 0, 100, 0, new[] { "HOOK" });
        AddObject("HOOK2", "There is a small hook attached to the rock here.", "hook", null, null, ObjectFlags.Visible, 0, 0, 0, 100, 0, new[] { "HOOK" });
        AddObject("POT", "There is a pot of gold here.", "pot filled with gold", "At the end of the rainbow is a pot of gold.", null, ObjectFlags.Take | ObjectFlags.Visible, 0, 10, 10, 15, 0, new[] { "GOLD" });
        AddObject("SPHER", "There is a beautiful crystal sphere here.", "crystal sphere", null, null, ObjectFlags.Sacred | ObjectFlags.Visible | ObjectFlags.Take, 0, 6, 6, 10, 0, new[] { "BALL", "CRYST", "GLASS" });
        AddObject("CAGE", "There is a mangled cage here.", "mangled cage", null, null, ObjectFlags.Visible, 0, 0, 0, 60, 0, new[] { "IRON" });
        AddObject("RCAGE", "There is an iron cage in the middle of the room.", "iron cage", null, null, ObjectFlags.Visible, 0, 0, 0, 100, 0, new[] { "CAGE", "IRON" });

        // Place objects in starting rooms
        PlaceInRoom("WHOUS", "FDOOR", "MAILB");
        PlaceInRoom("EHOUS", "WIND1");
        PlaceInRoom("KITCH", "WIND2", "SBAG", "BOTTL", "MATCH");
        PutInContainer("SBAG", "GARLI", "FOOD");
        PutInContainer("BOTTL", "WATER");
        PlaceInRoom("ATTIC", "BRICK", "ROPE", "KNIFE");
        PlaceInRoom("LROOM", "WDOOR", "DOOR", "TCASE", "LAMP", "RUG", "PAPER", "SWORD", "CANDL");
        PlaceInRoom("CELLA", "TDOOR");
        PlaceInRoom("MTROL", "TROLL");
        PutInContainer("TROLL", "AXE");
        PutInContainer("BALLO", "RECEP");
        var ballo = FindObject("BALLO");
        if (ballo != null) ballo.Open = true;
        PlaceInRoom("LEDG4", "BALLO", "HOOK2");
        PlaceInRoom("LEDG2", "HOOK1", "ZORKM");
        PlaceInRoom("MGRAI", "GRAIL");
        PlaceInRoom("MPEAR", "PEARL");
        PlaceInRoom("POG", "POT");
        PlaceInRoom("CAGER", "SPHER");
        PlaceInRoom("CAGED", "RCAGE");
        PlaceInRoom("MAZE5", "BONES", "BAGCO", "KEYS", "BLANT", "RKNIF");
        PlaceInRoom("CLEAR", "GRAT1", "LEAVE");
        PlaceInRoom("MGRAT", "GRAT2");
        PlaceInRoom("BOOM", "BRACE");
        PlaceInRoom("DEAD7", "COAL");
        PlaceInRoom("BATS", "JADE", "BAT");
        PlaceInRoom("STREA", "FUSE");
        PlaceInRoom("RESES", "TRUNK");
        PlaceInRoom("EGYPT", "COFFI");
        PlaceInRoom("ICY", "ICE");
        PlaceInRoom("RUBYR", "RUBY");
        PlaceInRoom("MTORC", "TORCH");
        PlaceInRoom("MIRR1", "REFL1");
        PlaceInRoom("MIRR2", "REFL2");
        PlaceInRoom("ATLAN", "TRIDE");
        PlaceInRoom("RESEN", "PUMP");
        PlaceInRoom("TSHAF", "TBASK");
        PlaceInRoom("DOME", "RAILI");
        PlaceInRoom("BWELL", "BUCKE");
        PlaceInRoom("TREAS", "THIEF");
        PlaceInRoom("LLD1", "GHOST", "GATES", "CORPS");
        PlaceInRoom("TEMP1", "BELL");
        PlaceInRoom("TEMP2", "BOOK");
        PlaceInRoom("CANY1", "STICK");

        // Starting location (BLOC = WHOUS in original)
        var start = FindRoom("WHOUS")!;
        _state.Winner.CurrentRoom = start;
        _state.Rooms.AddRange(_rooms.Values);
        _state.AllObjects.AddRange(_objects.Values);
        _state.World = this;
    }

    private void RegisterCarouselExitActions()
    {
        var carouselRooms = new[] { "CAVE4", "MGRAI", "PASS1", "CANY1", "PASS5", "PASS4", "MAZE1", "PASS3" };
        void CarouselOut(GameState state, string _)
        {
            state.Output.WriteLine("Unfortunately, it is impossible to tell directions in here.");
            var room = FindRoom(carouselRooms[Random.Shared.Next(carouselRooms.Length)]);
            if (room != null)
                Parser.MoveToRoom(state, room);
        }
        foreach (var dir in new[] { "NORTH", "SOUTH", "EAST", "WEST", "NW", "NE", "SE", "SW" })
            _exitActions[(NormId("CAROU"), dir)] = CarouselOut;
    }

    private void RegisterCoffinCure()
    {
        void CoffinCure(GameState state)
        {
            var coffin = FindObject("COFFI");
            state.SetFlag("EGYPT-FLAG!-FLAG", coffin == null || !state.Winner.Inventory.Contains(coffin));
        }
        _beforeExitActions[(NormId("EGYPT"), "EAST")] = CoffinCure;
        _beforeExitActions[(NormId("RESES"), "SOUTH")] = CoffinCure;
        _beforeExitActions[(NormId("RESES"), "UP")] = CoffinCure;
    }

    private void AddRoom(string id, string descLong, string descShort, bool hasLight,
        Dictionary<string, ExitTarget> exits,
        string[]? objectIds = null,
        Action<GameState, string>? roomAction = null,
        int visitScore = 0,
        Action<GameState>? onEnter = null)
    {
        var r = new Room
        {
            Id = id,
            DescriptionLong = descLong,
            DescriptionShort = descShort,
            HasLight = hasLight,
            RoomAction = roomAction,
            VisitScore = visitScore,
            OnEnter = onEnter
        };
        foreach (var e in exits)
            r.Exits[e.Key] = e.Value;
        _rooms[NormId(id)] = r;
        if (objectIds != null)
        {
            foreach (var oid in objectIds)
            {
                var o = FindObject(oid);
                if (o != null)
                {
                    o.InRoom = r;
                    r.Objects.Add(o);
                }
            }
        }
    }

    private void AddObject(string id, string descHere, string descShort, string? descUntouched,
        Action<GameState, string, GameObject?>? action,
        ObjectFlags flags, int light, int findScore, int trophyScore, int size, int capacity,
        string[]? synonyms = null)
    {
        if (_objects.ContainsKey(NormId(id))) return; // LAMP added twice in data
        var o = new GameObject
        {
            Id = id,
            DescriptionHere = descHere,
            DescriptionShort = descShort,
            DescriptionUntouched = descUntouched,
            ObjectAction = action,
            Flags = flags,
            LightAmount = light,
            FindScore = findScore,
            TrophyScore = trophyScore,
            Size = size,
            Capacity = capacity
        };
        if (synonyms != null)
            o.Synonyms = synonyms.ToList();
        _objects[NormId(id)] = o;
    }

    private void PlaceInRoom(string roomId, params string[] objectIds)
    {
        var r = FindRoom(roomId);
        if (r == null) return;
        foreach (var oid in objectIds)
        {
            var o = FindObject(oid);
            if (o != null)
            {
                o.InRoom = r;
                o.Carrier = null;
                if (!r.Objects.Contains(o))
                    r.Objects.Add(o);
            }
        }
    }

    private void PutInContainer(string containerId, params string[] objectIds)
    {
        var c = FindObject(containerId);
        if (c == null) return;
        foreach (var oid in objectIds)
        {
            var o = FindObject(oid);
            if (o != null)
            {
                o.Container = c;
                o.InRoom = null;
                o.Carrier = null;
                c.Contents.Add(o);
            }
        }
    }
}
