using System.Text;
using GorkyIDE.ConsoleUI.Shell.Formatting;

namespace GorkyIDE.ConsoleUI.Shell.Editor;

internal sealed class EditorTab
{
    private readonly List<string> lines;
    private string lineEnding;
    private bool insertFinalNewLine;
    private int cursorLine;
    private int cursorColumn;
    private int scrollLine;
    private int scrollColumn;

    private EditorTab(string path, List<string> lines, string lineEnding, bool insertFinalNewLine)
    {
        Path = path;
        this.lines = lines.Count == 0 ? new List<string> { string.Empty } : lines;
        this.lineEnding = lineEnding;
        this.insertFinalNewLine = insertFinalNewLine;
    }

    public string Path { get; }

    public string Title => System.IO.Path.GetFileName(Path) + (IsDirty ? " *" : string.Empty);

    public bool IsDirty { get; private set; }

    public static EditorTab Open(string path)
    {
        var text = File.ReadAllText(path);
        var lineEnding = DetectLineEnding(text);
        var endsWithNewLine = text.EndsWith("\n", StringComparison.Ordinal) || text.EndsWith("\r", StringComparison.Ordinal);
        var lines = text.Replace("\r", string.Empty).Split('\n').ToList();
        if (lines.Count > 0 && lines[^1].Length == 0 && endsWithNewLine)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return new EditorTab(path, lines, lineEnding, endsWithNewLine);
    }

