using Rulixa.Infrastructure.Quality;

namespace Rulixa.Application.Tests.Quality;

public sealed class QualityHandoffWarningEvaluatorTests
{
    [Fact]
    public void Evaluate_ForDraftingCaseWithoutExpectedFamily_ReturnsWarning()
    {
        var evaluator = new QualityHandoffWarningEvaluator();
        var cases = new[]
        {
            new QualityCaseArtifact(
                CaseId: "drafting-case",
                CorpusName: "Synthetic",
                CorpusCategory: "service-locator-root",
                WorkspaceType: "synthetic-modern",
                Entry: "symbol:Drafting.Root",
                Goal: "drafting ai analyze",
                Status: "passed",
                Tags: ["root-case"],
                PackSuccess: true,
                CrashFree: true,
                PartialPack: true,
                HasUnknownGuidance: true,
                FalseConfidenceDetected: false,
                Deterministic: true,
                DurationMilliseconds: 12,
                FirstUsefulMapTimeMs: 12,
                FailureReason: null,
                SkipReason: null,
                DegradedDiagnosticCount: 1,
                RepresentativeChainCount: 1,
                DegradedReasonCount: 1,
                UnknownGuidance:
                [
                    new UnknownGuidanceArtifact(
                        Code: "workflow.missing-downstream",
                        Family: "Shell",
                        CandidateCount: 1,
                        FirstCandidate: "Synthetic.LicenseService")
                ])
        };

        var warnings = evaluator.Evaluate(cases);

        Assert.Contains(warnings, static item => item.Category == "drafting-guidance-family-missing");
    }

    [Fact]
    public void Evaluate_ForSettingsCaseWithUiNoiseFirstCandidate_ReturnsWarning()
    {
        var evaluator = new QualityHandoffWarningEvaluator();
        var cases = new[]
        {
            new QualityCaseArtifact(
                CaseId: "settings-case",
                CorpusName: "Synthetic",
                CorpusCategory: "weak-signal-root",
                WorkspaceType: "synthetic-legacy",
                Entry: "file:ShellWindow.xaml",
                Goal: "legacy system",
                Status: "passed",
                Tags: ["weak-signal"],
                PackSuccess: true,
                CrashFree: true,
                PartialPack: true,
                HasUnknownGuidance: true,
                FalseConfidenceDetected: false,
                Deterministic: true,
                DurationMilliseconds: 20,
                FirstUsefulMapTimeMs: 20,
                FailureReason: null,
                SkipReason: null,
                DegradedDiagnosticCount: 2,
                RepresentativeChainCount: 0,
                DegradedReasonCount: 2,
                UnknownGuidance:
                [
                    new UnknownGuidanceArtifact(
                        Code: "persistence.missing-owner",
                        Family: "Settings",
                        CandidateCount: 1,
                        FirstCandidate: "Synthetic.SettingsOverlay")
                ])
        };

        var warnings = evaluator.Evaluate(cases);

        Assert.Contains(warnings, static item => item.Category == "settings-report-ui-noise");
    }
}
