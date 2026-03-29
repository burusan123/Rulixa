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
            var syntheticDefinitions = QualityArtifactSupport.CreateSyntheticCaseDefinitions();
            var optionalSmokeDefinitions = QualityArtifactSupport.CreateOptionalSmokeCaseDefinitions();
            var syntheticCases = await QualityArtifactSupport.ExecuteDefinitionsAsync(syntheticDefinitions);
            var optionalSmokeCases = await QualityArtifactSupport.ExecuteDefinitionsAsync(optionalSmokeDefinitions);
            var runDirectory = Path.Combine(outputRoot, runId);
            var humanOutputDirectory = Path.Combine(runDirectory, "human-outputs");
            var visualOutputDirectory = Path.Combine(runDirectory, "visual-outputs");
            Directory.CreateDirectory(humanOutputDirectory);
            Directory.CreateDirectory(visualOutputDirectory);

            var syntheticHumanOutputs = await QualityArtifactSupport.WriteAutomaticHumanOutputsAsync(
                syntheticDefinitions,
                syntheticCases,
                humanOutputDirectory);
            var observedHumanOutputs = await QualityArtifactSupport.WriteAutomaticHumanOutputsAsync(
                optionalSmokeDefinitions,
                optionalSmokeCases,
                humanOutputDirectory);
            var humanOutputs = syntheticHumanOutputs.Concat(observedHumanOutputs).ToArray();
            var syntheticVisualOutputs = await QualityArtifactSupport.WriteAutomaticVisualOutputsAsync(
                syntheticDefinitions,
                syntheticCases,
                visualOutputDirectory);
            var observedVisualOutputs = await QualityArtifactSupport.WriteAutomaticVisualOutputsAsync(
                optionalSmokeDefinitions,
                optionalSmokeCases,
                visualOutputDirectory);
            var visualOutputs = syntheticVisualOutputs.Concat(observedVisualOutputs).ToArray();

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
                visualOutputs: visualOutputs,
                releaseReviewPath: releaseReviewPath);

            var runArtifact = System.Text.Json.JsonSerializer.Deserialize<LocalQualityRunArtifact>(
                await File.ReadAllTextAsync(result.KpiPath),
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            Assert.NotNull(runArtifact);
            await new ReleaseReviewArtifactWriter().WriteAsync(runDirectory, runArtifact!);
            PublishReleaseReviewArtifacts(result.LatestDirectory, humanOutputs, visualOutputs, releaseReviewPath);

            Assert.True(File.Exists(result.KpiPath));
            Assert.True(File.Exists(result.GatePath));
            Assert.True(File.Exists(result.SummaryPath));
            Assert.True(File.Exists(releaseReviewPath));
            Assert.True(File.Exists(Path.Combine(result.LatestDirectory, "release-review.md")));
            Assert.True(File.Exists(Path.Combine(result.LatestDirectory, "human-outputs", "review-brief-modern-sibling-root.md")));
            Assert.True(File.Exists(Path.Combine(result.LatestDirectory, "human-outputs", "audit-snapshot-service-locator-root.md")));
            Assert.True(File.Exists(Path.Combine(result.LatestDirectory, "human-outputs", "design-knowledge-snapshot-dialog-heavy-root.md")));
            Assert.True(File.Exists(Path.Combine(result.LatestDirectory, "visual-outputs", "modern-sibling-root", "index.html")));
            Assert.True(File.Exists(Path.Combine(result.LatestDirectory, "visual-outputs", "service-locator-root", "index.html")));
            var expectedHumanOutputCount =
                syntheticDefinitions.Count(static item => QualityArtifactSupport.IsRootCase(item))
                + optionalSmokeCases.Count(static item =>
                    string.Equals(item.Status, "passed", StringComparison.OrdinalIgnoreCase)
                    && item.Tags.Contains("root-case", StringComparer.OrdinalIgnoreCase));
            Assert.Equal(expectedHumanOutputCount, humanOutputs.Length);
            Assert.Equal(expectedHumanOutputCount, visualOutputs.Length);

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
            Assert.Contains("review-brief-modern-sibling-root.md", summary, StringComparison.Ordinal);
            Assert.Contains(@"visual-outputs\modern-sibling-root\index.html", summary, StringComparison.Ordinal);
            Assert.Contains("audit-snapshot-service-locator-root.md", summary, StringComparison.Ordinal);
            Assert.Contains("release-review.md", summary, StringComparison.Ordinal);
            Assert.Contains("# Release Review", releaseReview, StringComparison.Ordinal);
            Assert.Contains("## Human Outputs", releaseReview, StringComparison.Ordinal);
            Assert.Contains("## Visual Outputs", releaseReview, StringComparison.Ordinal);
            Assert.Contains("## Handoff Follow-ups", releaseReview, StringComparison.Ordinal);
            Assert.Contains("review-brief-modern-sibling-root.md", releaseReview, StringComparison.Ordinal);
            Assert.Contains(@"visual-outputs\modern-sibling-root\index.html", releaseReview, StringComparison.Ordinal);
            Assert.Contains("design-knowledge-snapshot-template-heavy-root.md", releaseReview, StringComparison.Ordinal);

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
        IReadOnlyList<VisualOutputArtifactReference> visualOutputs,
        string releaseReviewPath)
    {
        var latestHumanOutputDirectory = Path.Combine(latestDirectory, "human-outputs");
        var latestVisualOutputDirectory = Path.Combine(latestDirectory, "visual-outputs");
        Directory.CreateDirectory(latestHumanOutputDirectory);
        Directory.CreateDirectory(latestVisualOutputDirectory);

        foreach (var humanOutput in humanOutputs)
        {
            var targetPath = Path.Combine(latestHumanOutputDirectory, Path.GetFileName(humanOutput.Path));
            File.Copy(humanOutput.Path, targetPath, overwrite: true);
        }

        foreach (var visualOutput in visualOutputs)
        {
            var sourceDirectory = Path.GetDirectoryName(visualOutput.Path);
            if (string.IsNullOrWhiteSpace(sourceDirectory))
            {
                continue;
            }

            var targetDirectory = Path.Combine(latestVisualOutputDirectory, visualOutput.CaseId);
            CopyDirectory(sourceDirectory, targetDirectory);
        }

        File.Copy(releaseReviewPath, Path.Combine(latestDirectory, "release-review.md"), overwrite: true);
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        if (Directory.Exists(targetDirectory))
        {
            Directory.Delete(targetDirectory, recursive: true);
        }

        Directory.CreateDirectory(targetDirectory);
        foreach (var file in Directory.GetFiles(sourceDirectory))
        {
            var targetFile = Path.Combine(targetDirectory, Path.GetFileName(file));
            File.Copy(file, targetFile, overwrite: true);
        }
    }
}
