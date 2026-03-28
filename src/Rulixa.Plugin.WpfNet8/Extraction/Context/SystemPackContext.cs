namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed record SystemPackContext(
    string RootSymbol,
    IReadOnlySet<string> ExplorationRootSymbols,
    IReadOnlySet<string> RelatedSymbols,
    IReadOnlyDictionary<string, string> FamilyBySymbol,
    IReadOnlyList<SystemSubMap> SubMaps)
{
    internal bool IsEnabled => !string.IsNullOrWhiteSpace(RootSymbol);

    internal string? TryGetFamily(string symbol) =>
        FamilyBySymbol.TryGetValue(symbol, out var family) ? family : null;
}

internal sealed record SystemSubMap(
    string Family,
    string RepresentativeSymbol,
    IReadOnlyList<string> RelatedSymbols,
    IReadOnlyList<string> FilePaths);
