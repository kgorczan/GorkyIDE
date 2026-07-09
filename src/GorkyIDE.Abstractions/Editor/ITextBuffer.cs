namespace GorkyIDE.Abstractions.Editor;

public interface ITextBuffer
{
    DocumentId Id { get; }

    long Version { get; }

    int Length { get; }

    void Insert(int index, ReadOnlySpan<char> text);

    void Delete(int index, int length);

    IDocumentSnapshot CreateSnapshot();
}
