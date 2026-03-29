using Rulixa.Infrastructure.Quality;

namespace Rulixa.Application.Tests.Quality;

public sealed class ReleaseReviewArtifactWriterTests
{
    [Fact]
    public async Task WriteAsync_WritesReleaseReviewWithHumanOutputsAndObservedCorpus()
    {
        var outputRoot = Path.Combine(Path.GetTempPath(), $"rulixa-release-review-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputRoot);

        try
        {
            var runArtifact = new LocalQualityRunArtifact(
                SchemaVersion: QualityArtifactConventions.RunSchemaVersion,
                RunId: "20260329T1200000000000Z",
                GeneratedAtUtc: "2026-03-29T12:00:00.0000000Z",
                Suites: [],
                Cases:
                [
                    new QualityCaseArtifact(
                        CaseId: "synthetic-root",
                        CorpusName: "ModernSiblingRoot",
                        CorpusCategory: "modern-sibling-root",
                        WorkspaceType: "synthetic-modern",
                        Entry: "symbol:ModernSiblingRoot.ViewModels.ShellViewModel",
                        Goal: "project",
                        Status: "passed",
                        Tags: ["root-case"],
                        PackSuccess: true,
                        CrashFree: true,
                        PartialPack: false,
                        HasUnknownGuidance: false,
                        FalseConfidenceDetected: false,
                        Deterministic: true,
                        DurationMilliseconds: 20,
                        FirstUsefulMapTimeMs: 20,
                        FailureReason: null,
                        SkipReason: null,
                        DegradedDiagnosticCount: 0,
                        RepresentativeChainCount: 3,
                        DegradedReasonCount: 0,
                        UnknownGuidance: [],
                        HandoffOutcome: "hit",
                        HandoffExpectedFamilies: ["Settings"],
                        HandoffObservedFamilies: ["Settings"],
                        HandoffFirstCandidate: null,
                        HandoffReason: "matched"),
                    new QualityCaseArtifact(
                        CaseId: "observed-legacy-root",
                        CorpusName: "ObservedWorkspace",
                        CorpusCategory: "legacy-codebehind-root",
                        WorkspaceType: "legacy-real",
                        Entry: "file:ShellWindow.xaml",
                        Goal: "legacy system",
                        Status: "skipped",
                        Tags: ["optional-smoke"],
                        PackSuccess: null,
                        CrashFree: null,
                        PartialPack: null,
                        HasUnknownGuidance: null,
                        FalseConfidenceDetected: null,
                        Deterministic: null,
                        DurationMilliseconds: 0,
                        FirstUsefulMapTimeMs: null,
                        FailureReason: null,
                        SkipReason: "smoke-env-disabled",
                        DegradedDiagnosticCount: 0,
                        RepresentativeChainCount: 0,
                        DegradedReasonCount: 0,
                        UnknownGuidance: [],
                        HandoffOutcome: "unknown",
                        HandoffExpectedFamilies: [],
                        HandoffObservedFamilies: [],
                        HandoffFirstCandidate: null,
                        HandoffReason: "smoke-env-disabled")
                ],
                QualityGate: new QualityGateArtifact(true, 1.0, 1.0, 0.0, 1.0, 0.0, []),
                SyntheticSummary: new LocalQualityObservationSummary(1, 1, 0, 0),
                OptionalSmokeSummary: new LocalQualityObservationSummary(1, 0, 0, 1),
                SyntheticCorpusHandoffs:
                [
                    new CorpusHandoffSummaryArtifact("modern-sibling-root", "synthetic", 1, 1, 0, 0)
                ],
                ObservedCorpusHandoffs:
                [
                    new CorpusHandoffSummaryArtifact("legacy-codebehind-root", "observed", 1, 0, 0, 1)
                ],
                MissOrUnknownCases:
                [
                    new CaseHandoffSummaryArtifact("observed-legacy-root", "legacy-codebehind-root", "observed", "unknown", null, "smoke-env-disabled")
                ],
                UnknownGuidanceSummary: new LocalUnknownGuidanceSummaryArtifact(0, 0, [], []),
                HandoffSummary: new LocalHandoffSummaryArtifact(1, 0, 1, ["Settings"], ["LegacyService.SettingsWindow"]),
                FirstUsefulMapTimeMs: 20,
                UnknownGuidanceCaseCount: 0,
                UnknownGuidanceItemCount: 0,
                UnknownGuidanceFamilyCount: 0,
                RepresentativeChainCount: 3,
                DegradedReasonCount: 0,
                TotalDegradedDiagnosticCount: 0,
                HandoffWarnings: [],
                PerformanceBaseline: new PerformanceBaselineArtifact(
                    new MetricBaselineArtifact(20, 18, 2, false),
                    new MetricBaselineArtifact(3, 3, 0, false),
                    new MetricBaselineArtifact(0, 0, 0, false),
                    new MetricBaselineArtifact(0, 0, 0, false),
                    [],
                    []),
                HumanOutputs:
                [
                    new HumanOutputArtifactReference("synthetic-root", "modern-sibling-root", "review", @"artifacts\local-quality\run\human-outputs\review-brief.md"),
                    new HumanOutputArtifactReference("synthetic-root", "modern-sibling-root", "audit", @"artifacts\local-quality\run\human-outputs\audit-snapshot.md")
                ],
                ReleaseReviewPath: @"artifacts\local-quality\run\release-review.md",
                RelatedArtifacts:
                [
                    @"artifacts\local-quality\run\human-outputs\review-brief.md"
                ]);

            var writer = new ReleaseReviewArtifactWriter();
            var path = await writer.WriteAsync(outputRoot, runArtifact);
            var markdown = await File.ReadAllTextAsync(path);

            Assert.True(File.Exists(path));
            Assert.Contains("# Release Review", markdown, StringComparison.Ordinal);
            Assert.Contains("## Gate", markdown, StringComparison.Ordinal);
            Assert.Contains("## Handoff", markdown, StringComparison.Ordinal);
            Assert.Contains("## Handoff Follow-ups", markdown, StringComparison.Ordinal);
            Assert.Contains("## Observed Corpus", markdown, StringComparison.Ordinal);
            Assert.Contains("smoke-env-disabled", markdown, StringComparison.Ordinal);
            Assert.Contains("human outputs: `none (observation-only)`", markdown, StringComparison.Ordinal);
            Assert.Contains("## Performance", markdown, StringComparison.Ordinal);
            Assert.Contains("## Human Outputs", markdown, StringComparison.Ordinal);
            Assert.Contains("review-brief.md", markdown, StringComparison.Ordinal);
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
