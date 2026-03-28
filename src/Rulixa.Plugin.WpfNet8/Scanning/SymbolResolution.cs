using System.Text.RegularExpressions;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Scanning;

internal static class SymbolResolution
{
    private static readonly Regex NamespaceRegex = new(@"namespace\s+(?<namespace>[A-Za-z0-9_\.]+)\s*[;{]", RegexOptions.Compiled);
    private static readonly Regex UsingRegex = new(@"^using\s+(?<namespace>[A-Za-z0-9_\.]+)\s*;", RegexOptions.Compiled | RegexOptions.Multiline);

    internal static string? ResolveTypeSymbol(
        string rawTypeName,
        string sourcePath,
        IReadOnlyDictionary<string, string> fileContents,
        IReadOnlyList<ScanSymbol> symbols,
        ICollection<Diagnostic> diagnostics,
        string diagnosticCode,
        params SymbolKind[] allowedKinds)
    {
        if (string.IsNullOrWhiteSpace(rawTypeName))
        {
            return null;
        }

        var trimmedTypeName = rawTypeName.Trim();
        var candidates = FindCandidates(trimmedTypeName, symbols, allowedKinds);
        if (candidates.Length == 1)
        {
            return candidates[0].QualifiedName;
        }

        if (candidates.Length > 1)
        {
            var narrowedCandidates = NarrowByNamespaces(candidates, sourcePath, fileContents);
            if (narrowedCandidates.Length == 1)
            {
                return narrowedCandidates[0].QualifiedName;
            }

            diagnostics.Add(new Diagnostic(
                diagnosticCode,
                $"型名 '{trimmedTypeName}' を一意に解決できませんでした。",
                sourcePath,
                DiagnosticSeverity.Warning,
                narrowedCandidates.Select(static candidate => candidate.QualifiedName).ToArray()));
            return null;
        }

        diagnostics.Add(new Diagnostic(
            diagnosticCode,
            $"型名 '{trimmedTypeName}' に対応するシンボルが見つかりませんでした。",
            sourcePath,
            DiagnosticSeverity.Warning,
            []));
        return null;
    }

    private static ScanSymbol[] FindCandidates(
        string rawTypeName,
        IReadOnlyList<ScanSymbol> symbols,
        IReadOnlyCollection<SymbolKind> allowedKinds)
    {
        var simpleName = GetSimpleTypeName(rawTypeName);
        return symbols
            .Where(symbol => allowedKinds.Contains(symbol.Kind))
            .Where(symbol =>
                string.Equals(symbol.QualifiedName, rawTypeName, StringComparison.Ordinal)
                || string.Equals(symbol.DisplayName, simpleName, StringComparison.Ordinal))
            .OrderBy(static symbol => symbol.QualifiedName, StringComparer.Ordinal)
            .ToArray();
    }

    private static ScanSymbol[] NarrowByNamespaces(
        IReadOnlyList<ScanSymbol> candidates,
        string sourcePath,
        IReadOnlyDictionary<string, string> fileContents)
    {
        if (!fileContents.TryGetValue(sourcePath, out var source))
        {
            return candidates.ToArray();
        }

        var namespaces = new HashSet<string>(StringComparer.Ordinal);
        var currentNamespace = NamespaceRegex.Match(source).Groups["namespace"].Value;
        if (!string.IsNullOrWhiteSpace(currentNamespace))
        {
            namespaces.Add(currentNamespace);
        }

        foreach (Match match in UsingRegex.Matches(source))
        {
            var namespaceName = match.Groups["namespace"].Value;
            if (!string.IsNullOrWhiteSpace(namespaceName))
            {
                namespaces.Add(namespaceName);
            }
        }

        var narrowed = candidates
            .Where(symbol => namespaces.Contains(GetNamespace(symbol.QualifiedName)))
            .ToArray();
        return narrowed.Length > 0 ? narrowed : candidates.ToArray();
    }

    internal static string GetSimpleTypeName(string symbolName)
    {
        if (string.IsNullOrWhiteSpace(symbolName))
        {
            return string.Empty;
        }

        var simpleName = symbolName.Trim();
        var genericTickIndex = simpleName.IndexOf('<');
        if (genericTickIndex >= 0)
        {
            simpleName = simpleName[..genericTickIndex];
        }

        var lastDotIndex = simpleName.LastIndexOf('.');
        return lastDotIndex >= 0 ? simpleName[(lastDotIndex + 1)..] : simpleName;
    }

    private static string GetNamespace(string qualifiedName)
    {
        var lastDotIndex = qualifiedName.LastIndexOf('.');
        return lastDotIndex > 0 ? qualifiedName[..lastDotIndex] : string.Empty;
    }
}
