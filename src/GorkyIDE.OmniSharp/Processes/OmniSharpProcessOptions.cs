namespace GorkyIDE.OmniSharp.Processes;

public sealed record OmniSharpProcessOptions(
    string ExecutablePath,
    string WorkingDirectory,
    TimeSpan StartupTimeout);
