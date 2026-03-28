using Rulixa.Domain.Packs;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class SystemPackSummaryBuilder
{
    internal static Contract? BuildContract(
        RelevantPackContext relevantContext,
        IReadOnlyList<IndexSection> indexes)
    {
        if (relevantContext.SystemPack is null || !relevantContext.SystemPack.IsEnabled)
        {
            return null;
        }

        var families = relevantContext.SystemPack.SubMaps
            .Select(static subMap => subMap.Family)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(PackAnalysisHelpers.GetSystemFamilyPriority)
            .ThenBy(static family => family, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var hubObjects = ExtractRepresentativeNames(indexes, "Hub Objects");
        var persistenceFamilies = ExtractPersistenceFamilies(indexes);
        var assetFamilies = ExtractAssetFamilies(indexes);
        var regressionConstraint = indexes.Any(static index => index.Title == "Architecture Tests")
            ? "回帰拘束は Architecture Tests で確認できます"
            : null;

        var summaryParts = new List<string>
        {
            $"{PackExtractionConventions.GetSimpleTypeName(relevantContext.SystemPack.RootSymbol)} から {string.Join(" / ", families)} の局所地図を束ねます"
        };
        if (hubObjects.Count > 0)
        {
            summaryParts.Add($"中心状態は {string.Join(" / ", hubObjects)} です");
        }

        if (persistenceFamilies.Count > 0)
        {
            summaryParts.Add($"永続化境界は {string.Join(" / ", persistenceFamilies)} family に触れます");
        }

        if (assetFamilies.Count > 0)
        {
            summaryParts.Add($"外部資産は {string.Join(" / ", assetFamilies)} family を解決します");
        }

        if (!string.IsNullOrWhiteSpace(regressionConstraint))
        {
            summaryParts.Add(regressionConstraint);
        }

        return new Contract(
            ContractKind.Startup,
            "System Pack",
            string.Join("。", summaryParts) + "。",
            relevantContext.SystemPack.SubMaps
                .SelectMany(static subMap => subMap.FilePaths.Take(1))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            relevantContext.SystemPack.SubMaps
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
            .Take(2)
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
