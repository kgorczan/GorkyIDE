using GorkyIDE.Abstractions.Editor;

namespace GorkyIDE.Abstractions.Language;

public sealed record Diagnostic(
    DocumentId DocumentId,
    TextRange Range,
    string Severity,
    string Message,
    string? Code);
