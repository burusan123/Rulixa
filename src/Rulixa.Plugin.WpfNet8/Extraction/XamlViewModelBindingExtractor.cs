using System.Text.RegularExpressions;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class XamlViewModelBindingExtractor
{
    private static readonly Regex XmlnsRegex = new(@"xmlns:(?<alias>\w+)=""clr-namespace:(?<namespace>[^"";]+)(?:;assembly=[^""]+)?""", RegexOptions.Compiled);
    private static readonly Regex XamlClassRegex = new(@"x:Class=""(?<class>[^""]+)""", RegexOptions.Compiled);
    private static readonly Regex DataTemplateRegex = new(@"DataTemplate[^>]*DataType=""\{x:Type\s+(?<alias>\w+):(?<type>\w+)\}""", RegexOptions.Compiled);
    private static readonly Regex DataContextAssignmentRegex = new(@"DataContext\s*=\s*(?<variable>[A-Za-z_]\w*)\b", RegexOptions.Compiled);
    private static readonly Regex ParameterRegex = new(@"(?<type>[A-Za-z_][A-Za-z0-9_\.<>]*)\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);

    public IReadOnlyList<ViewModelBinding> Extract(
        IReadOnlyDictionary<string, string> fileContents,
        IReadOnlyList<ScanSymbol> symbols)
    {
        var bindings = new List<ViewModelBinding>();
        var classLookup = symbols
            .Where(static symbol => symbol.Kind == SymbolKind.Class)
            .GroupBy(static symbol => symbol.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        foreach (var (path, content) in fileContents.Where(static pair => pair.Key.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)))
        {
            var xamlClass = XamlClassRegex.Match(content).Groups["class"].Value;
            if (string.IsNullOrWhiteSpace(xamlClass))
            {
                continue;
            }

            var aliasMap = XmlnsRegex.Matches(content)
                .Cast<Match>()
                .ToDictionary(
                    static match => match.Groups["alias"].Value,
                    static match => match.Groups["namespace"].Value,
                    StringComparer.OrdinalIgnoreCase);

            foreach (Match match in DataTemplateRegex.Matches(content))
            {
                var alias = match.Groups["alias"].Value;
                var typeName = match.Groups["type"].Value;
                if (!aliasMap.TryGetValue(alias, out var namespaceName))
                {
                    continue;
                }

                bindings.Add(new ViewModelBinding(
                    ViewPath: path,
                    ViewSymbol: xamlClass,
                    ViewModelSymbol: $"{namespaceName}.{typeName}",
                    BindingKind: ViewModelBindingKind.DataTemplate,
                    SourcePath: path,
                    Confidence: ConfidenceLevel.High,
                    Candidates: []));
            }
        }

        foreach (var (path, content) in fileContents.Where(static pair => pair.Key.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)))
        {
            var dataContextMatch = DataContextAssignmentRegex.Match(content);
            if (!dataContextMatch.Success)
            {
                continue;
            }

            var variable = dataContextMatch.Groups["variable"].Value;
            var parameterType = ResolveParameterType(content, variable);
            if (string.IsNullOrWhiteSpace(parameterType))
            {
                continue;
            }

            var xamlPath = path[..^3];
            var viewSymbol = fileContents.TryGetValue(xamlPath, out var xamlContent)
                ? XamlClassRegex.Match(xamlContent).Groups["class"].Value
                : Path.GetFileNameWithoutExtension(xamlPath);

            var candidates = classLookup.TryGetValue(parameterType, out var matchingSymbols)
                ? matchingSymbols
                : [];
            var resolvedSymbol = candidates.Length == 1 ? candidates[0].QualifiedName : parameterType;

            bindings.Add(new ViewModelBinding(
                ViewPath: xamlPath,
                ViewSymbol: viewSymbol,
                ViewModelSymbol: resolvedSymbol,
                BindingKind: IsRootDataContextPath(path) ? ViewModelBindingKind.RootDataContext : ViewModelBindingKind.ViewDataContext,
                SourcePath: path,
                Confidence: candidates.Length == 1 ? ConfidenceLevel.High : ConfidenceLevel.Medium,
                Candidates: candidates.Select(static symbol => symbol.QualifiedName).ToArray()));
        }

        return bindings
            .OrderBy(static binding => binding.ViewPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static binding => binding.ViewModelSymbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ResolveParameterType(string content, string variableName)
    {
        foreach (Match match in ParameterRegex.Matches(content))
        {
            if (string.Equals(match.Groups["name"].Value, variableName, StringComparison.Ordinal))
            {
                return match.Groups["type"].Value.Split('.').Last();
            }
        }

        return string.Empty;
    }

    private static bool IsRootDataContextPath(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName.Equals("MainWindow.xaml.cs", StringComparison.OrdinalIgnoreCase);
    }
}
