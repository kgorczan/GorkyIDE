namespace GorkyIDE.ConsoleUI.Shell.Input;

internal readonly record struct ShellInput(ConsoleKeyInfo? Key, MouseClick? MouseClick)
{
    public static ShellInput None { get; } = new(null, null);

    public static ShellInput FromKey(ConsoleKeyInfo key) => new(key, null);

    public static ShellInput FromMouse(MouseClick click) => new(null, click);
}
