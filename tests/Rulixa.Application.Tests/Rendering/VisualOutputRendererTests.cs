using Rulixa.Application.HumanOutputs;
using Rulixa.Infrastructure.Rendering;

namespace Rulixa.Application.Tests.Rendering;

public sealed class VisualOutputRendererTests
{
    [Fact]
    public async Task RenderAsync_WritesHtmlCssJsWithEmbeddedJson()
    {
        var renderer = new VisualOutputRenderer();
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"rulixa-visual-renderer-{Guid.NewGuid():N}");

        try
        {
            var result = await renderer.RenderAsync(CreateDocument(), outputDirectory);
            var html = await File.ReadAllTextAsync(result.IndexPath);
            var css = await File.ReadAllTextAsync(result.CssPath);
            var script = await File.ReadAllTextAsync(result.JavaScriptPath);

            Assert.True(File.Exists(result.IndexPath));
            Assert.True(File.Exists(result.CssPath));
            Assert.True(File.Exists(result.JavaScriptPath));
            Assert.Contains("id=\"rulixa-visual-data\"", html, StringComparison.Ordinal);
            Assert.Contains("app.css", html, StringComparison.Ordinal);
            Assert.Contains("app.js", html, StringComparison.Ordinal);
            Assert.Contains("data-role=\"search-input\"", html, StringComparison.Ordinal);
            Assert.Contains("data-role=\"view-nav\"", html, StringComparison.Ordinal);
            Assert.DoesNotContain("fetch(", script, StringComparison.Ordinal);
            Assert.Contains("--accent", css, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static VisualOutputDocument CreateDocument()
    {
        var overviewSection = new VisualSection(
            Id: "overview-root",
            Title: "Root Summary",
            Kind: "facts",
            Collapsible: false,
            Items:
            [
                new VisualItem("root", "ShellViewModel", "summary", "search", "root-entry", ["root"])
            ],
            Graph: null);

        var workflowSection = new VisualSection(
            Id: "workflow-routes",
            Title: "Representative Routes",
            Kind: "graph-list",
            Collapsible: false,
            Items:
            [
                new VisualItem("route-1", "Route 1", "Shell -> Workflow -> ProjectDocument", "Shell Workflow ProjectDocument", null, ["route"])
            ],
            Graph: new VisualGraph(
                "Representative Routes",
                [
                    new VisualGraphNode("shell", "Shell", true),
                    new VisualGraphNode("workflow", "Workflow", false)
                ],
                [
                    new VisualGraphEdge("shell", "workflow", "flows-to")
                ]));

        return new VisualOutputDocument(
            Title: "Rulixa Visual Output",
            Header: new VisualHeaderSummary(
                RootEntry: "symbol:App.ViewModels.ShellViewModel",
                Goal: "project",
                ResolvedKind: "Symbol",
                ResolvedPath: "src/App/ViewModels/ShellViewModel.cs",
                ResolvedSymbol: "App.ViewModels.ShellViewModel",
                SystemSummary: "Shell / Workflow / Evidence",
                CenterStates: ["ProjectDocument"],
                NextCandidates: ["App.Services.DraftingAnalyzer"],
                PrimaryUnknown: "unknown: downstream"),
            Views:
            [
                new VisualView("overview", "Overview", "summary", [overviewSection]),
                new VisualView("workflow", "Workflow", "routes", [workflowSection]),
                new VisualView("evidence", "Evidence", "evidence", []),
                new VisualView("unknowns", "Unknowns", "unknowns", []),
                new VisualView("architecture", "Architecture", "architecture", [])
            ],
            InspectorItems: new Dictionary<string, VisualInspectorItem>(StringComparer.Ordinal)
            {
                ["root-entry"] = new VisualInspectorItem(
                    "root-entry",
                    "ShellViewModel",
                    "root",
                    ["fact"],
                    "src/App/ViewModels/ShellViewModel.cs",
                    "App.ViewModels.ShellViewModel",
                    "snippet",
                    "reason",
                    ["candidate"])
            },
            InitialInspectorId: "root-entry");
    }
}
