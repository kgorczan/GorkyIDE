namespace GorkyIDE.ModuleSystem.Manifests;

public sealed record ModuleManifest(
    string Id,
    string Name,
    string Version,
    string EntryAssembly,
    string MinimumHostVersion,
    IReadOnlyList<ModuleDependency> Dependencies);
