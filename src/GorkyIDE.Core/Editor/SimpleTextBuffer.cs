using System.Text;
using GorkyIDE.Abstractions.Editor;

namespace GorkyIDE.Core.Editor;

public sealed class SimpleTextBuffer : ITextBuffer
{
    private readonly object gate = new();
    private readonly StringBuilder text;
    private long version;

    public SimpleTextBuffer(DocumentId id, string path, string initialText)
    {
        Id = id;
        Path = path;
        text = new StringBuilder(initialText);
    }

    public DocumentId Id { get; }

    public string Path { get; }

    public long Version
    {
        get
        {
            lock (gate)
            {
                return version;
            }
        }
    }

    public int Length
    {
        get
        {
            lock (gate)
            {
                return text.Length;
            }
        }
    }

    public void Insert(int index, ReadOnlySpan<char> value)
    {
        lock (gate)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (index > text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            text.Insert(index, value);
            version++;
        }
    }

    public void Delete(int index, int length)
    {
        lock (gate)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            if (index + length > text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            text.Remove(index, length);
            version++;
        }
    }

    public IDocumentSnapshot CreateSnapshot()
    {
        lock (gate)
        {
            return new DocumentSnapshot(Id, Path, version, text.ToString());
        }
    }
}
