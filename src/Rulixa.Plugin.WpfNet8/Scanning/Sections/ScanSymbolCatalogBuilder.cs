using System.Text.RegularExpressions;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Scanning;

internal sealed class ScanSymbolCatalogBuilder
{
    private static readonly Regex NamespaceRegex = new(@"namespace\s+(?<namespace>[A-Za-z0-9_\.]+)\s*[;{]", RegexOptions.Compiled);
    private static readonly Regex XamlClassRegex = new(@"x:Class=""(?<class>[^""]+)""", RegexOptions.Compiled);
    private static readonly Regex TypeRegex = new(
        @"(?:(?:public|internal|private|protected)\s+)?(?:(?:sealed|partial|abstract|static)\s+)*(?<kind>class|interface)\s+(?<name>[A-Za-z_]\w*)",
        RegexOptions.Compiled);
    private static readonly Regex PropertyRegex = new(
        @"^\s*public\s+(?<type>[A-Za-z_][A-Za-z0-9_<>\.\?,\[\]]*)\s+(?<name>[A-Za-z_]\w*)\s*\{\s*get;",
        RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex MethodRegex = new(
        @"^\s*(?:(?:public|private|internal|protected)\s+)+(?:static\s+|virtual\s+|override\s+|sealed\s+|async\s+|partial\s+|new\s+|unsafe\s+|extern\s+)*(?<return>[A-Za-z_][A-Za-z0-9_<>\.\?,\[\]]*|void)\s+(?<name>[A-Za-z_]\w*)\s*\(",
        RegexOptions.Compiled | RegexOptions.Multiline);

    internal IReadOnlyList<ScanSymbol> Build(IReadOnlyDictionary<string, string> fileContents)
    {
        var symbols = new List<ScanSymbol>();
        var nextId = 1;

        foreach (var (path, content) in fileContents.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
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

        return MergePartialTypeSymbols(symbols)
            .OrderBy(static symbol => symbol.QualifiedName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static symbol => symbol.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static symbol => symbol.StartLine)
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
            SymbolResolution.GetSimpleTypeName(xamlClass),
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

        foreach (Match typeMatch in TypeRegex.Matches(content))
        {
            var typeName = typeMatch.Groups["name"].Value;
            if (string.IsNullOrWhiteSpace(typeName))
            {
                continue;
            }

            var kind = typeMatch.Groups["kind"].Value.Equals("interface", StringComparison.Ordinal)
                ? SymbolKind.Interface
                : typeName.EndsWith("Window", StringComparison.Ordinal)
                    ? SymbolKind.Window
                    : SymbolKind.Class;
            var typeQualifiedName = Qualify(namespaceName, typeName);
            var typeStartLine = GetLineNumberAt(content, typeMatch.Index);
            var bodyRange = TryFindBodyRange(content, typeMatch.Index);
            var typeEndLine = bodyRange is null
                ? CountLines(content)
                : GetLineNumberAt(content, bodyRange.Value.EndIndex);

            symbols.Add(new ScanSymbol(
                $"sym-{nextId++:0000}",
                kind,
                typeQualifiedName,
                typeName,
                path,
                typeStartLine,
                typeEndLine,
                []));

            if (bodyRange is null || kind == SymbolKind.Interface)
            {
                continue;
            }

            AddMemberSymbols(path, content, typeName, typeQualifiedName, bodyRange.Value, symbols, ref nextId);
        }
    }

    private static void AddMemberSymbols(
        string path,
        string content,
        string typeName,
        string typeQualifiedName,
        (int BodyStartIndex, int EndIndex) bodyRange,
        ICollection<ScanSymbol> symbols,
        ref int nextId)
    {
        var body = content.Substring(bodyRange.BodyStartIndex, bodyRange.EndIndex - bodyRange.BodyStartIndex + 1);

        foreach (Match propertyMatch in PropertyRegex.Matches(body))
        {
            var propertyName = propertyMatch.Groups["name"].Value;
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                continue;
            }

            var absoluteIndex = bodyRange.BodyStartIndex + propertyMatch.Index;
            var lineNumber = GetLineNumberAt(content, absoluteIndex);
            var kind = propertyName.EndsWith("Command", StringComparison.Ordinal)
                ? SymbolKind.Command
                : SymbolKind.Property;

            symbols.Add(new ScanSymbol(
                $"sym-{nextId++:0000}",
                kind,
                $"{typeQualifiedName}.{propertyName}",
                propertyName,
                path,
                lineNumber,
                lineNumber,
                kind == SymbolKind.Command ? ["command"] : []));
        }

        foreach (Match methodMatch in MethodRegex.Matches(body))
        {
            var methodName = methodMatch.Groups["name"].Value;
            if (string.IsNullOrWhiteSpace(methodName) || string.Equals(methodName, typeName, StringComparison.Ordinal))
            {
                continue;
            }

            var absoluteIndex = bodyRange.BodyStartIndex + methodMatch.Index;
            var methodStartLine = GetLineNumberAt(content, absoluteIndex);
            var methodBodyRange = TryFindBodyRange(content, absoluteIndex);
            var methodEndLine = methodBodyRange is null
                ? methodStartLine
                : GetLineNumberAt(content, methodBodyRange.Value.EndIndex);

            symbols.Add(new ScanSymbol(
                $"sym-{nextId++:0000}",
                SymbolKind.Method,
                $"{typeQualifiedName}.{methodName}",
                methodName,
                path,
                methodStartLine,
                methodEndLine,
                []));
        }
    }

    private static IReadOnlyList<ScanSymbol> MergePartialTypeSymbols(IReadOnlyList<ScanSymbol> symbols)
    {
        var typeKinds = new HashSet<SymbolKind> { SymbolKind.Class, SymbolKind.Interface, SymbolKind.Window };
        var mergedTypes = symbols
            .Where(symbol => typeKinds.Contains(symbol.Kind))
            .GroupBy(static symbol => $"{symbol.Kind}:{symbol.QualifiedName}", StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var ordered = group
                    .OrderBy(static symbol => symbol.FilePath, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static symbol => symbol.StartLine)
                    .ToArray();
                var first = ordered[0];
                return first with
                {
                    StartLine = ordered.Min(static symbol => symbol.StartLine),
                    EndLine = ordered.Max(static symbol => symbol.EndLine),
                    FilePath = ordered[0].FilePath
                };
            });

        var nonTypes = symbols.Where(symbol => !typeKinds.Contains(symbol.Kind));
        return mergedTypes.Concat(nonTypes).ToArray();
    }

    private static (int BodyStartIndex, int EndIndex)? TryFindBodyRange(string source, int declarationIndex)
    {
        var openingBraceIndex = source.IndexOf('{', declarationIndex);
        if (openingBraceIndex < 0)
        {
            return null;
        }

        var depth = 0;
        for (var index = openingBraceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
                continue;
            }

            if (source[index] != '}')
            {
                continue;
            }

            depth--;
            if (depth == 0)
            {
                return (openingBraceIndex + 1, index);
            }
        }

        return null;
    }

    private static int GetLineNumberAt(string source, int index)
    {
        var boundedIndex = Math.Clamp(index, 0, Math.Max(0, source.Length - 1));
        var lineNumber = 1;
        for (var currentIndex = 0; currentIndex < boundedIndex; currentIndex++)
        {
            if (source[currentIndex] == '\n')
            {
                lineNumber++;
            }
        }

        return lineNumber;
    }

    private static int CountLines(string content) => content.Split('\n').Length;

    private static string Qualify(string namespaceName, string name) =>
        string.IsNullOrWhiteSpace(namespaceName) ? name : $"{namespaceName}.{name}";
}
