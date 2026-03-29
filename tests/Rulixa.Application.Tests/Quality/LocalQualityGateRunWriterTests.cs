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
            HumanOutputArtifactReference[] humanOutputs =
            [
                new HumanOutputArtifactReference(
                    CaseId: "synthetic-root",
                    CorpusCategory: "dialog-heavy-root",
                    Mode: "review",
                    Path: @"artifacts\local-quality\run\human-outputs\review-brief.md")
            ];
            VisualOutputArtifactReference[] visualOutputs =
            [
                new VisualOutputArtifactReference(
                    CaseId: "synthetic-root",
                    CorpusCategory: "dialog-heavy-root",
                    Path: @"artifacts\local-quality\run\visual-outputs\synthetic-root\index.html")
            ];

            var result = await writer.WriteAsync(
                outputRoot,
                "20260329T1200000000000Z",
                suites,
                relatedArtifacts:
                [
                    @"tests\Rulixa.Application.Tests\Cli\CompareEvidenceBundleTests.cs"
                ],
                humanOutputs: humanOutputs,
                visualOutputs: visualOutputs,
                releaseReviewPath: @"artifacts\local-quality\run\release-review.md",
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
            Assert.Contains("## Observed Corpus", summary, StringComparison.Ordinal);
            Assert.Contains("## Handoff Observations", summary, StringComparison.Ordinal);
            Assert.Contains("## Release Review", summary, StringComparison.Ordinal);
            Assert.Contains("## Case Handoff Details", summary, StringComparison.Ordinal);
            Assert.Contains("## Performance Baseline", summary, StringComparison.Ordinal);
            Assert.Contains("## Unknown Guidance Details", summary, StringComparison.Ordinal);
            Assert.Contains("## Degraded Diagnostics", summary, StringComparison.Ordinal);
            Assert.Contains("smoke-env-disabled", summary, StringComparison.Ordinal);
            Assert.Contains("synthetic corpus is the handoff quality baseline", summary, StringComparison.Ordinal);
            Assert.Contains("`dialog-heavy-root` hit=`1` miss=`0` unknown=`0` total=`1`", summary, StringComparison.Ordinal);
            Assert.Contains("`legacy-codebehind-root` / `real-workspace-legacy-root`: `skipped`", summary, StringComparison.Ordinal);
            Assert.Contains("TemplateHeavyResources.SettingsWindow", summary, StringComparison.Ordinal);
            Assert.Contains("handoff_warnings: `0`", summary, StringComparison.Ordinal);
            Assert.Contains("baseline: `none`", summary, StringComparison.Ordinal);
            Assert.Contains("review-brief.md", summary, StringComparison.Ordinal);
            Assert.Contains(@"visual-outputs\synthetic-root\index.html", summary, StringComparison.Ordinal);
            Assert.Contains("release-review.md", summary, StringComparison.Ordinal);

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
            Assert.Empty(runArtifact.HandoffWarnings);
            Assert.Equal(1, runArtifact.HandoffSummary.HitCount);
            Assert.Equal(0, runArtifact.HandoffSummary.MissCount);
            Assert.Equal(1, runArtifact.HandoffSummary.UnknownCount);
            Assert.Single(runArtifact.SyntheticCorpusHandoffs, static item => item.CorpusCategory == "dialog-heavy-root");
            Assert.Single(runArtifact.ObservedCorpusHandoffs, static item => item.CorpusCategory == "legacy-codebehind-root");
            Assert.Single(runArtifact.MissOrUnknownCases);
            Assert.Null(runArtifact.PerformanceBaseline);
            Assert.Single(runArtifact.HumanOutputs);
            Assert.Single(runArtifact.VisualOutputs);
            Assert.Equal(@"artifacts\local-quality\run\release-review.md", runArtifact.ReleaseReviewPath);
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

    [Fact]
    public async Task WriteAsync_WhenPreviousLatestExists_EmitsPerformanceBaselineComparison()
    {
        var outputRoot = Path.Combine(Path.GetTempPath(), $"rulixa-local-quality-baseline-{Guid.NewGuid():N}");

        try
        {
            var writer = new LocalQualityGateRunWriter();
            await writer.WriteAsync(
                outputRoot,
                "20260329T1200000000000Z",
                CreateSuites(includeFailedSmoke: false),
                generatedAtUtc: new DateTimeOffset(2026, 03, 29, 12, 0, 0, TimeSpan.Zero));

            var degradedSuites = CreateSuites(includeFailedSmoke: false, slowerSynthetic: true);
            var result = await writer.WriteAsync(
                outputRoot,
                "20260329T1300000000000Z",
                degradedSuites,
                generatedAtUtc: new DateTimeOffset(2026, 03, 29, 13, 0, 0, TimeSpan.Zero));

            var summary = await File.ReadAllTextAsync(result.SummaryPath);
            var runArtifact = JsonSerializer.Deserialize<LocalQualityRunArtifact>(await File.ReadAllTextAsync(result.KpiPath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(runArtifact);
            Assert.NotNull(runArtifact!.PerformanceBaseline);
            Assert.Contains("regression_warnings: `first_useful_map_time_ms`, `representative_chain_count`, `degraded_reason_count`", summary, StringComparison.Ordinal);
            Assert.Contains("case_comparisons:", summary, StringComparison.Ordinal);
            Assert.True(runArtifact.PerformanceBaseline!.RegressionWarnings.Count > 0);
            Assert.True(runArtifact.PerformanceBaseline.FirstUsefulMapTimeMs!.RegressionWarning);
            Assert.NotEmpty(runArtifact.PerformanceBaseline.CaseComparisons);
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    private static IReadOnlyList<LocalQualitySuiteInput> CreateSuites(bool includeFailedSmoke, bool slowerSynthetic = false)
    {
        var syntheticCases = new[]
        {
            new QualityCaseArtifact(
                CaseId: "synthetic-root",
                CorpusName: "LegacyDialogHeavy",
                CorpusCategory: "dialog-heavy-root",
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
                DurationMilliseconds: slowerSynthetic ? 40 : 25,
                FirstUsefulMapTimeMs: slowerSynthetic ? 40 : 25,
                FailureReason: null,
                SkipReason: null,
                DegradedDiagnosticCount: 0,
                RepresentativeChainCount: slowerSynthetic ? 0 : 1,
                DegradedReasonCount: 0,
                UnknownGuidance: [],
                HandoffOutcome: "hit",
                HandoffExpectedFamilies: ["Settings"],
                HandoffObservedFamilies: ["Settings", "Report/Export"],
                HandoffFirstCandidate: null,
                HandoffReason: "matched:Settings"),
            new QualityCaseArtifact(
                CaseId: "synthetic-weak-signal",
                CorpusName: "TemplateHeavyResources",
                CorpusCategory: "weak-signal-root",
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
                DurationMilliseconds: slowerSynthetic ? 48 : 32,
                FirstUsefulMapTimeMs: slowerSynthetic ? 48 : 32,
                FailureReason: null,
                SkipReason: null,
                DegradedDiagnosticCount: slowerSynthetic ? 3 : 2,
                RepresentativeChainCount: 1,
                DegradedReasonCount: slowerSynthetic ? 3 : 2,
                UnknownGuidance:
                [
                    new UnknownGuidanceArtifact(
                        Code: "workflow.missing-downstream",
                        Family: "Settings",
                        CandidateCount: 2,
                        FirstCandidate: "TemplateHeavyResources.SettingsWindow")
                ],
                HandoffOutcome: "unknown",
                HandoffExpectedFamilies: [],
                HandoffObservedFamilies: ["Settings"],
                HandoffFirstCandidate: "TemplateHeavyResources.SettingsWindow",
                HandoffReason: "guidance-only")
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
                CaseId: "real-workspace-legacy-root",
                CorpusName: "LegacyRealWorkspace",
                CorpusCategory: "legacy-codebehind-root",
                WorkspaceType: "legacy-real",
                Entry: "file:Configured/LegacyEntry.xaml",
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
