using System.Text.Json;
using Rulixa.Infrastructure.Quality;

namespace Rulixa.Application.Tests.Quality;

public sealed class LocalQualityGateRunWriterTests
{
    [Fact]
    public async Task WriteAsync_WritesRunArtifactsAndLatestDirectory()
    {
        var outputRoot = Path.Combine(Path.GetTempPath(), $"rulixa-local-quality-{Guid.NewGuid():N}");

        try
        {
            var suites = CreateSuites(includeFailedSmoke: false);
            var writer = new LocalQualityGateRunWriter();

            var result = await writer.WriteAsync(
                outputRoot,
                "20260329T1200000000000Z",
                suites,
                relatedArtifacts:
                [
                    @"tests\Rulixa.Application.Tests\Cli\CompareEvidenceBundleTests.cs"
                ],
                generatedAtUtc: new DateTimeOffset(2026, 03, 29, 12, 0, 0, TimeSpan.Zero));

            Assert.True(File.Exists(result.KpiPath));
            Assert.True(File.Exists(result.GatePath));
            Assert.True(File.Exists(result.SummaryPath));
            Assert.True(File.Exists(Path.Combine(result.LatestDirectory, "kpi.json")));
            Assert.True(File.Exists(Path.Combine(result.LatestDirectory, "gate.json")));
            Assert.True(File.Exists(Path.Combine(result.LatestDirectory, "summary.md")));
            Assert.True(File.Exists(Path.Combine(outputRoot, "latest.txt")));

            var summary = await File.ReadAllTextAsync(result.SummaryPath);
            Assert.Contains("## Gate", summary, StringComparison.Ordinal);
            Assert.Contains("## Synthetic Corpus", summary, StringComparison.Ordinal);
            Assert.Contains("## Optional Smoke", summary, StringComparison.Ordinal);
            Assert.Contains("## Handoff Observations", summary, StringComparison.Ordinal);
            Assert.Contains("## Unknown Guidance Details", summary, StringComparison.Ordinal);
            Assert.Contains("## Degraded Diagnostics", summary, StringComparison.Ordinal);
            Assert.Contains("smoke-env-disabled", summary, StringComparison.Ordinal);
            Assert.Contains("synthetic corpus is the handoff quality baseline", summary, StringComparison.Ordinal);
            Assert.Contains("TemplateHeavyResources.SettingsWindow", summary, StringComparison.Ordinal);

            var gate = JsonSerializer.Deserialize<QualityGateArtifact>(await File.ReadAllTextAsync(result.GatePath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.NotNull(gate);
            Assert.True(gate!.Passed);

            var runArtifact = JsonSerializer.Deserialize<LocalQualityRunArtifact>(await File.ReadAllTextAsync(result.KpiPath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.NotNull(runArtifact);
            Assert.Equal(QualityArtifactConventions.RunSchemaVersion, runArtifact!.SchemaVersion);
            Assert.Contains(runArtifact.Suites, static suite => suite.IncludedInGate);
            Assert.Contains(runArtifact.Suites, static suite => !suite.IncludedInGate);
            Assert.Equal(1, runArtifact.UnknownGuidanceCaseCount);
            Assert.Equal(1, runArtifact.UnknownGuidanceItemCount);
            Assert.Equal(1, runArtifact.UnknownGuidanceFamilyCount);
            Assert.Equal(2, runArtifact.DegradedReasonCount);
            Assert.Equal(2, runArtifact.RepresentativeChainCount);
            Assert.Equal(25, runArtifact.FirstUsefulMapTimeMs);
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
    public async Task WriteAsync_OptionalSmokeFailure_EmitsWarningWithoutFailingGate()
    {
        var outputRoot = Path.Combine(Path.GetTempPath(), $"rulixa-local-quality-warning-{Guid.NewGuid():N}");

        try
        {
            var suites = CreateSuites(includeFailedSmoke: true);
            var writer = new LocalQualityGateRunWriter();

            var result = await writer.WriteAsync(outputRoot, "20260329T1300000000000Z", suites);
            var summary = await File.ReadAllTextAsync(result.SummaryPath);
            var gate = JsonSerializer.Deserialize<QualityGateArtifact>(await File.ReadAllTextAsync(result.GatePath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(gate);
            Assert.True(gate!.Passed);
            Assert.Contains("warning: optional smoke failed", summary, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    private static IReadOnlyList<LocalQualitySuiteInput> CreateSuites(bool includeFailedSmoke)
    {
        var syntheticCases = new[]
        {
            new QualityCaseArtifact(
                CaseId: "synthetic-root",
                CorpusName: "LegacyDialogHeavy",
                WorkspaceType: "synthetic-legacy",
                Entry: "file:ShellWindow.xaml",
                Goal: "legacy system",
                Status: "passed",
                Tags: ["root-case", "deterministic"],
                PackSuccess: true,
                CrashFree: true,
                PartialPack: false,
                HasUnknownGuidance: false,
                FalseConfidenceDetected: false,
                Deterministic: true,
                DurationMilliseconds: 25,
                FirstUsefulMapTimeMs: 25,
                FailureReason: null,
                SkipReason: null,
                DegradedDiagnosticCount: 0,
                RepresentativeChainCount: 1,
                DegradedReasonCount: 0,
                UnknownGuidance: []),
            new QualityCaseArtifact(
                CaseId: "synthetic-weak-signal",
                CorpusName: "TemplateHeavyResources",
                WorkspaceType: "synthetic-legacy",
                Entry: "symbol:TemplateHeavyResources.ViewModels.ShellViewModel",
                Goal: "legacy system",
                Status: "passed",
                Tags: ["weak-signal"],
                PackSuccess: true,
                CrashFree: true,
                PartialPack: true,
                HasUnknownGuidance: true,
                FalseConfidenceDetected: false,
                Deterministic: true,
                DurationMilliseconds: 32,
                FirstUsefulMapTimeMs: 32,
                FailureReason: null,
                SkipReason: null,
                DegradedDiagnosticCount: 2,
                RepresentativeChainCount: 1,
                DegradedReasonCount: 2,
                UnknownGuidance:
                [
                    new UnknownGuidanceArtifact(
                        Code: "workflow.missing-downstream",
                        Family: "Settings",
                        CandidateCount: 2,
                        FirstCandidate: "TemplateHeavyResources.SettingsWindow")
                ])
        };

        var smokeStatus = includeFailedSmoke ? "failed" : "skipped";
        var smokeFailureReason = includeFailedSmoke ? "workspace failed" : null;
        var smokeSkipReason = includeFailedSmoke ? null : "smoke-env-disabled";
        bool? smokePackSuccess = includeFailedSmoke ? false : null;
        bool? smokeCrashFree = includeFailedSmoke ? false : null;
        bool? smokePartialPack = includeFailedSmoke ? false : null;
        bool? smokeHasUnknownGuidance = includeFailedSmoke ? false : null;
        bool? smokeFalseConfidence = includeFailedSmoke ? false : null;
        bool? smokeDeterministic = includeFailedSmoke ? false : null;

        var optionalSmokeCases = new[]
        {
            new QualityCaseArtifact(
                CaseId: "assessmeister-legacy-root",
                CorpusName: "AssessMeister_20260204",
                WorkspaceType: "legacy-real",
                Entry: "file:AssessMeister/Predict3DWindow.xaml",
                Goal: "legacy system",
                Status: smokeStatus,
                Tags: ["optional-smoke", "root-case"],
                PackSuccess: smokePackSuccess,
                CrashFree: smokeCrashFree,
                PartialPack: smokePartialPack,
                HasUnknownGuidance: smokeHasUnknownGuidance,
                FalseConfidenceDetected: smokeFalseConfidence,
                Deterministic: smokeDeterministic,
                DurationMilliseconds: 0,
                FirstUsefulMapTimeMs: null,
                FailureReason: smokeFailureReason,
                SkipReason: smokeSkipReason,
                DegradedDiagnosticCount: 0,
                RepresentativeChainCount: 0,
                DegradedReasonCount: 0,
                UnknownGuidance: [])
        };

        return
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
        ];
    }
}
