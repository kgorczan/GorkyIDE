namespace GorkyIDE.ConsoleUI.Shell.Rendering;

internal sealed record ViewMetrics(
    int Width,
    int Height,
    int MainTop,
    int MainHeight,
    int ExplorerWidth,
    int EditorLeft,
    int EditorWidth,
    int BottomTop,
    int BottomHeight,
    int BottomLeftWidth,
    int StatusTop,
    int StatusHeight)
{
    public int ExplorerContentTop => MainTop + 1;

    public int ExplorerContentHeight => Math.Max(0, MainHeight - 2);

    public int EditorContentLeft => EditorLeft + 1;

    public int EditorTabsTop => MainTop + 1;

    public int EditorTextTop => MainTop + 2;

    public int EditorTextLeft => EditorLeft + 1;

    public int EditorTextHeight => Math.Max(0, MainHeight - 3);

    public int EditorTextWidth => Math.Max(0, EditorWidth - 2);

    public static ViewMetrics FromConsole()
    {
        var width = Math.Max(80, Console.WindowWidth);
        var height = Math.Max(24, Console.WindowHeight);
        const int menuHeight = 3;
        const int bottomHeight = 5;
        const int statusHeight = 3;
        var mainTop = menuHeight;
        var mainHeight = Math.Max(8, height - menuHeight - bottomHeight - statusHeight);
        var explorerWidth = Math.Clamp(width / 4, 26, 44);
        var editorLeft = explorerWidth;
        var editorWidth = width - explorerWidth;
        var bottomTop = mainTop + mainHeight;
        var bottomLeftWidth = width / 2;
        var statusTop = bottomTop + bottomHeight;

        return new ViewMetrics(width, height, mainTop, mainHeight, explorerWidth, editorLeft, editorWidth, bottomTop, bottomHeight, bottomLeftWidth, statusTop, statusHeight);
    }

    public bool IsInsideExplorer(int column, int row)
    {
        return column > 0 && column < ExplorerWidth - 1 && row >= ExplorerContentTop && row < ExplorerContentTop + ExplorerContentHeight;
    }

    public bool IsInsideEditor(int column, int row)
    {
        return column > EditorLeft && column < Width - 1 && row > MainTop && row < MainTop + MainHeight - 1;
    }
}
