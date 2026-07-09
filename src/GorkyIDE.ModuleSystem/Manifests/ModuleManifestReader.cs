using System.Text.Json;

namespace GorkyIDE.ModuleSystem.Manifests;

public sealed class ModuleManifestReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<ModuleManifest> ReadAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var manifest = await JsonSerializer.DeserializeAsync<ModuleManifest>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        


        return manifest ?? throw new InvalidDataException($"Module manifest '{path}' is empty or invalid.");
    }
}