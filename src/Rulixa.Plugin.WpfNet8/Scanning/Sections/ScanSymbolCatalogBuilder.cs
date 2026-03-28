using System.Text.RegularExpressions;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Scanning;

internal sealed class ScanSymbolCatalogBuilder
{
    private static readonly Regex NamespaceRegex = new(@"namespace\s+(?<namespace>[A-Za-z0-9_\.]+)\s*[;{]", RegexOptions.Compiled);
    private static readonly Regex ClassRegex = new(@"(?:(?:public|internal)\s+)?(?:sealed\s+|partial\s+|abstract\s+)?class\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex InterfaceRegex = new(@"(?:(?:public|internal)\s+)?interface\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex CommandPropertyRegex = new(@"public\s+(?<type>[A-Za-z0-9_<>\.\?,]+)\s+(?<name>[A-Za-z_]\w*Command)\s*\{\s*get;", RegexOptions.Compiled);
    private static readonly Regex XamlClassRegex = new(@"x:Class=""(?<class>[^""]+)""", RegexOptions.Compiled);

    internal IReadOnlyList<ScanSymbol> Build(IReadOnlyDictionary<string, string> fileContents)
    {
        var symbols = new List<ScanSymbol>();
        var nextId = 1;

        foreach (var (path, content) in fileContents)
        {
            if (path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                AddXamlSymbol(path, content, symbols, ref nextId);
                continue;
            }

            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AddCSharpSymbols(path, content, symbols, ref nextId);
        }

        return symbols
            .OrderBy(static symbol => symbol.QualifiedName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static symbol => symbol.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddXamlSymbol(
        string path,
        string content,
        ICollection<ScanSymbol> symbols,
        ref int nextId)
    {
        var xamlClass = XamlClassRegex.Match(content).Groups["class"].Value;
        if (string.IsNullOrWhiteSpace(xamlClass))
        {
            return;
        }

        symbols.Add(new ScanSymbol(
            $"sym-{nextId++:0000}",
            xamlClass.EndsWith("Window", StringComparison.Ordinal) ? SymbolKind.Window : SymbolKind.Class,
            xamlClass,
            xamlClass.Split('.').Last(),
            path,
            1,
            CountLines(content),
            ["xaml"]));
    }

    private static void AddCSharpSymbols(
        string path,
        string content,
        ICollection<ScanSymbol> symbols,
        ref int nextId)
    {
        var namespaceName = NamespaceRegex.Match(content).Groups["namespace"].Value;

        foreach (Match match in InterfaceRegex.Matches(content))
        {
            var name = match.Groups["name"].Value;
            symbols.Add(new ScanSymbol(
                $"sym-{nextId++:0000}",
                SymbolKind.Interface,
                Qualify(namespaceName, name),
                name,
                path,
                1,
                CountLines(content),
                []));
        }

        foreach (Match match in ClassRegex.Matches(content))
        {
            var name = match.Groups["name"].Value;
            symbols.Add(new ScanSymbol(
                $"sym-{nextId++:0000}",
                name.EndsWith("Window", StringComparison.Ordinal) ? SymbolKind.Window : SymbolKind.Class,
                Qualify(namespaceName, name),
                name,
                path,
                1,
                CountLines(content),
                []));
        }

        foreach (Match match in CommandPropertyRegex.Matches(content))
        {
            var name = match.Groups["name"].Value;
            symbols.Add(new ScanSymbol(
                $"sym-{nextId++:0000}",
                SymbolKind.Command,
                Qualify(namespaceName, name),
                name,
                path,
                1,
                CountLines(content),
                ["command"]));
        }
    }

    private static int CountLines(string content) => content.Split('\n').Length;

    private static string Qualify(string namespaceName, string name) =>
        string.IsNullOrWhiteSpace(namespaceName) ? name : $"{namespaceName}.{name}";
}
