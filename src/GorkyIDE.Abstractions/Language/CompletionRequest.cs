using GorkyIDE.Abstractions.Editor;

namespace GorkyIDE.Abstractions.Language;

public sealed record CompletionRequest(
    IDocumentSnapshot Document,
    TextPosition Position,
    int MaxItems);
