using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class PartialSymbolAggregateResolver
{
    private const string PartialFileTagPrefix = "partial-file:";

    internal static IReadOnlyDictionary<string, PartialSymbolAggregate> Build(
        WorkspaceScanResult scanResult,
        IEnumerable<string> symbols)
    {
        var resolved = new Dictionary<string, PartialSymbolAggregate>(StringComparer.OrdinalIgnoreCase);

        foreach (var symbol in symbols
                     .Where(static value => !string.IsNullOrWhiteSpace(value))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var typeSymbol = scanResult.Symbols.FirstOrDefault(candidate =>
                string.Equals(candidate.QualifiedName, symbol, StringComparison.OrdinalIgnoreCase)
                && candidate.Kind is SymbolKind.Class or SymbolKind.Interface or SymbolKind.Window);
            if (typeSymbol is null)
            {
                continue;
            }

            var filePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            filePaths.Add(typeSymbol.FilePath);

            foreach (var tag in typeSymbol.Tags.Where(static tag => tag.StartsWith(PartialFileTagPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                filePaths.Add(tag[PartialFileTagPrefix.Length..]);
            }

            foreach (var memberSymbol in scanResult.Symbols.Where(candidate =>
                         candidate.QualifiedName.StartsWith($"{symbol}.", StringComparison.OrdinalIgnoreCase)))
            {
                filePaths.Add(memberSymbol.FilePath);
            }

            var orderedFiles = filePaths
                .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            resolved[symbol] = new PartialSymbolAggregate(
                symbol,
                typeSymbol.DisplayName,
                orderedFiles,
                scanResult.Symbols
                    .Where(candidate => candidate.QualifiedName.StartsWith($"{symbol}.", StringComparison.OrdinalIgnoreCase))
                    .Select(static candidate => candidate.QualifiedName)
                    .OrderBy(static qualifiedName => qualifiedName, StringComparer.OrdinalIgnoreCase)
                    .ToArray());
        }

        return resolved;
    }
}

internal sealed record PartialSymbolAggregate(
    string Symbol,
    string DisplayName,
    IReadOnlyList<string> FilePaths,
    IReadOnlyList<string> MemberSymbols);
