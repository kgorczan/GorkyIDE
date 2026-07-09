using GorkyIDE.ModuleSystem.Manifests;

namespace GorkyIDE.ModuleSystem.Discovery;

public sealed record DiscoveredModule(ModuleManifest Manifest, string DirectoryPath, string ManifestPath);
