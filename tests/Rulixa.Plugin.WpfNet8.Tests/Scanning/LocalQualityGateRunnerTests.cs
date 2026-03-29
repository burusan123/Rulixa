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
                ]);

            Assert.True(File.Exists(result.KpiPath));
            Assert.True(File.Exists(result.GatePath));
            Assert.True(File.Exists(result.SummaryPath));

            var summary = await File.ReadAllTextAsync(result.SummaryPath);
            Assert.Contains("## Gate", summary, StringComparison.Ordinal);
            Assert.Contains("## Synthetic Corpus", summary, StringComparison.Ordinal);
            Assert.Contains("## Optional Smoke", summary, StringComparison.Ordinal);
            Assert.Contains("## Handoff Observations", summary, StringComparison.Ordinal);
            Assert.Contains("## Unknown Guidance Details", summary, StringComparison.Ordinal);
            Assert.Contains("## Degraded Diagnostics", summary, StringComparison.Ordinal);
            Assert.Contains("tests\\Rulixa.Application.Tests\\Cli\\CompareEvidenceBundleTests.cs", summary, StringComparison.Ordinal);

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
}
