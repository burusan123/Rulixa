namespace Rulixa.Application.Tests.Docs;

public sealed class PublicDocsHardeningTests
{
    private static readonly string RepositoryRoot = GetRepositoryRoot();

    [Fact]
    public void PublicDocs_DoNotContainLocalAbsoluteLinks()
    {
        var files = EnumeratePublicMarkdownFiles().ToArray();

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("/D:/", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/C:/", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void SampleDocs_ContainMeaningfulExamples()
    {
        var sample = File.ReadAllText(Path.Combine(RepositoryRoot, "docs", "spec", "phase1", "examples", "sample_shell_pack_example.md"));
        var skill = File.ReadAllText(Path.Combine(RepositoryRoot, "plugins", "rulixa", "skills", "pack", "SKILL.md"));

        Assert.Contains("## 入力", sample, StringComparison.Ordinal);
        Assert.Contains("## 期待する selected snippets", sample, StringComparison.Ordinal);
        Assert.Contains("## 基本コマンド", skill, StringComparison.Ordinal);
        Assert.Contains("## 効果的な使い方", skill, StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumeratePublicMarkdownFiles()
    {
        yield return Path.Combine(RepositoryRoot, "README.md");

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
