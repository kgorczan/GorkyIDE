using System.Runtime.InteropServices;

namespace GorkyIDE.ConsoleUI.Shell.Input;

internal sealed class TerminalMouseScope : IDisposable
{
    private TerminalMouseScope()
    {
    }

    public static TerminalMouseScope Enable()
    {
        TryEnableWindowsVirtualTerminalInput();
        Console.Write("\u001b[?1000h\u001b[?1002h\u001b[?1006h");
        return new TerminalMouseScope();
    }

    public void Dispose()
    {
        Console.Write("\u001b[?1006l\u001b[?1002l\u001b[?1000l");
    }

    private static void TryEnableWindowsVirtualTerminalInput()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var inputHandle = GetStdHandle(-10);
        if (inputHandle == IntPtr.Zero || inputHandle == new IntPtr(-1))
        {
            return;
        }

        if (GetConsoleMode(inputHandle, out var mode))
        {
            SetConsoleMode(inputHandle, mode | 0x0200);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int standardHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr handle, out int mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr handle, int mode);
}
