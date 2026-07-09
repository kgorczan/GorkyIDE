using GorkyIDE.ModuleSystem.Manifests;

namespace GorkyIDE.ModuleSystem;

public sealed class ModuleCompatibilityValidator
{
    public bool IsCompatible(ModuleManifest manifest, Version hostVersion)
    {
        if (!Version.TryParse(manifest.MinimumHostVersion, out var minimumHostVersion))
        {
            return false;
        }

        return hostVersion >= minimumHostVersion;
    }
}
