namespace GorkyIDE.Abstractions.Language;

public interface ICompletionProvider
{
    ValueTask<IReadOnlyList<CompletionItem>> GetCompletionsAsync(
        CompletionRequest request,
        CancellationToken cancellationToken);
}
