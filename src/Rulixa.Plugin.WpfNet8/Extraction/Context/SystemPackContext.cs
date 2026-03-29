namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed record SystemPackContext(
    string RootSymbol,
    IReadOnlySet<string> ExplorationRootSymbols,
    IReadOnlySet<string> RelatedSymbols,
    IReadOnlyDictionary<string, string> FamilyBySymbol,
    IReadOnlyDictionary<string, IReadOnlyList<string>> FamilyCandidatesBySymbol,
    IReadOnlyList<string> PersistenceFamilies,
    IReadOnlyList<string> AssetFamilies,
    string? RegressionConstraintFamily,
    IReadOnlyList<SystemSubMap> SubMaps)
{
    internal bool IsEnabled => !string.IsNullOrWhiteSpace(RootSymbol);

    internal string? TryGetFamily(string symbol) =>
        FamilyBySymbol.TryGetValue(symbol, out var family) ? family : null;

    internal IReadOnlyList<string> GetFamilyCandidates(string symbol) =>
        FamilyCandidatesBySymbol.TryGetValue(symbol, out var families)
            ? families
            : [];
}

internal sealed record SystemSubMap(
    string Family,
    string RepresentativeSymbol,
    IReadOnlyList<string> RelatedSymbols,
    IReadOnlyList<string> FilePaths);
