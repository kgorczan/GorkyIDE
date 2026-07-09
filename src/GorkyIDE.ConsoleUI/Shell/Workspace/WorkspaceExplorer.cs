using System.Text;
using GorkyIDE.ConsoleUI.Shell.Workspace;

namespace GorkyIDE.ConsoleUI.Shell.Workspace;

internal sealed class WorkspaceExplorer
{
    private readonly ExplorerNode root;
    private int selectedIndex;
    private int scrollIndex;

    private WorkspaceExplorer(ExplorerNode root)
    {
        this.root = root;
        this.root.IsExpanded = true;
    }

    public static WorkspaceExplorer FromWorkspace(WorkspaceSelection workspace)
    {
        if (!workspace.Exists)
        {
            return new WorkspaceExplorer(new ExplorerNode("No workspace", string.Empty, isDirectory: false, null));
        }

        var rootPath = workspace.Kind is WorkspaceKind.Solution or WorkspaceKind.Project
            ? Path.GetDirectoryName(workspace.Path) ?? workspace.Path
            : workspace.Path;

        var rootName = Path.GetFileName(rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return new WorkspaceExplorer(new ExplorerNode(rootName, rootPath, isDirectory: true, null));
    }

    public void MoveSelection(int delta)
    {
        var visible = FlattenVisible();
        if (visible.Count == 0)
        {
            selectedIndex = 0;
            return;
        }

        selectedIndex = Math.Clamp(selectedIndex + delta, 0, visible.Count - 1);
    }

    public string? ExpandOrOpenSelected()
    {
        var selected = SelectedNode;
        if (selected is null)
        {
            return null;
        }

        if (!selected.IsDirectory)
        {
            return selected.Path;
        }

        selected.IsExpanded = !selected.IsExpanded;
        if (selected.IsExpanded)
        {
            selected.LoadChildren();
        }

        return null;
    }

    public void CollapseSelectedOrMoveToParent()
    {
        var selected = SelectedNode;
        if (selected is null)
        {
            return;
        }

        if (selected.IsDirectory && selected.IsExpanded)
        {
            selected.IsExpanded = false;
            return;
        }

        if (selected.Parent is null)
        {
            return;
        }

        var visible = FlattenVisible();
        var parentIndex = visible.IndexOf(selected.Parent);
        if (parentIndex >= 0)
        {
            selectedIndex = parentIndex;
        }
    }

    public bool TrySelectVisibleRow(int rowOffset)
    {
        if (rowOffset < 0)
        {
            return false;
        }

        var visible = FlattenVisible();
        var targetIndex = scrollIndex + rowOffset;
        if (targetIndex >= visible.Count)
        {
            return false;
        }

        selectedIndex = targetIndex;
        return true;
    }

    public void EnsureSelectionVisible(int visibleHeight)
    {
        if (selectedIndex < scrollIndex)
        {
            scrollIndex = selectedIndex;
        }
        else if (selectedIndex >= scrollIndex + visibleHeight)
        {
            scrollIndex = selectedIndex - visibleHeight + 1;
        }

        scrollIndex = Math.Max(0, scrollIndex);
    }

    public string Render(int visibleHeight)
    {
        var visible = FlattenVisible();
        var text = new StringBuilder();
        var currentIndex = scrollIndex;

        foreach (var node in visible.Skip(scrollIndex).Take(Math.Max(0, visibleHeight)))
        {
            var indent = new string(' ', node.Depth * 2);
            var marker = currentIndex == selectedIndex ? ">" : " ";
            var icon = node.IsDirectory ? node.IsExpanded ? "▾" : "▸" : " ";
            text.Append(marker).Append(' ').Append(indent).Append(icon).Append(' ').Append(node.Name).AppendLine();
            currentIndex++;
        }

        return text.ToString();
    }

    private ExplorerNode? SelectedNode
    {
        get
        {
            var visible = FlattenVisible();
            if (visible.Count == 0)
            {
                return null;
            }

            selectedIndex = Math.Clamp(selectedIndex, 0, visible.Count - 1);
            return visible[selectedIndex];
        }
    }

    private List<ExplorerNode> FlattenVisible()
    {
        var nodes = new List<ExplorerNode>();
        root.AddVisibleNodes(nodes);
        return nodes;
    }

    private sealed class ExplorerNode
    {
        private bool childrenLoaded;
        private List<ExplorerNode> children = new();

        public ExplorerNode(string name, string path, bool isDirectory, ExplorerNode? parent)
        {
            Name = name;
            Path = path;
            IsDirectory = isDirectory;
            Parent = parent;
            Depth = parent is null ? 0 : parent.Depth + 1;
        }

        public string Name { get; }

        public string Path { get; }

        public bool IsDirectory { get; }

        public ExplorerNode? Parent { get; }

        public int Depth { get; }

        public bool IsExpanded { get; set; }

        public void AddVisibleNodes(List<ExplorerNode> nodes)
        {
            nodes.Add(this);
            if (!IsDirectory || !IsExpanded)
            {
                return;
            }

            LoadChildren();
            foreach (var child in children)
            {
                child.AddVisibleNodes(nodes);
            }
        }

        public void LoadChildren()
        {
            if (childrenLoaded || !IsDirectory || !Directory.Exists(Path))
            {
                return;
            }

            var directory = new DirectoryInfo(Path);
            var loadedChildren = new List<ExplorerNode>();
            foreach (var childDirectory in directory.EnumerateDirectories().Where(ShouldShow).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                loadedChildren.Add(new ExplorerNode(childDirectory.Name, childDirectory.FullName, isDirectory: true, this));
            }

            foreach (var file in directory.EnumerateFiles().Where(ShouldShow).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                loadedChildren.Add(new ExplorerNode(file.Name, file.FullName, isDirectory: false, this));
            }

            children = loadedChildren;
            childrenLoaded = true;
        }

        private static bool ShouldShow(FileSystemInfo item)
        {
            return item.Name is not "bin" and not "obj" and not ".git" and not ".vs";
        }
    }
}
