using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class SystemUnknownAggregationSupport
{
    internal static IReadOnlyList<Diagnostic> Aggregate(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IEnumerable<Diagnostic> unknowns)
    {
        var diagnostics = unknowns
            .Where(static diagnostic => !string.IsNullOrWhiteSpace(diagnostic.Code))
            .ToArray();
        if (relevantContext.SystemPack is null || !relevantContext.SystemPack.IsEnabled)
        {
            return diagnostics
                .Distinct()
                .OrderBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ThenBy(static diagnostic => diagnostic.FilePath, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return diagnostics
            .GroupBy(diagnostic => BuildAggregationKey(relevantContext, diagnostic), StringComparer.OrdinalIgnoreCase)
            .Select(group => MergeGroup(scanResult, relevantContext, group.ToArray()))
            .OrderBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static Diagnostic MergeGroup(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IReadOnlyList<Diagnostic> group)
    {
        var first = group[0];
        var family = ResolveUnknownFamily(relevantContext, first);
        var candidates = RankCandidates(scanResult, relevantContext, family, group.SelectMany(static diagnostic => diagnostic.Candidates));
        return new Diagnostic(
            first.Code,
            first.Message,
            first.FilePath,
            first.Severity,
            candidates);
    }

    private static string BuildAggregationKey(
        RelevantPackContext relevantContext,
        Diagnostic diagnostic)
    {
        var root = relevantContext.SystemPack?.RootSymbol ?? string.Empty;
        var family = ResolveUnknownFamily(relevantContext, diagnostic);
        return $"{root}|{family}|{diagnostic.Code}";
    }

    private static string ResolveUnknownFamily(
        RelevantPackContext relevantContext,
        Diagnostic diagnostic)
    {
        if (string.Equals(diagnostic.Code, "architecture-tests.not-found", StringComparison.OrdinalIgnoreCase))
        {
            return "Architecture";
        }

        var candidateFamilies = diagnostic.Candidates
            .Select(candidate => ResolveCandidateFamily(relevantContext, diagnostic.Code, candidate))
            .Where(static family => !string.IsNullOrWhiteSpace(family))
            .ToArray();
        if (candidateFamilies.Length > 0)
        {
            return SystemFamilyRoutingSupport.SelectPreferredFamily(candidateFamilies);
        }

        return diagnostic.Code switch
        {
            "workflow.missing-downstream" or "workflow.ambiguous-target" => "Drafting",
            "external-asset.unresolved-source" => "Settings",
            _ => "Shell"
        };
    }

    private static string ResolveCandidateFamily(
        RelevantPackContext relevantContext,
        string code,
        string candidate)
    {
        var mappedFamily = PackAnalysisHelpers.GetSystemFamily(relevantContext, candidate);
        if (!string.Equals(mappedFamily, "Shell", StringComparison.OrdinalIgnoreCase))
        {
            return mappedFamily;
        }

        if (string.Equals(code, "workflow.missing-downstream", StringComparison.OrdinalIgnoreCase))
        {
            var simpleName = PackExtractionConventions.GetSimpleTypeName(candidate);
            if (PackAnalysisHelpers.IsAlgorithmLikeName(simpleName) || PackAnalysisHelpers.IsAnalyzerLikeName(simpleName))
            {
                return "Drafting";
            }
        }

        return SystemFamilyRoutingSupport.ResolveFamily(null, candidate);
    }

    private static IReadOnlyList<string> RankCandidates(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        string family,
        IEnumerable<string> candidates)
    {
        return candidates
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
            .GroupBy(static candidate => candidate, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Candidate = group.Key,
                Score = ScoreCandidate(scanResult, relevantContext, family, group.Key)
            })
            .OrderByDescending(static candidate => candidate.Score)
            .ThenBy(static candidate => candidate.Candidate, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(static candidate => candidate.Candidate)
            .ToArray();
    }

    private static int ScoreCandidate(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        string family,
        string candidate)
    {
        var simpleName = PackExtractionConventions.GetSimpleTypeName(candidate);
        var candidateFamily = ResolveCandidateFamily(relevantContext, string.Empty, candidate);
        var score = string.Equals(candidateFamily, family, StringComparison.OrdinalIgnoreCase) ? 50 : 0;

        if (string.Equals(family, "Drafting", StringComparison.OrdinalIgnoreCase))
        {
            score += PackAnalysisHelpers.IsAlgorithmLikeName(simpleName) ? 40 : 0;
            score += PackAnalysisHelpers.IsAnalyzerLikeName(simpleName) ? 35 : 0;
            score += simpleName.EndsWith("Port", StringComparison.Ordinal) || simpleName.EndsWith("Adapter", StringComparison.Ordinal) ? 20 : 0;
        }

        if (string.Equals(family, "Settings", StringComparison.OrdinalIgnoreCase))
        {
            score += PackAnalysisHelpers.IsSettingsLikeName(simpleName) ? 35 : 0;
            score += simpleName.EndsWith("Query", StringComparison.Ordinal) ? 15 : 0;
        }

        if (string.Equals(family, "3D", StringComparison.OrdinalIgnoreCase))
        {
            score += PackAnalysisHelpers.IsThreeDLikeName(simpleName) ? 35 : 0;
        }

        if (string.Equals(family, "Report/Export", StringComparison.OrdinalIgnoreCase))
        {
            score += PackAnalysisHelpers.IsReportLikeName(simpleName) ? 35 : 0;
            score += simpleName.EndsWith("Template", StringComparison.Ordinal) ? 15 : 0;
        }

        score += scanResult.ServiceRegistrations.Any(registration =>
            string.Equals(registration.ServiceType, candidate, StringComparison.OrdinalIgnoreCase)
            || string.Equals(registration.ImplementationType, candidate, StringComparison.OrdinalIgnoreCase))
            ? 10
            : 0;

        return score;
    }
}
