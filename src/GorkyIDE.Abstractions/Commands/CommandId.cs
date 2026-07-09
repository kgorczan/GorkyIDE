namespace GorkyIDE.Abstractions.Commands;

public readonly record struct CommandId(string Value)
{
    public override string ToString() => Value;
}
