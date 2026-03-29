using Rulixa.Cli;

namespace Rulixa.Application.Tests.Cli;

public sealed class RenderVisualCommandTests
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

    [Fact]
    public async Task Main_RenderVisual_WritesArtifactDirectory()
    {
        var outputRoot = Path.Combine(Path.GetTempPath(), $"rulixa-render-visual-{Guid.NewGuid():N}");

        try
        {
            var exitCode = await Program.Main(
            [
                "render-visual",
                "--workspace", FixtureRoot,
                "--entry", $"symbol:{ShellViewModelSymbol}",
                "--goal", "project",
                "--out-dir", outputRoot
            ]);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(outputRoot, "index.html")));
            Assert.True(File.Exists(Path.Combine(outputRoot, "app.css")));
            Assert.True(File.Exists(Path.Combine(outputRoot, "app.js")));

            var html = await File.ReadAllTextAsync(Path.Combine(outputRoot, "index.html"));
            Assert.Contains("Overview", html, StringComparison.Ordinal);
            Assert.Contains("Workflow", html, StringComparison.Ordinal);
            Assert.Contains("Evidence", html, StringComparison.Ordinal);
            Assert.Contains("Unknowns", html, StringComparison.Ordinal);
            Assert.Contains("Architecture", html, StringComparison.Ordinal);
            Assert.Contains("rulixa-visual-data", html, StringComparison.Ordinal);
            Assert.DoesNotContain("/D:/", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("D:\\", html, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }
}
