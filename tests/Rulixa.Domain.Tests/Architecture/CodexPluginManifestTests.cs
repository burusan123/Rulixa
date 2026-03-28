using System.Text.Json;

namespace Rulixa.Domain.Tests.Architecture;

public sealed class CodexPluginManifestTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void PluginManifest_IsValidJson()
    {
        var manifestPath = Path.Combine(RepoRoot, "plugins", "rulixa", ".codex-plugin", "plugin.json");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));

        Assert.Equal("rulixa", document.RootElement.GetProperty("name").GetString());
        Assert.Equal("Rulixa", document.RootElement.GetProperty("interface").GetProperty("displayName").GetString());
    }
}
