namespace GorkyIDE.Abstractions.Language;

public sealed record CompletionItem(
    string Label,
    string InsertText,
    string? Detail,
    string? Kind);
