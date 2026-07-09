namespace GorkyIDE.Abstractions.Editor;

public interface IDocumentSnapshot
{
    DocumentId Id { get; }

    string Path { get; }

    long Version { get; }

    int Length { get; }

    string GetText();

    string GetText(int start, int length);
}
