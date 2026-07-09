using System.Collections.Concurrent;
using GorkyIDE.Abstractions.Commands;

namespace GorkyIDE.Core.Commands;

public sealed class CommandRegistry : ICommandRegistry
{
    private readonly ConcurrentDictionary<CommandId, IdeCommand> commands = new();

    public bool TryRegister(IdeCommand command) => commands.TryAdd(command.Id, command);

    public bool TryGet(CommandId id, out IdeCommand command) => commands.TryGetValue(id, out command!);

    public IReadOnlyCollection<IdeCommand> List() => commands.Values.OrderBy(command => command.Id.Value, StringComparer.Ordinal).ToArray();
}
