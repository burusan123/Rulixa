using System.Text.Json;
using Rulixa.Infrastructure.Quality;

namespace Rulixa.Plugin.WpfNet8.Tests.Scanning;

public sealed class QualityArtifactTests
{
    [Fact]
    public async Task WriteArtifact_ForSyntheticAndOptionalSmoke_WritesJsonAndEvaluatesGate()
    {
        var outputRoot = QualityArtifactSupport.CreateArtifactOutputRoot();
        try
        {
            var cases = new List<QualityCaseArtifact>();
            foreach (var definition in QualityArtifactSupport.CreateSyntheticCaseDefinitions())
            {
                cases.Add(await QualityArtifactSupport.ExecuteCaseAsync(definition));
            }

            foreach (var definition in QualityArtifactSupport.CreateOptionalSmokeCaseDefinitions())
            {
                cases.Add(await QualityArtifactSupport.ExecuteCaseAsync(definition));
            }

            var writer = new QualityArtifactWriter();
            var filePath = await writer.WriteAsync(outputRoot, "plugin-tests", cases, new DateTimeOffset(2026, 03, 29, 3, 0, 0, TimeSpan.Zero));
            var json = await File.ReadAllTextAsync(filePath);
            var artifact = JsonSerializer.Deserialize<QualityArtifact>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(artifact);
            Assert.Equal(QualityArtifactConventions.SchemaVersion, artifact!.SchemaVersion);
            Assert.Contains(artifact.Cases, static item => item.Tags.Contains("root-case", StringComparer.OrdinalIgnoreCase));
            Assert.Contains(artifact.Cases, static item => !item.Tags.Contains("root-case", StringComparer.OrdinalIgnoreCase));
            Assert.Contains(artifact.Cases, static item =>
                item.CorpusName == "TemplateHeavyResources"
                && item.FalseConfidenceDetected == false);
            Assert.Contains(artifact.Cases, static item =>
                item.Tags.Contains("deterministic", StringComparer.OrdinalIgnoreCase)
                && item.Deterministic == true);
            Assert.True(artifact.RepresentativeChainCount > 0);
            Assert.True(artifact.UnknownGuidanceCaseCount > 0);
            Assert.True(artifact.UnknownGuidanceItemCount > 0);
            Assert.True(artifact.UnknownGuidanceFamilyCount > 0);
            Assert.True(artifact.DegradedReasonCount > 0);
            Assert.NotNull(artifact.FirstUsefulMapTimeMs);
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
    public void BuildDefaultOutputDirectory_ReturnsArtifactsLocalQualityPath()
    {
        var repoRoot = @"D:\C#\Rulixa";
        var outputDirectory = QualityArtifactConventions.BuildDefaultOutputDirectory(repoRoot);

        Assert.Equal(Path.Combine(repoRoot, "artifacts", "local-quality"), outputDirectory);
    }

    [Fact]
    public async Task ExecuteCaseAsync_ForUnavailableOptionalSmoke_RecordsSkippedStatus()
    {
        var originalFlag = Environment.GetEnvironmentVariable(RealWorkspaceOptionalSmokeTests.EnableEnvironmentVariableName);
        var definition = new QualityCaseDefinition(
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

        try
        {
            Environment.SetEnvironmentVariable(RealWorkspaceOptionalSmokeTests.EnableEnvironmentVariableName, "1");

            var result = await QualityArtifactSupport.ExecuteCaseAsync(definition);

            Assert.Equal("skipped", result.Status);
            Assert.Equal("workspace-missing", result.SkipReason);
        }
        finally
        {
            Environment.SetEnvironmentVariable(RealWorkspaceOptionalSmokeTests.EnableEnvironmentVariableName, originalFlag);
        }
    }
}
