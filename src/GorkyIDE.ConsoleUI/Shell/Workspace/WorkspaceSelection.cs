namespace GorkyIDE.ConsoleUI.Shell.Workspace;

internal sealed record WorkspaceSelection(string Path, WorkspaceKind Kind, bool Exists)
{
    public static WorkspaceSelection None { get; } = new(string.Empty, WorkspaceKind.None, false);

    public static WorkspaceSelection FromPath(string path)
    {
        var fullPath = System.IO.Path.GetFullPath(path.Trim().Trim('"'));

        if (Directory.Exists(fullPath))
        {
            return new WorkspaceSelection(fullPath, WorkspaceKind.Folder, true);
        }

        if (File.Exists(fullPath))
        {
            var extension = System.IO.Path.GetExtension(fullPath);
            var kind = extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
                ? WorkspaceKind.Solution
                : extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
                    ? WorkspaceKind.Project
                    : WorkspaceKind.File;

            return new WorkspaceSelection(fullPath, kind, true);
        }

        return new WorkspaceSelection(fullPath, WorkspaceKind.None, false);
    }
}
