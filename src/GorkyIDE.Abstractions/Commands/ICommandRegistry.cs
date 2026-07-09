namespace GorkyIDE.Abstractions.Commands;

public interface ICommandRegistry
{
    bool TryRegister(IdeCommand command);

    bool TryGet(CommandId id, out IdeCommand command);

    IReadOnlyCollection<IdeCommand> List();
}
