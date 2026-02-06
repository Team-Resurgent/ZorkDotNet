namespace ZorkDotNet.Game;

/// <summary>
/// Simple SAVE/RESTORE (writes room, score, moves, flags, inventory to file).
/// </summary>
public static class GameSave
{
    public const string DefaultSavePath = "zork.sav";
    public const string DefaultScriptPath = "zork.log";

    public static void Save(GameState state, string path = DefaultSavePath)
    {
        try
        {
            using var w = new StreamWriter(path, append: false);
            w.WriteLine(state.Here.Id);
            w.WriteLine(state.Winner.Score);
            w.WriteLine(state.Winner.Moves);
            w.WriteLine(state.Winner.BriefMode ? 1 : 0);
            w.WriteLine(state.Winner.SuperBriefMode ? 1 : 0);
            foreach (var kv in state.Flags.OrderBy(x => x.Key))
                w.WriteLine(kv.Key + "=" + (kv.Value ? 1 : 0));
            w.WriteLine("---");
            foreach (var o in state.Winner.Inventory)
                w.WriteLine(o.Id);
            state.Output.WriteLine("Done.");
        }
        catch (Exception ex)
        {
            state.Output.WriteLine("Save failed: " + ex.Message);
        }
    }

    public static void Restore(GameState state, string path = DefaultSavePath)
    {
        if (state.World == null) { state.Output.WriteLine("No game to restore."); return; }
        try
        {
            var lines = File.ReadAllLines(path).ToList();
            var i = 0;
            if (i >= lines.Count) throw new InvalidDataException("Empty save file.");
            var roomId = lines[i++];
            var room = state.World.FindRoom(roomId);
            if (room == null) throw new InvalidDataException("Unknown room: " + roomId);
            state.Winner.CurrentRoom = room;
            room.Seen = true;
            if (i >= lines.Count) throw new InvalidDataException("Truncated.");
            state.Winner.Score = int.Parse(lines[i++]);
            state.Winner.Moves = int.Parse(lines[i++]);
            state.Winner.BriefMode = lines[i++] == "1";
            state.Winner.SuperBriefMode = lines[i++] == "1";
            while (i < lines.Count && lines[i] != "---")
            {
                var parts = lines[i].Split('=', 2);
                if (parts.Length == 2) state.SetFlag(parts[0], parts[1] == "1");
                i++;
            }
            if (i < lines.Count) i++; // skip "---"
            state.Winner.Inventory.Clear();
            while (i < lines.Count)
            {
                var o = state.World.FindObject(lines[i].Trim());
                if (o != null)
                {
                    if (o.InRoom != null) o.InRoom.Objects.Remove(o);
                    if (o.Container != null) o.Container.Contents.Remove(o);
                    o.InRoom = null;
                    o.Container = null;
                    o.Carrier = state.Winner;
                    state.Winner.Inventory.Add(o);
                }
                i++;
            }
            state.Output.WriteLine("Restored.");
            Parser.Execute(state, "LOOK");
        }
        catch (Exception ex)
        {
            state.Output.WriteLine("Restore failed: " + ex.Message);
        }
    }
}
