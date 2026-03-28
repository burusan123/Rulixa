using System.Text.Json;
using System.Text.Json.Serialization;
using Rulixa.Cli;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.Tests.Cli;

public sealed class PackEvidenceBundleTests
{
    private const string ShellViewModelSymbol = "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel";

    private static readonly string FixtureRoot = Path.GetFullPath(
        Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Rulixa.Plugin.WpfNet8.Tests",
            "Fixtures",
            "AssessMeisterLike"));

    [Fact]
    public async Task Main_WithEvidenceDir_WritesStableEvidenceBundle()
    {
        var outputRoot = Path.Combine(Path.GetTempPath(), $"rulixa-evidence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputRoot);

        try
        {
            var packOutputPath = Path.Combine(outputRoot, "pack-output.md");
            var args = new[]
            {
                "pack",
                "--workspace", FixtureRoot,
                "--entry", $"symbol:{ShellViewModelSymbol}",
                "--goal", "project",
                "--out", packOutputPath,
                "--evidence-dir", outputRoot
            };

            var firstExitCode = await Program.Main(args);
            var firstDirectory = Assert.Single(Directory.GetDirectories(outputRoot));
            var firstPack = await File.ReadAllTextAsync(Path.Combine(firstDirectory, "pack.md"));

            var secondExitCode = await Program.Main(args);
            var secondDirectory = Assert.Single(Directory.GetDirectories(outputRoot));

            Assert.Equal(0, firstExitCode);
            Assert.Equal(0, secondExitCode);
            Assert.Equal(firstDirectory, secondDirectory);
            Assert.True(File.Exists(Path.Combine(firstDirectory, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(firstDirectory, "scan.json")));
            Assert.True(File.Exists(Path.Combine(firstDirectory, "resolved-entry.json")));
            Assert.True(File.Exists(Path.Combine(firstDirectory, "pack.md")));

            var manifest = await File.ReadAllTextAsync(Path.Combine(firstDirectory, "manifest.json"));
            var writtenPack = await File.ReadAllTextAsync(packOutputPath);
            using var manifestDocument = JsonDocument.Parse(manifest);
            var selectionSummary = manifestDocument.RootElement.GetProperty("selectionSummary");
            var decisionTraces = manifestDocument.RootElement.GetProperty("decisionTraces");
            var contracts = selectionSummary.GetProperty("contracts");
            var selectedFiles = selectionSummary.GetProperty("selectedFiles");
            var selectedSnippets = selectionSummary.GetProperty("selectedSnippets");

            Assert.Equal(writtenPack, firstPack);
            Assert.Contains("\"entry\": \"symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel\"", manifest, StringComparison.Ordinal);
            Assert.Contains("\"goal\": \"project\"", manifest, StringComparison.Ordinal);
            Assert.Contains(contracts.EnumerateArray(), element =>
                element.GetProperty("title").GetString() == "OpenSettingsCommand");
            Assert.Contains(selectedFiles.EnumerateArray(), element =>
                element.GetProperty("reason").GetString() == "conventional-view"
                || element.GetProperty("reason").GetString() == "code-behind");
            Assert.Contains(selectedSnippets.EnumerateArray(), element =>
                element.GetProperty("reason").GetString() == "dependency-injection"
                || element.GetProperty("reason").GetString() == "navigation-xaml-binding");
            Assert.Contains(decisionTraces.EnumerateArray(), element =>
                element.GetProperty("category").GetString() == "command-selection"
                && element.GetProperty("goalTerms").EnumerateArray().Any(term => term.GetString() == "project"));
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
    public async Task WriteAsync_WhenCanonicalBundleAlreadyExistsWithDifferentContent_CreatesRevisionDirectory()
    {
        var outputRoot = Path.Combine(Path.GetTempPath(), $"rulixa-evidence-revision-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputRoot);

        try
        {
            var writer = new EvidenceBundleWriter(CreateJsonOptions());
            var budget = new Budget(MaxFiles: 8, MaxTotalLines: 1600, MaxSnippetsPerFile: 3);
            var entry = new Entry(EntryKind.Symbol, "Sample.App.ShellViewModel");
            var resolvedEntry = new ResolvedEntry(
                "symbol:Sample.App.ShellViewModel",
                ResolvedEntryKind.Symbol,
                "src/Sample.App/ViewModels/ShellViewModel.cs",
                "Sample.App.ShellViewModel",
                ConfidenceLevel.High,
                []);
            var scanResult = new WorkspaceScanResult(
                "rulixa.phase1.scan.v1",
                @"D:\Workspace\Sample",
                new DateTimeOffset(2026, 03, 28, 8, 30, 0, TimeSpan.Zero),
                new ProjectSummary([], [], ["net8.0-windows"], true, [], ["Sample.App.ShellViewModel"]),
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                []);
            var contextPack = new ContextPack(
                Goal: "project",
                Entry: entry,
                ResolvedEntry: resolvedEntry,
                Contracts: [],
                Indexes: [],
                SelectedSnippets: [],
                SelectedFiles: [],
                DecisionTraces: [],
                Unknowns: [new Diagnostic("diag.sample", "sample", null, DiagnosticSeverity.Info, [])]);

            var firstDirectory = await writer.WriteAsync(
                outputRoot,
                scanResult.WorkspaceRoot,
                budget,
                scanResult,
                resolvedEntry,
                contextPack,
                "# pack 1");

            var secondDirectory = await writer.WriteAsync(
                outputRoot,
                scanResult.WorkspaceRoot,
                budget,
                scanResult,
                resolvedEntry,
                contextPack,
                "# pack 2");

            var directories = Directory.GetDirectories(outputRoot).OrderBy(static path => path, StringComparer.Ordinal).ToArray();

            Assert.Equal(2, directories.Length);
            Assert.Equal(firstDirectory, directories[0]);
            Assert.Equal(secondDirectory, directories[1]);
            Assert.EndsWith("__r2", secondDirectory, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
