using System.Text;
using ZorkDotNet.Game;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var state = new GameState();
var world = new World(state);

// Intro from original Zork
state.Output.WriteLine();
state.Output.WriteLine("Zork I: The Great Underground Empire");
state.Output.WriteLine("Copyright (c) 1981, 1982, 1983 Infocom, Inc. All rights reserved.");
state.Output.WriteLine("Zork is a registered trademark of Infocom, Inc.");
state.Output.WriteLine("Revision 88 / Serial number 840726");
state.Output.WriteLine();

Parser.Execute(state, "LOOK");

while (state.Running)
{
    state.Output.Write("> ");
    var line = Console.ReadLine();
    if (line == null) break;
    line = line.Trim();
    if (string.IsNullOrEmpty(line)) continue;
    state.Winner.Moves++;
    Parser.Execute(state, line);
    state.ProcessClocks();
}
