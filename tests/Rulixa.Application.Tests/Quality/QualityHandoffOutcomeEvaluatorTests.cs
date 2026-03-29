using Rulixa.Infrastructure.Quality;

namespace Rulixa.Application.Tests.Quality;

public sealed class QualityHandoffOutcomeEvaluatorTests
{
    [Fact]
    public void Evaluate_WhenExpectedFamilyIsObserved_ReturnsHit()
    {
        var evaluator = new QualityHandoffOutcomeEvaluator();

        var result = evaluator.Evaluate(
            expectedFamilies: ["Drafting", "Report/Export"],
            observedFamilies: ["Drafting", "Shell"],
            unknownGuidance:
            [
                new UnknownGuidanceArtifact(
                    Code: "workflow.missing-downstream",
                    Family: "Drafting",
                    CandidateCount: 1,
                    FirstCandidate: "Synthetic.DraftingRunner")
            ],
            representativeChainCount: 1,
            falseConfidenceDetected: false,
            status: "passed",
            failureReason: null,
            degradedDiagnosticCount: 0);

        Assert.Equal("hit", result.Outcome);
        Assert.Contains("Drafting", result.ObservedFamilies);
    }

    [Fact]
    public void Evaluate_WhenGuidanceOnlyAndFirstCandidateIsUiNoise_ReturnsMiss()
    {
        var evaluator = new QualityHandoffOutcomeEvaluator();

        var result = evaluator.Evaluate(
            expectedFamilies: [],
            observedFamilies: ["Settings"],
            unknownGuidance:
            [
                new UnknownGuidanceArtifact(
                    Code: "workflow.missing-downstream",
                    Family: "Settings",
                    CandidateCount: 1,
                    FirstCandidate: "Synthetic.SettingsOverlay")
            ],
            representativeChainCount: 0,
            falseConfidenceDetected: false,
            status: "passed",
            failureReason: null,
            degradedDiagnosticCount: 1);

        Assert.Equal("miss", result.Outcome);
        Assert.Equal("ui-noise-first-candidate", result.Reason);
    }
}
