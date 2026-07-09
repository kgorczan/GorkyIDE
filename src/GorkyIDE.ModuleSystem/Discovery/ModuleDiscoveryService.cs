using GorkyIDE.ModuleSystem.Manifests;

namespace GorkyIDE.ModuleSystem.Discovery;

public sealed class ModuleDiscoveryService
{
    private readonly ModuleManifestReader manifestReader;

    public ModuleDiscoveryService(ModuleManifestReader manifestReader)
    {
        this.manifestReader = manifestReader;
    }

    public async ValueTask<IReadOnlyList<DiscoveredModule>> DiscoverAsync(string modulesRoot, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(modulesRoot))
        {
            return Array.Empty<DiscoveredModule>();
        }

        var manifestPaths = Directory.EnumerateFiles(modulesRoot, "module.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var modules = new List<DiscoveredModule>(manifestPaths.Length);

        foreach (var manifestPath in manifestPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var manifest = await manifestReader.ReadAsync(manifestPath, cancellationToken).ConfigureAwait(false);
            modules.Add(new DiscoveredModule(manifest, Path.GetDirectoryName(manifestPath)!, manifestPath));
        }

        return modules;
    }
}
