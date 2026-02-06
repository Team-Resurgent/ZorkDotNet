namespace ZorkDotNet.Game;

/// <summary>
/// Writes to two TextWriters (e.g. console + script file).
/// </summary>
public sealed class TeeWriter : TextWriter
{
    private readonly TextWriter _first;
    private readonly TextWriter _second;

    public override System.Text.Encoding Encoding => _first.Encoding;

    public TeeWriter(TextWriter first, TextWriter second)
    {
        _first = first;
        _second = second;
    }

    public override void Write(char value)
    {
        _first.Write(value);
        _second.Write(value);
    }

    public override void Write(string? value)
    {
        _first.Write(value);
        _second.Write(value);
    }

    public override void WriteLine(string? value)
    {
        _first.WriteLine(value);
        _second.WriteLine(value);
    }

    public override void Flush()
    {
        _first.Flush();
        _second.Flush();
    }
}
