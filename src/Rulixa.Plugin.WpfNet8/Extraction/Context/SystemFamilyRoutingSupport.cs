namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class SystemFamilyRoutingSupport
{
    private static readonly string[] ResolutionPriority =
    [
        "Drafting",
        "Settings",
        "3D",
        "Report/Export",
        "Shell",
        "Architecture"
    ];

    internal static IReadOnlyList<string> ResolveCandidateFamilies(
        string? firstHopSymbol,
        string targetSymbol)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddSymbolFamilies(candidates, targetSymbol);

        if (!string.IsNullOrWhiteSpace(firstHopSymbol))
        {
            AddRouteFamilies(candidates, firstHopSymbol!, targetSymbol);
        }

        if (candidates.Count == 0)
        {
            candidates.Add("Shell");
        }

        return candidates
            .OrderBy(GetResolutionPriority)
            .ThenBy(static family => family, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static string ResolveFamily(
        string? firstHopSymbol,
        string targetSymbol) =>
        ResolveCandidateFamilies(firstHopSymbol, targetSymbol).First();

    internal static int GetResolutionPriority(string family)
    {
        var index = Array.FindIndex(
            ResolutionPriority,
            candidate => string.Equals(candidate, family, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : int.MaxValue;
    }

    internal static string SelectPreferredFamily(IEnumerable<string> families) =>
        families
            .Where(static family => !string.IsNullOrWhiteSpace(family))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(GetResolutionPriority)
            .ThenBy(static family => family, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? "Shell";

    private static void AddSymbolFamilies(ISet<string> families, string symbolOrName)
    {
        var simpleName = PackExtractionConventions.GetSimpleTypeName(symbolOrName);
        if (string.Equals(symbolOrName, "__architecture__", StringComparison.OrdinalIgnoreCase))
        {
            families.Add("Architecture");
            return;
        }

        if (simpleName.Contains("Architecture", StringComparison.Ordinal)
            || simpleName.Contains("Golden", StringComparison.Ordinal)
            || simpleName.Contains("Regression", StringComparison.Ordinal)
            || simpleName.Contains("Compatibility", StringComparison.Ordinal))
        {
            families.Add("Architecture");
        }

        if (simpleName.Contains("Drafting", StringComparison.Ordinal)
            || simpleName.Contains("Diagram", StringComparison.Ordinal)
            || simpleName.Contains("Floor", StringComparison.Ordinal)
            || PackAnalysisHelpers.IsAlgorithmLikeName(simpleName)
            || PackAnalysisHelpers.IsAnalyzerLikeName(simpleName))
        {
            families.Add("Drafting");
        }

        if (PackAnalysisHelpers.IsSettingsLikeName(simpleName))
        {
            families.Add("Settings");
        }

        if (PackAnalysisHelpers.IsThreeDLikeName(simpleName))
        {
            families.Add("3D");
        }

        if (PackAnalysisHelpers.IsReportLikeName(simpleName))
        {
            families.Add("Report/Export");
        }
    }

    private static void AddRouteFamilies(
        ISet<string> families,
        string firstHopSymbol,
        string targetSymbol)
    {
        foreach (var candidate in ResolveCandidateFamilies(null, firstHopSymbol))
        {
            if (string.Equals(candidate, "Shell", StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate, "Architecture", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsRouteCompatible(candidate, targetSymbol))
            {
                families.Add(candidate);
            }
        }
    }

    private static bool IsRouteCompatible(string family, string targetSymbol)
    {
        var simpleName = PackExtractionConventions.GetSimpleTypeName(targetSymbol);
        return family switch
        {
            "Drafting" => simpleName.Contains("Drafting", StringComparison.Ordinal)
                || simpleName.Contains("Diagram", StringComparison.Ordinal)
                || simpleName.Contains("Floor", StringComparison.Ordinal)
                || PackAnalysisHelpers.IsAlgorithmLikeName(simpleName)
                || PackAnalysisHelpers.IsAnalyzerLikeName(simpleName)
                || PackAnalysisHelpers.IsViewModelLikeName(simpleName)
                || PackAnalysisHelpers.IsWorkflowLikeName(simpleName)
                || PackAnalysisHelpers.IsPersistenceLikeName(simpleName)
                || PackAnalysisHelpers.IsHubObjectLikeName(simpleName)
                || simpleName.EndsWith("Window", StringComparison.Ordinal),
            "Settings" => PackAnalysisHelpers.IsSettingsLikeName(simpleName)
                || PackAnalysisHelpers.IsPersistenceLikeName(simpleName)
                || PackAnalysisHelpers.IsWorkflowLikeName(simpleName)
                || PackAnalysisHelpers.IsHubObjectLikeName(simpleName)
                || PackAnalysisHelpers.IsViewModelLikeName(simpleName)
                || simpleName.EndsWith("Window", StringComparison.Ordinal),
            "3D" => PackAnalysisHelpers.IsThreeDLikeName(simpleName)
                || PackAnalysisHelpers.IsViewModelLikeName(simpleName)
                || PackAnalysisHelpers.IsWorkflowLikeName(simpleName)
                || simpleName.EndsWith("Window", StringComparison.Ordinal),
            "Report/Export" => PackAnalysisHelpers.IsReportLikeName(simpleName)
                || PackAnalysisHelpers.IsWorkflowLikeName(simpleName)
                || PackAnalysisHelpers.IsPersistenceLikeName(simpleName)
                || PackAnalysisHelpers.IsViewModelLikeName(simpleName)
                || simpleName.EndsWith("Window", StringComparison.Ordinal),
            _ => false
        };
    }
}
