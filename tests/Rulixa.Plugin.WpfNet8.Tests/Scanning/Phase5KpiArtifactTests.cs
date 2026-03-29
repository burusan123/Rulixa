using System.Text.Json;
using Rulixa.Infrastructure.Quality;

namespace Rulixa.Plugin.WpfNet8.Tests.Scanning;

public sealed class Phase5KpiArtifactTests
{
    [Fact]
    public async Task WriteArtifact_ForSyntheticAndOptionalSmoke_WritesJsonAndEvaluatesGate()
    {
        var outputRoot = Phase5KpiArtifactSupport.CreateArtifactOutputRoot();
        try
        {
            var cases = new List<Phase5KpiCaseArtifact>();
            foreach (var definition in Phase5KpiArtifactSupport.CreateSyntheticCaseDefinitions())
            {
                cases.Add(await Phase5KpiArtifactSupport.ExecuteCaseAsync(definition));
            }

            foreach (var definition in Phase5KpiArtifactSupport.CreateOptionalSmokeCaseDefinitions())
            {
                cases.Add(await Phase5KpiArtifactSupport.ExecuteCaseAsync(definition));
            }

            var writer = new Phase5KpiArtifactWriter();
            var filePath = await writer.WriteAsync(outputRoot, "plugin-tests", cases, new DateTimeOffset(2026, 03, 29, 3, 0, 0, TimeSpan.Zero));
            var json = await File.ReadAllTextAsync(filePath);
            var artifact = JsonSerializer.Deserialize<Phase5KpiArtifact>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(artifact);
            Assert.Equal(Phase5KpiArtifactConventions.SchemaVersion, artifact!.SchemaVersion);
            Assert.Contains(artifact.Cases, static item => item.Tags.Contains("root-case", StringComparer.OrdinalIgnoreCase));
            Assert.Contains(artifact.Cases, static item => !item.Tags.Contains("root-case", StringComparer.OrdinalIgnoreCase));
            Assert.Contains(artifact.Cases, static item =>
                item.CorpusName == "TemplateHeavyResources"
                && item.FalseConfidenceDetected == false);
            Assert.Contains(artifact.Cases, static item =>
                item.Tags.Contains("deterministic", StringComparer.OrdinalIgnoreCase)
                && item.Deterministic == true);
            Assert.True(File.Exists(filePath));
            Assert.NotNull(artifact.QualityGate);
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
    public void BuildDefaultOutputDirectory_ReturnsArtifactsPhase5Path()
    {
        var repoRoot = @"D:\C#\Rulixa";
        var outputDirectory = Phase5KpiArtifactConventions.BuildDefaultOutputDirectory(repoRoot);

        Assert.Equal(Path.Combine(repoRoot, "artifacts", "phase5"), outputDirectory);
    }

    [Fact]
    public async Task ExecuteCaseAsync_ForUnavailableOptionalSmoke_RecordsSkippedStatus()
    {
        var definition = new Phase5KpiCaseDefinition(
            CaseId: "missing-optional-smoke",
            CorpusName: "MissingWorkspace",
            WorkspaceType: "legacy-real",
            WorkspaceRoot: @"D:\does-not-exist",
            Entry: new Rulixa.Domain.Entries.Entry(Rulixa.Domain.Entries.EntryKind.File, "Missing.xaml"),
            Goal: "legacy system",
            Tags: ["optional-smoke", "root-case"],
            RequiredFamilies: [],
            RequireCenterState: false,
            ExpectUnknownGuidance: false,
            DisallowedRepresentativeSections: []);

        var result = await Phase5KpiArtifactSupport.ExecuteCaseAsync(definition);

        Assert.Equal("skipped", result.Status);
        Assert.Equal("workspace-missing", result.SkipReason);
    }
}
