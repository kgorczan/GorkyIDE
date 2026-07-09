using Spectre.Console;

namespace GorkyIDE.ConsoleUI.Shell;

public sealed class ConsoleShell
{
    public Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Main").Ratio(4),
                new Layout("Bottom").Ratio(1),
                new Layout("Status").Size(3));

        layout["Main"].SplitColumns(
            new Layout("Files").Ratio(1),
            new Layout("Editor").Ratio(3));

        layout["Bottom"].SplitColumns(
            new Layout("Diagnostics").Ratio(1),
            new Layout("Output").Ratio(1));

        layout["Files"].Update(new Panel("No workspace opened yet.").Header("Files"));
        layout["Editor"].Update(new Panel("Welcome to GorkyIDE").Header("Editor"));
        layout["Diagnostics"].Update(new Panel("No diagnostics.").Header("Diagnostics"));
        layout["Output"].Update(new Panel("Modules: not loaded\nOmniSharp: not started").Header("Output"));
        layout["Status"].Update(new Panel("Ready | Ctrl+C to exit").Header("Status"));

        AnsiConsole.Write(layout);
        return Task.CompletedTask;
    }
}
