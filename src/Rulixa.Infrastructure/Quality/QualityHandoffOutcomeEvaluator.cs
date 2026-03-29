namespace Rulixa.Infrastructure.Quality;

internal sealed class QualityHandoffOutcomeEvaluator
{
    private static readonly string[] UiNoiseTokens = ["Overlay", "Prompt", "Renderer"];

    public HandoffOutcomeEvaluation Evaluate(
        IReadOnlyList<string> expectedFamilies,
        IReadOnlyList<string> observedFamilies,
        IReadOnlyList<UnknownGuidanceArtifact> unknownGuidance,
        int representativeChainCount,
        bool falseConfidenceDetected,
        string status,
        string? failureReason,
        int degradedDiagnosticCount)
    {
        ArgumentNullException.ThrowIfNull(expectedFamilies);
        ArgumentNullException.ThrowIfNull(observedFamilies);
        ArgumentNullException.ThrowIfNull(unknownGuidance);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        var normalizedExpectedFamilies = expectedFamilies
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static item => item, StringComparer.Ordinal)
            .ToArray();
        var normalizedObservedFamilies = observedFamilies
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static item => item, StringComparer.Ordinal)
            .ToArray();
        var firstCandidate = unknownGuidance
            .Select(static item => item.FirstCandidate)
            .FirstOrDefault(static item => !string.IsNullOrWhiteSpace(item));

        if (!string.Equals(status, "passed", StringComparison.OrdinalIgnoreCase))
        {
            return degradedDiagnosticCount > 0 || unknownGuidance.Any(static item => item.CandidateCount > 0)
                ? new HandoffOutcomeEvaluation("unknown", normalizedExpectedFamilies, normalizedObservedFamilies, firstCandidate, "degraded-with-guidance")
                : new HandoffOutcomeEvaluation("miss", normalizedExpectedFamilies, normalizedObservedFamilies, firstCandidate, failureReason ?? "raw-failure-without-guidance");
        }

        if (falseConfidenceDetected)
        {
            return new HandoffOutcomeEvaluation("miss", normalizedExpectedFamilies, normalizedObservedFamilies, firstCandidate, "false-confidence-detected");
        }

        if (normalizedExpectedFamilies.Length > 0)
        {
            var matchedFamilies = normalizedObservedFamilies.Intersect(normalizedExpectedFamilies, StringComparer.Ordinal).ToArray();
            if (matchedFamilies.Length > 0 && (representativeChainCount > 0 || unknownGuidance.Any(static item => item.CandidateCount > 0)))
            {
                return new HandoffOutcomeEvaluation("hit", normalizedExpectedFamilies, normalizedObservedFamilies, firstCandidate, $"matched:{string.Join(",", matchedFamilies)}");
            }

            if (unknownGuidance.Any(static item => item.CandidateCount > 0))
            {
                return IsUiNoise(firstCandidate)
                    ? new HandoffOutcomeEvaluation("miss", normalizedExpectedFamilies, normalizedObservedFamilies, firstCandidate, "ui-noise-first-candidate")
                    : new HandoffOutcomeEvaluation("unknown", normalizedExpectedFamilies, normalizedObservedFamilies, firstCandidate, "guidance-present-but-expected-family-missing");
            }

            return new HandoffOutcomeEvaluation("miss", normalizedExpectedFamilies, normalizedObservedFamilies, firstCandidate, "expected-family-missing");
        }

        if (unknownGuidance.Any(static item => item.CandidateCount > 0))
        {
            return IsUiNoise(firstCandidate)
                ? new HandoffOutcomeEvaluation("miss", normalizedExpectedFamilies, normalizedObservedFamilies, firstCandidate, "ui-noise-first-candidate")
                : new HandoffOutcomeEvaluation("unknown", normalizedExpectedFamilies, normalizedObservedFamilies, firstCandidate, "guidance-only");
        }

        if (representativeChainCount > 0)
        {
            return new HandoffOutcomeEvaluation("hit", normalizedExpectedFamilies, normalizedObservedFamilies, firstCandidate, "representative-only");
        }

        return new HandoffOutcomeEvaluation("miss", normalizedExpectedFamilies, normalizedObservedFamilies, firstCandidate, "no-guidance-or-representative");
    }

    private static bool IsUiNoise(string? firstCandidate) =>
        !string.IsNullOrWhiteSpace(firstCandidate)
        && UiNoiseTokens.Any(token => firstCandidate.Contains(token, StringComparison.OrdinalIgnoreCase));
}

internal sealed record HandoffOutcomeEvaluation(
    string Outcome,
    IReadOnlyList<string> ExpectedFamilies,
    IReadOnlyList<string> ObservedFamilies,
    string? FirstCandidate,
    string Reason);
