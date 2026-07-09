namespace GorkyIDE.Abstractions.Editor;

public readonly record struct DocumentId(string Value)
{
    public override string ToString() => Value;
}
