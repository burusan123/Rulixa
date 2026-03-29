using Rulixa.Cli;

namespace Rulixa.Application.Tests.Cli;

public sealed class RenderHumanCommandTests
{
    private const string ShellViewModelSymbol = "ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel";

    private static readonly string FixtureRoot = Path.GetFullPath(
        Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Rulixa.Plugin.WpfNet8.Tests",
            "Fixtures",
            "ReferenceWorkspaceLike"));

    [Theory]
    [InlineData("review", "Review Brief")]
    [InlineData("audit", "Audit Snapshot")]
    [InlineData("knowledge", "Design Knowledge Snapshot")]
    public async Task Main_RenderHuman_WritesMarkdownForEachMode(string mode, string title)
    {
        var outputRoot = Path.Combine(Path.GetTempPath(), $"rulixa-render-human-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputRoot);

        try
        {
            var outputPath = Path.Combine(outputRoot, $"{mode}.md");
            var args = new[]
            {
                "render-human",
                "--workspace", FixtureRoot,
                "--entry", $"symbol:{ShellViewModelSymbol}",
                "--goal", "project",
                "--mode", mode,
                "--out", outputPath
            };

            var exitCode = await Program.Main(args);
            var markdown = await File.ReadAllTextAsync(outputPath);

            Assert.Equal(0, exitCode);
            Assert.Contains($"# {title}", markdown, StringComparison.Ordinal);
            Assert.Contains($"- mode: `{mode}`", markdown, StringComparison.Ordinal);

            switch (mode)
            {
                case "review":
                    Assert.Contains("## 概要", markdown, StringComparison.Ordinal);
                    Assert.Contains("## Unknown / Risk", markdown, StringComparison.Ordinal);
                    break;
                case "audit":
                    Assert.Contains("## Root Entry", markdown, StringComparison.Ordinal);
                    Assert.Contains("## Degraded Diagnostics", markdown, StringComparison.Ordinal);
                    break;
                case "knowledge":
                    Assert.Contains("## Subsystem Map", markdown, StringComparison.Ordinal);
                    Assert.Contains("## Known Unknowns", markdown, StringComparison.Ordinal);
                    break;
            }
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Main_RenderHuman_WithEvidenceDir_WritesAuditAndEvidenceBundle()
    {
        var outputRoot = Path.Combine(Path.GetTempPath(), $"rulixa-render-human-evidence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputRoot);

        try
        {
            var outputPath = Path.Combine(outputRoot, "audit.md");
            var evidenceRoot = Path.Combine(outputRoot, "evidence");
            var args = new[]
            {
                "render-human",
                "--workspace", FixtureRoot,
                "--entry", $"symbol:{ShellViewModelSymbol}",
                "--goal", "project",
                "--mode", "audit",
                "--out", outputPath,
                "--evidence-dir", evidenceRoot
            };

            var exitCode = await Program.Main(args);
            var markdown = await File.ReadAllTextAsync(outputPath);
            var evidenceDirectory = Assert.Single(Directory.GetDirectories(evidenceRoot));

            Assert.Equal(0, exitCode);
            Assert.Contains("# Audit Snapshot", markdown, StringComparison.Ordinal);
            Assert.Contains("Evidence Source", markdown, StringComparison.Ordinal);
            Assert.Contains(evidenceDirectory.Replace('\\', '/'), markdown.Replace('\\', '/'), StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(evidenceDirectory, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(evidenceDirectory, "pack.md")));
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Main_RenderHuman_WithInvalidMode_ReturnsUsageError()
    {
        var errorWriter = new StringWriter();
        var originalError = Console.Error;

        try
        {
            Console.SetError(errorWriter);
            var exitCode = await Program.Main(
            [
                "render-human",
                "--workspace", FixtureRoot,
                "--entry", $"symbol:{ShellViewModelSymbol}",
                "--goal", "project",
                "--mode", "invalid"
            ]);

            Assert.Equal(1, exitCode);
            Assert.Contains("review / audit / knowledge", errorWriter.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetError(originalError);
            errorWriter.Dispose();
        }
    }
}
