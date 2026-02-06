using System.Text;
using ZorkDotNet.Game;

namespace ZorkDotNet.Tests;

/// <summary>
/// Runs the game with a scripted list of commands and logs all input/output to a file and in-memory.
/// </summary>
public sealed class GameRunner : IDisposable
{
    private readonly GameState _state;
    private readonly StringWriter _capture;
    private readonly StreamWriter? _logFile;
    private readonly TextWriter _tee;

    public GameState State => _state;

    /// <summary>
    /// Create a runner that optionally writes to a log file. If logPath is null, only in-memory capture is used.
    /// </summary>
    public GameRunner(string? logPath = null)
    {
        _state = new GameState();
        _ = new World(_state);
        _capture = new StringWriter(new StringBuilder());
        if (!string.IsNullOrEmpty(logPath))
        {
            var dir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            _logFile = new StreamWriter(logPath, append: false, Encoding.UTF8);
            _tee = new TeeWriter(_capture, _logFile);
        }
        else
        {
            _logFile = null;
            _tee = _capture;
        }
        _state.Output = _tee;
    }

    /// <summary>
    /// Run a single command: increment moves, execute, process clocks. Logs "> command" then game output.
    /// </summary>
    public void Execute(string command)
    {
        _tee.WriteLine("> " + command);
        _state.Winner.Moves++;
        Parser.Execute(_state, command);
        _state.ProcessClocks();
        _tee.Flush();
    }

    /// <summary>
    /// Run multiple commands in sequence (no intro LOOK is run automatically).
    /// </summary>
    public void Run(params string[] commands)
    {
        foreach (var cmd in commands)
        {
            if (string.IsNullOrWhiteSpace(cmd)) continue;
            Execute(cmd.Trim());
        }
    }

    /// <summary>
    /// Get all output so far as a single string.
    /// </summary>
    public string GetOutput() => _capture.ToString();

    public void Dispose()
    {
        _logFile?.Dispose();
    }

    /// <summary>
    /// Create a log path under a base directory (e.g. TestLogs) with test name and optional timestamp.
    /// </summary>
    public static string GetLogPath(string testName, string baseDir = "TestLogs", bool includeTimestamp = true)
    {
        var name = testName.Replace(" ", "_");
        var stamp = includeTimestamp ? "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") : "";
        return Path.Combine(baseDir, name + stamp + ".log");
    }
}
