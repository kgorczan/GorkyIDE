using GorkyIDE.Abstractions.Editor;

namespace GorkyIDE.Core.Editor;

public sealed class DocumentSnapshot : IDocumentSnapshot
{
    private readonly string text;

    public DocumentSnapshot(DocumentId id, string path, long version, string text)
    {
        Id = id;
        Path = path;
        Version = version;
        this.text = text;
    }

    public DocumentId Id { get; }

    public string Path { get; }

    public long Version { get; }

    public int Length => text.Length;

    public string GetText() => text;

    public string GetText(int start, int length) => text.Substring(start, length);
}
