namespace GorkyIDE.OmniSharp.Processes;

public interface IOmniSharpProcess : IAsyncDisposable
{
    bool IsRunning { get; }

    ValueTask StartAsync(OmniSharpProcessOptions options, CancellationToken cancellationToken);

    ValueTask StopAsync(CancellationToken cancellationToken);
}
