using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class HighSignalSelectionSupport
{
    internal static SectionSelectionEvaluation Evaluate(
        GoalExpansionProfile goalProfile,
        IEnumerable<SectionTextEvidence> textEvidence,
        SectionSignalEvidence evidence)
    {
        var matchedSources = textEvidence
            .Select(source => new PackDecisionMatchedSource(
                source.Source,
                source.Values
                    .SelectMany(GoalDrivenExpansionPlanner.ExtractIdentifierTerms)
                    .Where(goalProfile.Terms.Contains)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static term => term, StringComparer.Ordinal)
                    .ToArray()))
            .Where(static source => source.Terms.Count > 0)
            .OrderBy(static source => source.Source, StringComparer.Ordinal)
            .ToArray();
        var matchedTerms = matchedSources
            .SelectMany(static source => source.Terms)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static term => term, StringComparer.Ordinal)
            .ToArray();

        var score = 0;
        score += Math.Min(3, matchedTerms.Length);
        score += evidence.ConstructorDependencyMatches > 0 ? 3 : 0;
        score += evidence.DirectMethodCallMatches > 0 ? 3 : 0;
        score += evidence.ServiceRegistrationMatches > 0 ? 2 : 0;
        score += evidence.PartialSymbolMatches > 0 ? 1 : 0;
        score += evidence.FileKindMatches > 0 ? 1 : 0;
        score += evidence.GoalCategoryMatches > 0 ? 2 : 0;
        score += Math.Min(3, evidence.SemanticSignalCount);
        score += Math.Min(2, evidence.DownstreamCount);
        score -= evidence.HasAmbiguousCandidates ? 4 : 0;
        score -= evidence.HasOnlyFilePathEvidence ? 3 : 0;
        score -= evidence.DownstreamCount == 0 ? 2 : 0;
        score -= evidence.ExceedsHopBudget ? 4 : 0;

        var confidence = evidence.HasAmbiguousCandidates
            ? SectionConfidence.Low
            : score >= 8 && evidence.PositiveEvidenceCount >= 4
                ? SectionConfidence.High
                : score >= 4 && evidence.PositiveEvidenceCount >= 2
                    ? SectionConfidence.Medium
                    : SectionConfidence.Low;

        return new SectionSelectionEvaluation(
            Score: score,
            Confidence: confidence,
            GoalTerms: goalProfile.Terms,
            MatchedTerms: matchedTerms,
            MatchedSources: matchedSources,
            Evidence: evidence);
    }

    internal static PackDecisionTrace BuildDecisionTrace(
        string category,
        string itemKey,
        string decisionKind,
        string summary,
        SectionSelectionEvaluation evaluation,
        int rank,
        int candidateCount) =>
        new(
            Category: category,
            ItemKey: itemKey,
            DecisionKind: decisionKind,
            Summary: summary,
            Score: evaluation.Score,
            Rank: rank,
            CandidateCount: candidateCount,
            GoalTerms: evaluation.GoalTerms,
            MatchedTerms: evaluation.MatchedTerms,
            MatchedSources: evaluation.MatchedSources);

    internal static Diagnostic BuildDiagnostic(
        string code,
        string message,
        string? filePath,
        DiagnosticSeverity severity,
        IEnumerable<string> candidates) =>
        new(
            code,
            message,
            filePath,
            severity,
            candidates
                .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToArray());

    internal static Diagnostic BuildGuidedDiagnostic(
        string code,
        string knownRange,
        string stoppingPoint,
        string? filePath,
        DiagnosticSeverity severity,
        IEnumerable<string> candidates) =>
        BuildDiagnostic(
            code,
            $"追跡できた範囲: {knownRange}。停止点: {stoppingPoint}。",
            filePath,
            severity,
            candidates);

    internal static PackDecisionTrace BuildGuidedUnknownTrace(
        string category,
        string itemKey,
        string summary,
        GoalExpansionProfile goalProfile,
        int candidateCount) =>
        BuildDecisionTrace(
            category,
            itemKey,
            "unknown-raised",
            summary,
            new SectionSelectionEvaluation(0, SectionConfidence.Low, goalProfile.Terms, [], [], new SectionSignalEvidence()),
            0,
            candidateCount);

    internal static IReadOnlyList<string> RankGuidanceCandidates(
        WorkspaceScanResult scanResult,
        GoalExpansionProfile goalProfile,
        IEnumerable<GuidanceCandidate> candidates)
    {
        return candidates
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate.Symbol))
            .GroupBy(static candidate => candidate.Symbol, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Symbol = group.Key,
                Score = group.Sum(candidate => ScoreCandidate(scanResult, goalProfile, candidate))
            })
            .OrderByDescending(static candidate => candidate.Score)
            .ThenBy(static candidate => candidate.Symbol, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(static candidate => candidate.Symbol)
            .ToArray();
    }

    private static int ScoreCandidate(
        WorkspaceScanResult scanResult,
        GoalExpansionProfile goalProfile,
        GuidanceCandidate candidate)
    {
        var simpleName = PackExtractionConventions.GetSimpleTypeName(candidate.Symbol);
        var score = candidate.FromDirectMethodCall ? 40 : 0;
        score += candidate.FromConstructorDependency ? 30 : 0;
        score += candidate.FromServiceRegistration
            || scanResult.ServiceRegistrations.Any(registration =>
                string.Equals(registration.ServiceType, candidate.Symbol, StringComparison.OrdinalIgnoreCase)
                || string.Equals(registration.ImplementationType, candidate.Symbol, StringComparison.OrdinalIgnoreCase))
            ? 20
            : 0;
        score += candidate.FromPortLikeOwner
            && (PackAnalysisHelpers.IsAlgorithmLikeName(simpleName) || PackAnalysisHelpers.IsAnalyzerLikeName(simpleName))
            ? 25
            : 0;

        score += PackAnalysisHelpers.IsAlgorithmLikeName(simpleName) ? 50 : 0;
        score += PackAnalysisHelpers.IsAnalyzerLikeName(simpleName) ? 45 : 0;
        score += PackAnalysisHelpers.IsPersistenceLikeName(simpleName) ? 35 : 0;
        score += PackAnalysisHelpers.IsHubObjectLikeName(simpleName) ? 25 : 0;

        if (goalProfile.HasCategory("drafting")
            && (PackAnalysisHelpers.IsAlgorithmLikeName(simpleName) || PackAnalysisHelpers.IsAnalyzerLikeName(simpleName)))
        {
            score += 20;
        }

        if ((goalProfile.HasCategory("project") || goalProfile.HasCategory("system"))
            && (PackAnalysisHelpers.IsPersistenceLikeName(simpleName) || PackAnalysisHelpers.IsHubObjectLikeName(simpleName)))
        {
            score += 15;
        }

        return score;
    }
}

