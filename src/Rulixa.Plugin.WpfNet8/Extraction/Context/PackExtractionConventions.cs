using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class PackExtractionConventions
{
    private const int LargeCSharpFileThreshold = 250;

    internal static string? BuildConventionalViewName(string symbol)
    {
        var displayName = GetSimpleTypeName(symbol);
        return displayName.EndsWith("ViewModel", StringComparison.Ordinal)
            ? $"{displayName[..^"ViewModel".Length]}View"
            : null;
    }

    internal static string? FindConventionalViewModelSymbol(WorkspaceScanResult scanResult, string resolvedPath)
    {
        if (!resolvedPath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var fileName = Path.GetFileNameWithoutExtension(resolvedPath);
        if (!fileName.EndsWith("View", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var viewModelName = $"{fileName[..^"View".Length]}ViewModel";
        return scanResult.Symbols
            .FirstOrDefault(symbol =>
                symbol.Kind == SymbolKind.Class
                && string.Equals(symbol.DisplayName, viewModelName, StringComparison.OrdinalIgnoreCase))
            ?.QualifiedName;
    }

    internal static void AddCodeBehindIfPresent(
        WorkspaceScanResult scanResult,
        string viewPath,
        ICollection<FileSelectionCandidate> fileCandidates,
        int priority,
        bool required)
    {
        var codeBehindPath = $"{viewPath}.cs";
        if (scanResult.Files.Any(file => string.Equals(file.Path, codeBehindPath, StringComparison.OrdinalIgnoreCase)))
        {
            fileCandidates.Add(new FileSelectionCandidate(codeBehindPath, "code-behind", priority, required));
        }
    }

    internal static bool ShouldCreateSnippet(WorkspaceScanResult scanResult, string path) =>
        path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
        && scanResult.Files.FirstOrDefault(file => string.Equals(file.Path, path, StringComparison.OrdinalIgnoreCase))?.LineCount > LargeCSharpFileThreshold;

    internal static string GetSimpleTypeName(string typeName) => typeName.Split('.').Last().TrimEnd('?');
}
