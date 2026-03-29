using Rulixa.Domain.Packs;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class SystemPackSummaryBuilder
{
    private const int MaxSummaryFamilies = 5;

    internal static Contract? BuildContract(
        RelevantPackContext relevantContext,
        IReadOnlyList<IndexSection> indexes)
    {
        if (relevantContext.SystemPack is null || !relevantContext.SystemPack.IsEnabled)
        {
            return null;
        }

        var systemPack = relevantContext.SystemPack;
        var families = systemPack.SubMaps
            .Select(static subMap => subMap.Family)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(PackAnalysisHelpers.GetSystemFamilyPriority)
            .ThenBy(static family => family, StringComparer.OrdinalIgnoreCase)
            .Take(MaxSummaryFamilies)
            .ToArray();
        var centerState = ExtractRepresentativeNames(indexes, "Hub Objects").FirstOrDefault();
        var persistenceFamilies = SelectFamilies(
            systemPack.PersistenceFamilies,
            ExtractPersistenceFamilies(indexes),
            maxCount: 3);
        var assetFamilies = SelectFamilies(
            systemPack.AssetFamilies,
            ExtractAssetFamilies(indexes),
            maxCount: 3);
        var regressionConstraint = ResolveRegressionConstraint(systemPack, indexes);

        var summaryParts = new List<string>
        {
            $"{PackExtractionConventions.GetSimpleTypeName(systemPack.RootSymbol)} を起点に {string.Join(" / ", families)} の system map を束ねています。"
        };

        if (!string.IsNullOrWhiteSpace(centerState))
        {
            summaryParts.Add($"中心状態は {centerState} です。");
        }

        if (persistenceFamilies.Count > 0)
        {
            summaryParts.Add($"永続化は {string.Join(" / ", persistenceFamilies)} family に触れます。");
        }

        if (assetFamilies.Count > 0)
        {
            summaryParts.Add($"外部資産は {string.Join(" / ", assetFamilies)} family を解決します。");
        }

        if (!string.IsNullOrWhiteSpace(regressionConstraint))
        {
            summaryParts.Add(regressionConstraint);
        }

        return new Contract(
            ContractKind.Startup,
            "System Pack",
            string.Join(" ", summaryParts),
            systemPack.SubMaps
                .SelectMany(static subMap => subMap.FilePaths.Take(1))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            systemPack.SubMaps
                .Select(static subMap => subMap.RepresentativeSymbol)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    internal static IReadOnlyList<FileSelectionCandidate> BuildRepresentativeFiles(RelevantPackContext relevantContext)
    {
        if (relevantContext.SystemPack is null)
        {
            return [];
        }

        return relevantContext.SystemPack.SubMaps
            .Where(static subMap => !string.Equals(subMap.Family, "Architecture", StringComparison.OrdinalIgnoreCase))
            .SelectMany(static subMap => subMap.FilePaths.Take(1)
                .Select(path => new FileSelectionCandidate(path, "system-pack", 34, false)))
            .DistinctBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> SelectFamilies(
        IReadOnlyList<string> preferredFamilies,
        IReadOnlyList<string> fallbackFamilies,
        int maxCount)
    {
        var selected = preferredFamilies
            .Concat(fallbackFamilies)
            .Where(static family => !string.IsNullOrWhiteSpace(family))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxCount)
            .ToArray();
        return selected;
    }

    private static string? ResolveRegressionConstraint(
        SystemPackContext systemPack,
        IReadOnlyList<IndexSection> indexes)
    {
        if (!string.IsNullOrWhiteSpace(systemPack.RegressionConstraintFamily))
        {
            return $"{systemPack.RegressionConstraintFamily} family の回帰拘束があります。";
        }

        return indexes.Any(static index => index.Title == "Architecture Tests")
            ? "Architecture family の回帰拘束があります。"
            : null;
    }

    private static IReadOnlyList<string> ExtractRepresentativeNames(
        IReadOnlyList<IndexSection> indexes,
        string title)
    {
        return indexes
            .Where(index => index.Title == title)
            .SelectMany(static index => index.Lines)
            .Select(static line => line.Split(':', 2).Last().Trim())
            .Select(static line => line.Split("->", StringSplitOptions.TrimEntries)[0])
            .Select(static line => line.Split('(', StringSplitOptions.TrimEntries)[0].Trim())
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(1)
            .ToArray();
    }

    private static IReadOnlyList<string> ExtractPersistenceFamilies(IReadOnlyList<IndexSection> indexes)
    {
        var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in indexes.Where(index => index.Title == "Persistence").SelectMany(static index => index.Lines))
        {
            AddIfContains(line, families, "Repository", "repository");
            AddIfContains(line, families, "Query", "query");
            AddIfContains(line, families, "Saver", "saver");
            AddIfContains(line, families, "Store", "store");
            AddIfContains(line, families, "Settings", "settings");
        }

        return families.OrderBy(static family => family, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyList<string> ExtractAssetFamilies(IReadOnlyList<IndexSection> indexes)
    {
        var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in indexes.Where(index => index.Title == "External Assets").SelectMany(static index => index.Lines))
        {
            AddIfContains(line, families, ".xlsx", "excel");
            AddIfContains(line, families, ".json", "json");
            AddIfContains(line, families, ".onnx", "onnx-model");
            AddIfContains(line, families, ".pdf", "pdf");
            AddIfContains(line, families, ".template", "template");
        }

        return families.OrderBy(static family => family, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void AddIfContains(string line, ISet<string> families, string token, string family)
    {
        if (line.Contains(token, StringComparison.OrdinalIgnoreCase))
        {
            families.Add(family);
        }
    }
}
