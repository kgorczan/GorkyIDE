using System.Text;

namespace GorkyIDE.ConsoleUI.Shell.Input;

internal static class ShellInputReader
{
    public static ShellInput Read()
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key != ConsoleKey.Escape)
        {
            return ShellInput.FromKey(key);
        }

        var sequence = ReadEscapeSequence();
        if (sequence.Length == 1)
        {
            return ShellInput.FromKey(key);
        }

        return TryParseMouseClick(sequence, out var mouseClick)
            ? ShellInput.FromMouse(mouseClick)
            : ShellInput.None;
    }

    private static string ReadEscapeSequence()
    {
        var text = new StringBuilder("\u001b");
        for (var attempt = 0; attempt < 20; attempt++)
        {
            Thread.Sleep(1);
            while (Console.KeyAvailable)
            {
                text.Append(Console.ReadKey(intercept: true).KeyChar);
            }
        }

        return text.ToString();
    }

    private static bool TryParseMouseClick(string sequence, out MouseClick mouseClick)
    {
        mouseClick = default;
        if (!sequence.StartsWith("\u001b[<", StringComparison.Ordinal) || sequence.Length < 7)
        {
            return false;
        }

        var final = sequence[^1];
        if (final != 'M')
        {
            return false;
        }

        var payload = sequence[3..^1];
        var parts = payload.Split(';');
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var button) ||
            !int.TryParse(parts[1], out var column) ||
            !int.TryParse(parts[2], out var row))
        {
            return false;
        }

        if ((button & 0b11) != 0)
        {
            return false;
        }

        mouseClick = new MouseClick(column - 1, row - 1);
        return true;
    }
}
