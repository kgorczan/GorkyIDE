using GorkyIDE.ConsoleUI.Shell.Editor;
using GorkyIDE.ConsoleUI.Shell.Formatting;
using GorkyIDE.ConsoleUI.Shell.Input;
using GorkyIDE.ConsoleUI.Shell.Rendering;
using GorkyIDE.ConsoleUI.Shell.Workspace;
using Spectre.Console;

namespace GorkyIDE.ConsoleUI.Shell;

public sealed class ConsoleShell
{
    private const int MaxOpenTabs = 3;
    private const int MaxEditableFileBytes = 1_048_576;

    public Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SafeClear();
        if (args.Length == 0)
        {
            RenderTopMenu();
        }

        var workspace = args.Length > 0
            ? WorkspaceSelection.FromPath(args[0])
            : PromptForWorkspace(cancellationToken);

        var explorer = WorkspaceExplorer.FromWorkspace(workspace);
        var editor = new EditorWorkspace(MaxOpenTabs, MaxEditableFileBytes);
        var formatter = new EditorConfigFormatter();
        var focus = ShellFocus.Explorer;
        var status = "VS shortcuts: Ctrl+S save, Ctrl+K Ctrl+D format, Ctrl+F4 close tab, F6 focus, Q quit.";
        var pendingFormatChord = false;

        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            RenderNonInteractive(workspace, explorer, editor);
            return Task.CompletedTask;
        }

        using var mouseScope = TerminalMouseScope.Enable();
        try
        {
            Console.CursorVisible = false;
            while (!cancellationToken.IsCancellationRequested)
            {
                var metrics = ViewMetrics.FromConsole();
                explorer.EnsureSelectionVisible(metrics.ExplorerContentHeight);
                editor.EnsureCursorVisible(metrics.EditorTextHeight, metrics.EditorTextWidth);
                RenderInteractive(workspace, explorer, editor, focus, status, metrics);

                var input = ShellInputReader.Read();
                if (input.Key is { Key: ConsoleKey.Q })
                {
                    break;
                }

                if (input.MouseClick is { } mouseClick)
                {
                    HandleMouseClick(mouseClick, metrics, explorer, editor, ref focus, ref status);
                    continue;
                }

                if (input.Key is not { } key)
                {
                    continue;
                }

                if (pendingFormatChord)
                {
                    if (key.Key == ConsoleKey.D && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        status = editor.FormatActive(formatter);
                    }
                    else
                    {
                        status = "Format canceled. Use Ctrl+K, Ctrl+D.";
                    }

                    pendingFormatChord = false;
                    continue;
                }

                if (key.Key == ConsoleKey.F6 || key.Key == ConsoleKey.Tab && key.Modifiers == 0)
                {
                    focus = focus == ShellFocus.Explorer ? ShellFocus.Editor : ShellFocus.Explorer;
                    continue;
                }

                if (key.Key == ConsoleKey.S && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    status = editor.SaveActive();
                    continue;
                }

                if (key.Key == ConsoleKey.F4 && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    status = editor.CloseActive();
                    continue;
                }

                if (key.Key == ConsoleKey.K && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    pendingFormatChord = true;
                    status = "Ctrl+K pressed. Press Ctrl+D to format document.";
                    continue;
                }

                if (focus == ShellFocus.Explorer)
                {
                    HandleExplorerKey(key, explorer, editor, ref status);
                    continue;
                }

                status = editor.HandleEditorKey(key);
            }
        }
        finally
        {
            SafeClear();
            Console.CursorVisible = true;
        }

        return Task.CompletedTask;
    }

    private static void HandleExplorerKey(ConsoleKeyInfo key, WorkspaceExplorer explorer, EditorWorkspace editor, ref string status)
    {
        if (key.Key == ConsoleKey.UpArrow)
        {
            explorer.MoveSelection(-1);
            return;
        }

        if (key.Key == ConsoleKey.DownArrow)
        {
            explorer.MoveSelection(1);
            return;
        }

        if (key.Key == ConsoleKey.LeftArrow)
        {
            explorer.CollapseSelectedOrMoveToParent();
            return;
        }

        if (key.Key is ConsoleKey.RightArrow or ConsoleKey.Enter)
        {
            var openedFile = explorer.ExpandOrOpenSelected();
            if (openedFile is not null)
            {
                status = editor.Open(openedFile);
            }
        }
    }

    private static void HandleMouseClick(MouseClick mouseClick, ViewMetrics metrics, WorkspaceExplorer explorer, EditorWorkspace editor, ref ShellFocus focus, ref string status)
    {
        if (metrics.IsInsideExplorer(mouseClick.Column, mouseClick.Row))
        {
            focus = ShellFocus.Explorer;
            if (explorer.TrySelectVisibleRow(mouseClick.Row - metrics.ExplorerContentTop))
            {
                var openedFile = explorer.ExpandOrOpenSelected();
                if (openedFile is not null)
                {
                    status = editor.Open(openedFile);
                    focus = ShellFocus.Editor;
                }
            }

            return;
        }

        if (metrics.IsInsideEditor(mouseClick.Column, mouseClick.Row))
        {
            focus = ShellFocus.Editor;
            if (mouseClick.Row == metrics.EditorTabsTop)
            {
                editor.TrySelectTabByColumn(mouseClick.Column - metrics.EditorContentLeft);
                return;
            }

            editor.MoveCursorToScreenPosition(mouseClick.Row - metrics.EditorTextTop, mouseClick.Column - metrics.EditorTextLeft, metrics.EditorTextHeight, metrics.EditorTextWidth);
        }
    }

    private static void RenderInteractive(WorkspaceSelection workspace, WorkspaceExplorer explorer, EditorWorkspace editor, ShellFocus focus, string status, ViewMetrics metrics)
    {
        SafeClear();
        WriteBox(0, 0, metrics.Width, 3, "GorkyIDE", "Open  Workspace | Recent | Settings | Exit");
        WriteBox(0, metrics.MainTop, metrics.ExplorerWidth, metrics.MainHeight, focus == ShellFocus.Explorer ? "Solution Explorer *" : "Solution Explorer", explorer.Render(metrics.ExplorerContentHeight));
        WriteBox(metrics.EditorLeft, metrics.MainTop, metrics.EditorWidth, metrics.MainHeight, focus == ShellFocus.Editor ? editor.Title + " *" : editor.Title, editor.Render(metrics.EditorTextHeight, metrics.EditorTextWidth));
        WriteBox(0, metrics.BottomTop, metrics.BottomLeftWidth, metrics.BottomHeight, "Diagnostics", "No diagnostics.");
        WriteBox(metrics.BottomLeftWidth, metrics.BottomTop, metrics.Width - metrics.BottomLeftWidth, metrics.BottomHeight, "Output", $"Workspace: {workspace.Kind}\nModules: not loaded\nOmniSharp: not started");
        WriteBox(0, metrics.StatusTop, metrics.Width, metrics.StatusHeight, "Status", status);
    }

    private static void RenderNonInteractive(WorkspaceSelection workspace, WorkspaceExplorer explorer, EditorWorkspace editor)
    {
        AnsiConsole.Write(new Panel("Open  Workspace | Recent | Settings | Exit").Header("GorkyIDE"));
        AnsiConsole.Write(new Panel(explorer.Render(18)).Header("Solution Explorer"));
        AnsiConsole.Write(new Panel(editor.Render(18, 100)).Header(editor.Title));
        AnsiConsole.MarkupLine($"[yellow]Workspace:[/] {Markup.Escape(workspace.Path)}");
        AnsiConsole.MarkupLine("[grey]Run in an interactive terminal for mouse, tabs, scrolling, and editing.[/]");
    }

    private static void WriteBox(int left, int top, int width, int height, string title, string content)
    {
        if (width < 4 || height < 3)
        {
            return;
        }

        var safeTitle = title.Length > width - 4 ? title[..Math.Max(0, width - 4)] : title;
        WriteAt(left, top, "┌" + "─" + safeTitle + new string('─', Math.Max(0, width - safeTitle.Length - 3)) + "┐", width);
        var contentLines = content.Replace("\r", string.Empty).Split('\n');
        for (var rowOffset = 1; rowOffset < height - 1; rowOffset++)
        {
            var line = rowOffset - 1 < contentLines.Length ? contentLines[rowOffset - 1] : string.Empty;
            WriteAt(left, top + rowOffset, "│" + Fit(line, width - 2) + "│", width);
        }

        WriteAt(left, top + height - 1, "└" + new string('─', width - 2) + "┘", width);
    }

    private static void WriteAt(int left, int top, string value, int width)
    {
        if (top < 0 || top >= Console.WindowHeight || left < 0 || left >= Console.WindowWidth)
        {
            return;
        }

        Console.SetCursorPosition(left, top);
        Console.Write(Fit(value, Math.Min(width, Console.WindowWidth - left)));
    }

    private static string Fit(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var plainValue = value.Length > width ? value[..width] : value;
        return plainValue.PadRight(width);
    }

    private static void SafeClear()
    {
        try
        {
            AnsiConsole.Clear();
        }
        catch (IOException)
        {
        }
    }

    private static void RenderTopMenu()
    {
        AnsiConsole.Write(new Panel("[bold]Open[/]  Workspace | Recent | Settings | Exit").Header("GorkyIDE"));
    }

    private static WorkspaceSelection PromptForWorkspace(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var action = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("What do you want to do?").AddChoices("Open folder / .sln / .csproj", "Exit"));
            if (action == "Exit")
            {
                Environment.ExitCode = 0;
                return WorkspaceSelection.None;
            }

            var path = AnsiConsole.Ask<string>("Path to [green]folder[/], [green].sln[/], or [green].csproj[/]:");
            var workspace = WorkspaceSelection.FromPath(path);
            if (workspace.Exists)
            {
                return workspace;
            }

            AnsiConsole.MarkupLine("[red]Path does not exist. Please try again.[/]");
        }
    }
}



