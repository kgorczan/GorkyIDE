namespace GorkyIDE.Abstractions.Commands;

public sealed class CommandContext
{
    public CommandContext(IServiceProvider services, IReadOnlyDictionary<string, string> arguments)
    {
        Services = services;
        Arguments = arguments;
    }

    public IServiceProvider Services { get; }

    public IReadOnlyDictionary<string, string> Arguments { get; }
}
