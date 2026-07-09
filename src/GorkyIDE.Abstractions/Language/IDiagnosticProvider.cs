using GorkyIDE.Abstractions.Editor;

namespace GorkyIDE.Abstractions.Language;

public interface IDiagnosticProvider
{
    ValueTask<IReadOnlyList<Diagnostic>> GetDiagnosticsAsync(
        IDocumentSnapshot document,
        CancellationToken cancellationToken);
}
