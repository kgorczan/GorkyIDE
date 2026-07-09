using System.Collections.Concurrent;
using GorkyIDE.Abstractions.Editor;
using GorkyIDE.Abstractions.Language;

namespace GorkyIDE.Core.Diagnostics;

public sealed class DiagnosticStore
{
    private readonly ConcurrentDictionary<DocumentId, IReadOnlyList<Diagnostic>> diagnosticsByDocument = new();

    public void Replace(DocumentId documentId, IReadOnlyList<Diagnostic> diagnostics)
    {
        diagnosticsByDocument[documentId] = diagnostics;
    }

    public IReadOnlyList<Diagnostic> Get(DocumentId documentId)
    {
        return diagnosticsByDocument.TryGetValue(documentId, out var diagnostics)
            ? diagnostics
            : Array.Empty<Diagnostic>();
    }

    public void Clear(DocumentId documentId)
    {
        diagnosticsByDocument.TryRemove(documentId, out _);
    }
}
