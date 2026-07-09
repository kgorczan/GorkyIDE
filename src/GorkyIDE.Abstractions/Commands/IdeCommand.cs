namespace GorkyIDE.Abstractions.Commands;

public sealed record IdeCommand(
    CommandId Id,
    string DisplayName,
    string Description,
    Func<CommandContext, CancellationToken, ValueTask> ExecuteAsync);
