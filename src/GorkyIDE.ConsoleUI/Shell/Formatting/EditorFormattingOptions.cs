namespace GorkyIDE.ConsoleUI.Shell.Formatting;

internal sealed record EditorFormattingOptions(
    string IndentStyle,
    int IndentSize,
    int TabWidth,
    string EndOfLine,
    bool TrimTrailingWhitespace,
    bool InsertFinalNewLine)
{
    public static EditorFormattingOptions Default { get; } = new(
        IndentStyle: "space",
        IndentSize: 4,
        TabWidth: 4,
        EndOfLine: OperatingSystem.IsWindows() ? "crlf" : "lf",
        TrimTrailingWhitespace: true,
        InsertFinalNewLine: true);

    public string LineEnding => EndOfLine.ToLowerInvariant() switch
    {
        "crlf" => "\r\n",
        "cr" => "\r",
        _ => "\n"
    };
}
