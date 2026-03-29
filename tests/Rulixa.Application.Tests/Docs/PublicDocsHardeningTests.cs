using System.Text.Json;

namespace Rulixa.Application.Tests.Docs;

public sealed class PublicDocsHardeningTests
{
    private static readonly string RepositoryRoot = GetRepositoryRoot();

    [Fact]
    public void PublicFacingDocs_DoNotContainLocalAbsoluteLinks()
    {
        var files = EnumeratePublicFacingFiles().ToArray();

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("/D:/", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/C:/", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void PublicFacingExamples_ContainInputReadingAndOutputGuidance()
    {
        var sample = File.ReadAllText(Path.Combine(RepositoryRoot, "docs", "spec", "phase1", "examples", "sample_shell_pack_example.md"));
        var skill = File.ReadAllText(Path.Combine(RepositoryRoot, "plugins", "rulixa", "skills", "pack", "SKILL.md"));

        Assert.Contains("## 入力", sample, StringComparison.Ordinal);
        Assert.Contains("## 期待する読み方", sample, StringComparison.Ordinal);
        Assert.Contains("## 出力の見方", sample, StringComparison.Ordinal);

        Assert.Contains("## 入力", skill, StringComparison.Ordinal);
        Assert.Contains("## 基本コマンド", skill, StringComparison.Ordinal);
        Assert.Contains("## 効果的な使い方", skill, StringComparison.Ordinal);

        var readme = File.ReadAllText(Path.Combine(RepositoryRoot, "README.md"));
        var fullSpec = File.ReadAllText(Path.Combine(RepositoryRoot, "docs", "project_full_spec.md"));

        Assert.Contains("release-review.md", readme, StringComparison.Ordinal);
        Assert.Contains("human-outputs", readme, StringComparison.Ordinal);
        Assert.Contains("release-review.md", fullSpec, StringComparison.Ordinal);
        Assert.Contains("human-outputs", fullSpec, StringComparison.Ordinal);
    }

    [Fact]
    public void PluginMetadata_IsValidJsonAndDoesNotContainLocalAbsoluteLinks()
    {
        var pluginJsonPath = Path.Combine(RepositoryRoot, "plugins", "rulixa", ".codex-plugin", "plugin.json");
        var json = File.ReadAllText(pluginJsonPath);

        Assert.DoesNotContain("/D:/", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/C:/", json, StringComparison.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("rulixa", document.RootElement.GetProperty("name").GetString());
        Assert.Equal("Rulixa", document.RootElement.GetProperty("interface").GetProperty("displayName").GetString());
        Assert.Contains("render-human", document.RootElement.GetProperty("interface").GetProperty("longDescription").GetString(), StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumeratePublicFacingFiles()
    {
        yield return Path.Combine(RepositoryRoot, "README.md");
        yield return Path.Combine(RepositoryRoot, "docs", "product-readiness.md");
        yield return Path.Combine(RepositoryRoot, "docs", "project_full_spec.md");
        yield return Path.Combine(RepositoryRoot, "plugins", "rulixa", ".codex-plugin", "plugin.json");

        foreach (var file in Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "docs", "spec"), "*.md", SearchOption.AllDirectories))
        {
            yield return file;
        }

        foreach (var file in Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "plugins", "rulixa", "skills"), "*.md", SearchOption.AllDirectories))
        {
            yield return file;
        }
    }

    private static string GetRepositoryRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