    public string HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                MoveCursorVertical(-1);
                return string.Empty;
            case ConsoleKey.DownArrow:
                MoveCursorVertical(1);
                return string.Empty;
            case ConsoleKey.LeftArrow:
                MoveCursorLeft();
                return string.Empty;
            case ConsoleKey.RightArrow:
                MoveCursorRight();
                return string.Empty;
            case ConsoleKey.PageUp:
                MoveCursorVertical(-10);
                return string.Empty;
            case ConsoleKey.PageDown:
                MoveCursorVertical(10);
                return string.Empty;
            case ConsoleKey.Home:
                cursorColumn = 0;
                return string.Empty;
            case ConsoleKey.End:
                cursorColumn = lines[cursorLine].Length;
                return string.Empty;
            case ConsoleKey.Backspace:
                Backspace();
                return "Edited.";
            case ConsoleKey.Delete:
                Delete();
                return "Edited.";
            case ConsoleKey.Enter:
                SplitLine();
                return "Edited.";
        }

        if (!char.IsControl(key.KeyChar))
        {
            Insert(key.KeyChar);
            return "Edited.";
        }

        return string.Empty;
    }

    public string Format(EditorFormattingOptions options)
    {
        var changed = false;
        lineEnding = options.LineEnding;
        insertFinalNewLine = options.InsertFinalNewLine;

        for (var index = 0; index < lines.Count; index++)
        {
            var formattedLine = FormatLine(lines[index], options);
            if (!string.Equals(lines[index], formattedLine, StringComparison.Ordinal))
            {
                lines[index] = formattedLine;
                changed = true;
            }
        }

        cursorLine = Math.Clamp(cursorLine, 0, lines.Count - 1);
        cursorColumn = Math.Clamp(cursorColumn, 0, lines[cursorLine].Length);

        if (changed)
        {
            IsDirty = true;
            return $"Formatted using .editorconfig ({options.IndentStyle}, {options.IndentSize}).";
        }

        return $"Already formatted using .editorconfig ({options.IndentStyle}, {options.IndentSize}).";
    }

    public void Save()
    {
        var content = string.Join(lineEnding, lines);
        if (insertFinalNewLine)
        {
            content += lineEnding;
        }

        File.WriteAllText(Path, content);
        IsDirty = false;
    }

    public void EnsureCursorVisible(int visibleHeight, int visibleWidth)
    {
        if (cursorLine < scrollLine)
        {
            scrollLine = cursorLine;
        }
        else if (cursorLine >= scrollLine + visibleHeight)
        {
            scrollLine = cursorLine - visibleHeight + 1;
        }

        var textWidth = Math.Max(1, visibleWidth - 7);
        if (cursorColumn < scrollColumn)
        {
            scrollColumn = cursorColumn;
        }
        else if (cursorColumn >= scrollColumn + textWidth)
        {
            scrollColumn = cursorColumn - textWidth + 1;
        }

        scrollLine = Math.Max(0, scrollLine);
        scrollColumn = Math.Max(0, scrollColumn);
    }

    public void MoveCursorToScreenPosition(int rowOffset, int columnOffset, int visibleHeight, int visibleWidth)
    {
        if (rowOffset < 0 || rowOffset >= visibleHeight)
        {
            return;
        }

        cursorLine = Math.Clamp(scrollLine + rowOffset, 0, lines.Count - 1);
        cursorColumn = Math.Clamp(scrollColumn + Math.Max(0, columnOffset - 6), 0, lines[cursorLine].Length);
        EnsureCursorVisible(visibleHeight, visibleWidth);
    }

    public string Render(int visibleHeight, int visibleWidth)
    {
        var text = new StringBuilder();
        var lastLine = Math.Min(lines.Count, scrollLine + visibleHeight);

        for (var lineIndex = scrollLine; lineIndex < lastLine; lineIndex++)
        {
            var line = lines[lineIndex];
            var visibleLine = scrollColumn < line.Length ? line[scrollColumn..] : string.Empty;
            var linePrefix = (lineIndex + 1).ToString().PadLeft(4) + " │";

            if (lineIndex == cursorLine)
            {
                var cursorOffset = Math.Clamp(cursorColumn - scrollColumn, 0, Math.Max(0, visibleWidth - linePrefix.Length - 1));
                visibleLine = DrawCursor(visibleLine, cursorOffset);
            }

            text.Append(linePrefix).Append(Fit(visibleLine, Math.Max(0, visibleWidth - linePrefix.Length))).AppendLine();
        }

        return text.ToString();
    }

    private static string DetectLineEnding(string text)
    {
        var crlf = text.IndexOf("\r\n", StringComparison.Ordinal);
        if (crlf >= 0)
        {
            return "\r\n";
        }

        return text.IndexOf('\r') >= 0 ? "\r" : "\n";
    }

    private static string FormatLine(string line, EditorFormattingOptions options)
    {
        var contentStart = 0;
        var visualWidth = 0;
        while (contentStart < line.Length && line[contentStart] is ' ' or '\t')
        {
            if (line[contentStart] == '\t')
            {
                visualWidth += options.TabWidth - visualWidth % options.TabWidth;
            }
            else
            {
                visualWidth++;
            }

            contentStart++;
        }

        var content = options.TrimTrailingWhitespace ? line[contentStart..].TrimEnd() : line[contentStart..];
        var indentation = options.IndentStyle.Equals("tab", StringComparison.OrdinalIgnoreCase)
            ? new string('\t', visualWidth / options.TabWidth) + new string(' ', visualWidth % options.TabWidth)
            : new string(' ', visualWidth / options.IndentSize * options.IndentSize + visualWidth % options.IndentSize);

        return content.Length == 0 ? string.Empty : indentation + content;
    }

    private static string DrawCursor(string line, int cursorOffset)
    {
        if (cursorOffset >= line.Length)
        {
            return line + "█";
        }

        return line[..cursorOffset] + "█" + line[(cursorOffset + 1)..];
    }

    private static string Fit(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        return value.Length > width ? value[..width] : value.PadRight(width);
    }

    private void MoveCursorVertical(int delta)
    {
        cursorLine = Math.Clamp(cursorLine + delta, 0, lines.Count - 1);
        cursorColumn = Math.Min(cursorColumn, lines[cursorLine].Length);
    }

    private void MoveCursorLeft()
    {
        if (cursorColumn > 0)
        {
            cursorColumn--;
            return;
        }

        if (cursorLine > 0)
        {
            cursorLine--;
            cursorColumn = lines[cursorLine].Length;
        }
    }

    private void MoveCursorRight()
    {
        if (cursorColumn < lines[cursorLine].Length)
        {
            cursorColumn++;
            return;
        }

        if (cursorLine < lines.Count - 1)
        {
            cursorLine++;
            cursorColumn = 0;
        }
    }

    private void Insert(char character)
    {
        lines[cursorLine] = lines[cursorLine].Insert(cursorColumn, character.ToString());
        cursorColumn++;
        IsDirty = true;
    }

    private void Backspace()
    {
        if (cursorColumn > 0)
        {
            lines[cursorLine] = lines[cursorLine].Remove(cursorColumn - 1, 1);
            cursorColumn--;
            IsDirty = true;
            return;
        }

        if (cursorLine > 0)
        {
            var previousLength = lines[cursorLine - 1].Length;
            lines[cursorLine - 1] += lines[cursorLine];
            lines.RemoveAt(cursorLine);
            cursorLine--;
            cursorColumn = previousLength;
            IsDirty = true;
        }
    }

    private void Delete()
    {
        if (cursorColumn < lines[cursorLine].Length)
        {
            lines[cursorLine] = lines[cursorLine].Remove(cursorColumn, 1);
            IsDirty = true;
            return;
        }

        if (cursorLine < lines.Count - 1)
        {
            lines[cursorLine] += lines[cursorLine + 1];
            lines.RemoveAt(cursorLine + 1);
            IsDirty = true;
        }
    }

    private void SplitLine()
    {
        var currentLine = lines[cursorLine];
        var before = currentLine[..cursorColumn];
        var after = currentLine[cursorColumn..];
        lines[cursorLine] = before;
        lines.Insert(cursorLine + 1, after);
        cursorLine++;
        cursorColumn = 0;
        IsDirty = true;
    }
}
