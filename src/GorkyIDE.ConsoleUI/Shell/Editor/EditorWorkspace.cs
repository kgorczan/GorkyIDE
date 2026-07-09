using GorkyIDE.ConsoleUI.Shell.Formatting;

namespace GorkyIDE.ConsoleUI.Shell.Editor;

internal sealed class EditorWorkspace
{
    private readonly int maxOpenTabs;
    private readonly int maxEditableFileBytes;
    private readonly List<EditorTab> tabs = new();
    private int activeIndex;

    public EditorWorkspace(int maxOpenTabs, int maxEditableFileBytes)
    {
        this.maxOpenTabs = maxOpenTabs;
        this.maxEditableFileBytes = maxEditableFileBytes;
    }

    public string Title => ActiveTab is null ? "Editor" : $"Editor - {ActiveTab.Title}";

    public string Open(string path)
    {
        var existingIndex = tabs.FindIndex(tab => tab.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            activeIndex = existingIndex;
            return $"Activated {Path.GetFileName(path)}.";
        }

        if (tabs.Count >= maxOpenTabs)
        {
            return $"Maximum of {maxOpenTabs} open files reached. Close a tab with Ctrl+F4.";
        }

        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > maxEditableFileBytes)
        {
            return "File is too large for the current in-terminal editor.";
        }

        tabs.Add(EditorTab.Open(path));
        activeIndex = tabs.Count - 1;
        return $"Opened {Path.GetFileName(path)}.";
    }

    public string CloseActive()
    {
        if (ActiveTab is null)
        {
            return "No open tab.";
        }

        var closedName = ActiveTab.Title;
        tabs.RemoveAt(activeIndex);
        activeIndex = Math.Clamp(activeIndex, 0, Math.Max(0, tabs.Count - 1));
        return $"Closed {closedName}.";
    }

    public string SaveActive()
    {
        if (ActiveTab is null)
        {
            return "No open tab to save.";
        }

        ActiveTab.Save();
        return $"Saved {ActiveTab.Title}.";
    }

    public string FormatActive(EditorConfigFormatter formatter)
    {
        if (ActiveTab is null)
        {
            return "No open tab to format.";
        }

        var options = formatter.LoadForFile(ActiveTab.Path);
        return ActiveTab.Format(options);
    }

    public string HandleEditorKey(ConsoleKeyInfo key)
    {
        return ActiveTab is null ? "Open a file from Solution Explorer first." : ActiveTab.HandleKey(key);
    }

    public void EnsureCursorVisible(int visibleHeight, int visibleWidth)
    {
        ActiveTab?.EnsureCursorVisible(visibleHeight, visibleWidth);
    }

    public void MoveCursorToScreenPosition(int rowOffset, int columnOffset, int visibleHeight, int visibleWidth)
    {
        ActiveTab?.MoveCursorToScreenPosition(rowOffset, columnOffset, visibleHeight, visibleWidth);
    }

    public bool TrySelectTabByColumn(int columnOffset)
    {
        var currentColumn = 0;
        for (var tabIndex = 0; tabIndex < tabs.Count; tabIndex++)
        {
            var label = TabLabel(tabIndex);
            if (columnOffset >= currentColumn && columnOffset < currentColumn + label.Length)
            {
                activeIndex = tabIndex;
                return true;
            }

            currentColumn += label.Length + 1;
        }

        return false;
    }

    public string Render(int visibleHeight, int visibleWidth)
    {
        if (ActiveTab is null)
        {
            return "No file open. Click a file in Solution Explorer or select it and press Enter.";
        }

        return RenderTabs(visibleWidth) + Environment.NewLine + ActiveTab.Render(Math.Max(0, visibleHeight - 1), visibleWidth);
    }

    private EditorTab? ActiveTab => tabs.Count == 0 ? null : tabs[activeIndex];

    private string RenderTabs(int visibleWidth)
    {
        var labels = Enumerable.Range(0, tabs.Count).Select(TabLabel);
        var text = string.Join(' ', labels);
        return text.Length > visibleWidth ? text[..visibleWidth] : text.PadRight(visibleWidth);
    }

    private string TabLabel(int tabIndex)
    {
        var tab = tabs[tabIndex];
        var activeMarker = tabIndex == activeIndex ? "*" : " ";
        var dirtyMarker = tab.IsDirty ? "●" : string.Empty;
        return $"[{activeMarker}{tab.Title}{dirtyMarker}]";
    }
}

