using Rulixa.Infrastructure.Quality;

namespace Rulixa.Plugin.WpfNet8.Tests.Scanning;

public sealed class LocalQualityGateRunnerTests
{
    private const string OutputRootEnvironmentVariableName = "RULIXA_LOCAL_QUALITY_OUTPUT_ROOT";
    private const string RunIdEnvironmentVariableName = "RULIXA_LOCAL_QUALITY_RUN_ID";

    [Fact]
    public async Task RunLocalQualityGate_WritesRunArtifactsAndSummary()
    {
        var outputRoot = Environment.GetEnvironmentVariable(OutputRootEnvironmentVariableName);
        var runId = Environment.GetEnvironmentVariable(RunIdEnvironmentVariableName);
        var temporaryOutput = string.IsNullOrWhiteSpace(outputRoot);

        outputRoot ??= QualityArtifactSupport.CreateArtifactOutputRoot();
        runId ??= QualityArtifactConventions.BuildRunId(DateTimeOffset.UtcNow);

        try
        {
            var syntheticCases = await QualityArtifactSupport.ExecuteDefinitionsAsync(
                QualityArtifactSupport.CreateSyntheticCaseDefinitions());
            var optionalSmokeCases = await QualityArtifactSupport.ExecuteDefinitionsAsync(
                QualityArtifactSupport.CreateOptionalSmokeCaseDefinitions());
            var runDirectory = Path.Combine(outputRoot, runId);
            var humanOutputDirectory = Path.Combine(runDirectory, "human-outputs");
            Directory.CreateDirectory(humanOutputDirectory);

            var reviewDefinition = Assert.Single(
                QualityArtifactSupport.CreateSyntheticCaseDefinitions(),
                static item => item.CaseId == "modern-sibling-root");
            var auditDefinition = Assert.Single(
                QualityArtifactSupport.CreateSyntheticCaseDefinitions(),
                static item => item.CaseId == "service-locator-root");
            var knowledgeDefinition = Assert.Single(
                QualityArtifactSupport.CreateSyntheticCaseDefinitions(),
                static item => item.CaseId == "dialog-heavy-root");

            var humanOutputs = new[]
            {
                await QualityArtifactSupport.WriteHumanOutputAsync(
                    reviewDefinition,
                    Rulixa.Application.HumanOutputs.HumanOutputMode.Review,
                    Path.Combine(humanOutputDirectory, "review-brief.md")),
                await QualityArtifactSupport.WriteHumanOutputAsync(
                    auditDefinition,
                    Rulixa.Application.HumanOutputs.HumanOutputMode.Audit,
                    Path.Combine(humanOutputDirectory, "audit-snapshot.md")),
                await QualityArtifactSupport.WriteHumanOutputAsync(
                    knowledgeDefinition,
                    Rulixa.Application.HumanOutputs.HumanOutputMode.Knowledge,
                    Path.Combine(humanOutputDirectory, "design-knowledge-snapshot.md"))
            };

            var releaseReviewPath = Path.Combine(runDirectory, "release-review.md");

            var writer = new LocalQualityGateRunWriter();
            var result = await writer.WriteAsync(
                outputRoot,
                runId,
                [
                    new LocalQualitySuiteInput(
                        Name: "synthetic-corpus",
                        Scope: "gate",
                        IncludedInGate: true,
                        Cases: syntheticCases),
                    new LocalQualitySuiteInput(
                        Name: "optional-smoke",
                        Scope: "optional-smoke",
                        IncludedInGate: false,
                        Cases: optionalSmokeCases)
                ],
                relatedArtifacts:
                [
                    @"tests\Rulixa.Application.Tests\Cli\CompareEvidenceBundleTests.cs"
                ],
                humanOutputs: humanOutputs,
                releaseReviewPath: releaseReviewPath);

            var runArtifact = System.Text.Json.JsonSerializer.Deserialize<LocalQualityRunArtifact>(
                await File.ReadAllTextAsync(result.KpiPath),
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            Assert.NotNull(runArtifact);
            await new ReleaseReviewArtifactWriter().WriteAsync(runDirectory, runArtifact!);
            PublishReleaseReviewArtifacts(result.LatestDirectory, humanOutputs, releaseReviewPath);

            Assert.True(File.Exists(result.KpiPath));
            Assert.True(File.Exists(result.GatePath));
            Assert.True(File.Exists(result.SummaryPath));
            Assert.True(File.Exists(releaseReviewPath));
            Assert.True(File.Exists(Path.Combine(result.LatestDirectory, "release-review.md")));
            Assert.True(File.Exists(Path.Combine(result.LatestDirectory, "human-outputs", "review-brief.md")));

            var summary = await File.ReadAllTextAsync(result.SummaryPath);
            var releaseReview = await File.ReadAllTextAsync(releaseReviewPath);
            Assert.Contains("## Gate", summary, StringComparison.Ordinal);
            Assert.Contains("## Synthetic Corpus", summary, StringComparison.Ordinal);
            Assert.Contains("## Observed Corpus", summary, StringComparison.Ordinal);
            Assert.Contains("## Handoff Observations", summary, StringComparison.Ordinal);
            Assert.Contains("## Release Review", summary, StringComparison.Ordinal);
            Assert.Contains("## Case Handoff Details", summary, StringComparison.Ordinal);
            Assert.Contains("## Performance Baseline", summary, StringComparison.Ordinal);
            Assert.Contains("## Unknown Guidance Details", summary, StringComparison.Ordinal);
            Assert.Contains("## Degraded Diagnostics", summary, StringComparison.Ordinal);
            Assert.Contains("tests\\Rulixa.Application.Tests\\Cli\\CompareEvidenceBundleTests.cs", summary, StringComparison.Ordinal);
            Assert.Contains("review-brief.md", summary, StringComparison.Ordinal);
            Assert.Contains("release-review.md", summary, StringComparison.Ordinal);
            Assert.Contains("# Release Review", releaseReview, StringComparison.Ordinal);
            Assert.Contains("## Human Outputs", releaseReview, StringComparison.Ordinal);
            Assert.Contains("review-brief.md", releaseReview, StringComparison.Ordinal);

            var latestSummaryPath = Path.Combine(result.LatestDirectory, "summary.md");
            Assert.True(File.Exists(latestSummaryPath));
        }
        finally
        {
            if (temporaryOutput && Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    private static void PublishReleaseReviewArtifacts(
        string latestDirectory,
        IReadOnlyList<HumanOutputArtifactReference> humanOutputs,
        string releaseReviewPath)
    {
        var latestHumanOutputDirectory = Path.Combine(latestDirectory, "human-outputs");
        Directory.CreateDirectory(latestHumanOutputDirectory);

        foreach (var humanOutput in humanOutputs)
        {
            var targetPath = Path.Combine(latestHumanOutputDirectory, Path.GetFileName(humanOutput.Path));
            File.Copy(humanOutput.Path, targetPath, overwrite: true);
        }

        File.Copy(releaseReviewPath, Path.Combine(latestDirectory, "release-review.md"), overwrite: true);
    }
}
