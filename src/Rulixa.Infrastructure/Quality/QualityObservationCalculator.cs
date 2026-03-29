namespace Rulixa.Infrastructure.Quality;

internal sealed class QualityObservationCalculator
{
    public QualityObservationSummary Calculate(IReadOnlyList<QualityCaseArtifact> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);

        var casesWithGuidance = cases.Where(static item => item.UnknownGuidance.Count > 0).ToArray();
        var guidanceItems = cases.SelectMany(static item => item.UnknownGuidance).ToArray();
        var guidanceFamilies = guidanceItems
            .Select(static item => item.Family)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static item => item, StringComparer.Ordinal)
            .ToArray();
        var firstCandidates = guidanceItems
            .Select(static item => item.FirstCandidate)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static item => item, StringComparer.Ordinal)
            .Take(5)
            .ToArray();

        var firstUsefulMapTimeMs = cases
            .Where(static item => item.PackSuccess == true && item.FirstUsefulMapTimeMs is not null)
            .Select(static item => item.FirstUsefulMapTimeMs!.Value)
            .DefaultIfEmpty()
            .Min();

        return new QualityObservationSummary(
            FirstUsefulMapTimeMs: firstUsefulMapTimeMs == 0 ? null : firstUsefulMapTimeMs,
            UnknownGuidanceCaseCount: casesWithGuidance.Length,
            UnknownGuidanceItemCount: guidanceItems.Length,
            UnknownGuidanceFamilyCount: guidanceFamilies.Length,
            RepresentativeChainCount: cases.Sum(static item => item.RepresentativeChainCount),
            DegradedReasonCount: cases.Sum(static item => item.DegradedReasonCount),
            Families: guidanceFamilies,
            FirstCandidates: firstCandidates);
    }
}

internal sealed record QualityObservationSummary(
    long? FirstUsefulMapTimeMs,
    int UnknownGuidanceCaseCount,
    int UnknownGuidanceItemCount,
    int UnknownGuidanceFamilyCount,
    int RepresentativeChainCount,
    int DegradedReasonCount,
    IReadOnlyList<string> Families,
    IReadOnlyList<string> FirstCandidates);
