using System.Text.Json;

namespace Rulixa.Cli;

internal sealed class EvidenceBundleReader(JsonSerializerOptions jsonOptions)
{
    public async Task<EvidenceManifestDto> ReadAsync(string bundleDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bundleDirectory);

        var fullDirectoryPath = Path.GetFullPath(bundleDirectory);
        var manifestPath = Path.Combine(fullDirectoryPath, EvidenceBundleConventions.ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            throw new ArgumentException($"manifest が見つかりません: {manifestPath}", nameof(bundleDirectory));
        }

        var json = await File.ReadAllTextAsync(manifestPath).ConfigureAwait(false);
        var manifest = JsonSerializer.Deserialize<EvidenceManifestDto>(json, jsonOptions)
            ?? throw new ArgumentException($"manifest を読み取れませんでした: {manifestPath}", nameof(bundleDirectory));

        if (!string.Equals(manifest.SchemaVersion, EvidenceBundleConventions.SchemaVersion, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"未対応の evidence schema です: {manifest.SchemaVersion}",
                nameof(bundleDirectory));
        }

        return manifest;
    }
}
