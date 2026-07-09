namespace GorkyIDE.ConsoleUI.Shell.Formatting;

internal sealed class EditorConfigFormatter
{
    public EditorFormattingOptions LoadForFile(string filePath)
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var editorConfigPath in FindEditorConfigFiles(filePath))
        {
            ApplyEditorConfig(editorConfigPath, filePath, settings);
        }

        return CreateOptions(settings);
    }

    private static IEnumerable<string> FindEditorConfigFiles(string filePath)
    {
        var stack = new Stack<string>();
        var directory = new DirectoryInfo(Path.GetDirectoryName(filePath) ?? Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var editorConfigPath = Path.Combine(directory.FullName, ".editorconfig");
            if (File.Exists(editorConfigPath))
            {
                stack.Push(editorConfigPath);
            }

            directory = directory.Parent;
        }

        return stack;
    }

    private static void ApplyEditorConfig(string editorConfigPath, string filePath, Dictionary<string, string> settings)
    {
        var currentSectionApplies = true;
        foreach (var rawLine in File.ReadLines(editorConfigPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                var pattern = line[1..^1].Trim();
                currentSectionApplies = GlobMatches(pattern, filePath, Path.GetDirectoryName(editorConfigPath) ?? string.Empty);

                continue;
            }

            if (!currentSectionApplies)
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            settings[key] = value;
        }
    }

    private static bool GlobMatches(string pattern, string filePath, string editorConfigDirectory)
    {
        if (pattern == "*")
        {
            return true;
        }

        var fileName = Path.GetFileName(filePath);
        if (pattern.StartsWith("*.", StringComparison.Ordinal))
        {
            return fileName.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);
        }

        if (!pattern.Contains('/') && !pattern.Contains('\\'))
        {
            return string.Equals(pattern, fileName, StringComparison.OrdinalIgnoreCase);
        }

        var relativePath = Path.GetRelativePath(editorConfigDirectory, filePath).Replace('\\', '/');
        return SimpleWildcardMatch(relativePath, pattern.Replace('\\', '/'));
    }

    private static bool SimpleWildcardMatch(string value, string pattern)
    {
        var valueIndex = 0;
        var patternIndex = 0;
        var starIndex = -1;
        var matchIndex = 0;

        while (valueIndex < value.Length)
        {
            if (patternIndex < pattern.Length && (pattern[patternIndex] == '?' || char.ToUpperInvariant(pattern[patternIndex]) == char.ToUpperInvariant(value[valueIndex])))
            {
                valueIndex++;
                patternIndex++;
            }
            else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                starIndex = patternIndex++;
                matchIndex = valueIndex;
            }
            else if (starIndex != -1)
            {
                patternIndex = starIndex + 1;
                valueIndex = ++matchIndex;
            }
            else
            {
                return false;
            }
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }

    private static EditorFormattingOptions CreateOptions(IReadOnlyDictionary<string, string> settings)
    {
        var defaults = EditorFormattingOptions.Default;
        var indentStyle = Get(settings, "indent_style", defaults.IndentStyle).Equals("tab", StringComparison.OrdinalIgnoreCase) ? "tab" : "space";
        var tabWidth = GetInt(settings, "tab_width", defaults.TabWidth);
        var indentSize = Get(settings, "indent_size", defaults.IndentSize.ToString()).Equals("tab", StringComparison.OrdinalIgnoreCase)
            ? tabWidth
            : GetInt(settings, "indent_size", defaults.IndentSize);
        var endOfLine = Get(settings, "end_of_line", defaults.EndOfLine).ToLowerInvariant();

        return new EditorFormattingOptions(
            indentStyle,
            Math.Max(1, indentSize),
            Math.Max(1, tabWidth),
            endOfLine is "lf" or "crlf" or "cr" ? endOfLine : defaults.EndOfLine,
            GetBool(settings, "trim_trailing_whitespace", defaults.TrimTrailingWhitespace),
            GetBool(settings, "insert_final_newline", defaults.InsertFinalNewLine));
    }

    private static string Get(IReadOnlyDictionary<string, string> settings, string key, string fallback)
    {
        return settings.TryGetValue(key, out var value) ? value : fallback;
    }

    private static int GetInt(IReadOnlyDictionary<string, string> settings, string key, int fallback)
    {
        return settings.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static bool GetBool(IReadOnlyDictionary<string, string> settings, string key, bool fallback)
    {
        return settings.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