internal sealed record SectionTextEvidence(
    string Source,
    IReadOnlyList<string> Values);

internal sealed record SectionSignalEvidence(
    int ConstructorDependencyMatches = 0,
    int DirectMethodCallMatches = 0,
    int ServiceRegistrationMatches = 0,
    int PartialSymbolMatches = 0,
    int FileKindMatches = 0,
    int GoalCategoryMatches = 0,
    int SemanticSignalCount = 0,
    int DownstreamCount = 0,
    bool HasAmbiguousCandidates = false,
    bool HasOnlyFilePathEvidence = false,
    bool ExceedsHopBudget = false)
{
    internal int PositiveEvidenceCount =>
        CountIfPositive(ConstructorDependencyMatches)
        + CountIfPositive(DirectMethodCallMatches)
        + CountIfPositive(ServiceRegistrationMatches)
        + CountIfPositive(PartialSymbolMatches)
        + CountIfPositive(FileKindMatches)
        + CountIfPositive(GoalCategoryMatches)
        + CountIfPositive(SemanticSignalCount)
        + CountIfPositive(DownstreamCount);

    private static int CountIfPositive(int value) => value > 0 ? 1 : 0;
}

internal sealed record SectionSelectionEvaluation(
    int Score,
    SectionConfidence Confidence,
    IReadOnlyList<string> GoalTerms,
    IReadOnlyList<string> MatchedTerms,
    IReadOnlyList<PackDecisionMatchedSource> MatchedSources,
    SectionSignalEvidence Evidence)
{
    internal bool IsSelectable => Score >= 4 && Confidence != SectionConfidence.Low && !Evidence.HasAmbiguousCandidates;

    internal string ConfidenceLabel => Confidence switch
    {
        SectionConfidence.High => "高信頼チェーン",
        SectionConfidence.Medium => "中信頼候補",
        _ => "低信頼ヒント"
    };

    internal int ToPriority(int basePriority) =>
        Math.Max(-5, basePriority - Math.Min(Score, 10));
}

internal enum SectionConfidence
{
    Low,
    Medium,
    High
}

internal sealed record GuidanceCandidate(
    string Symbol,
    bool FromDirectMethodCall = false,
    bool FromConstructorDependency = false,
    bool FromServiceRegistration = false,
    bool FromPortLikeOwner = false);
