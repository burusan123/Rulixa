using System.Text.RegularExpressions;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class XamlViewModelBindingExtractor
{
    private static readonly Regex XmlCommentRegex = new(@"<!--.*?-->", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex XmlnsRegex = new(@"xmlns:(?<alias>\w+)=""clr-namespace:(?<namespace>[^"";]+)(?:;assembly=[^""]+)?""", RegexOptions.Compiled);
    private static readonly Regex XamlClassRegex = new(@"x:Class=""(?<class>[^""]+)""", RegexOptions.Compiled);
    private static readonly Regex DataTemplateRegex = new(@"DataTemplate[^>]*DataType=""\{x:Type\s+(?<alias>\w+):(?<type>\w+)\}""", RegexOptions.Compiled);
    private static readonly Regex DataContextAssignmentRegex = new(@"(?:this\.)?DataContext\s*=\s*(?<variable>[A-Za-z_]\w*)\b", RegexOptions.Compiled);
    private static readonly Regex DataContextThisAssignmentRegex = new(@"(?:this\.)?DataContext\s*=\s*this\b", RegexOptions.Compiled);
    private static readonly Regex DataContextNewAssignmentRegex = new(@"(?:this\.)?DataContext\s*=\s*new\s+(?<type>[A-Za-z_]\w*)\s*\(", RegexOptions.Compiled);
    private static readonly Regex ParameterRegex = new(@"(?<type>[A-Za-z_][A-Za-z0-9_\.<>]*)\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex StartupUriRegex = new(@"StartupUri\s*=\s*""(?<path>[^""]+)""", RegexOptions.Compiled);
    private static readonly Regex WindowCreationRegex = new(@"new\s+(?<window>[A-Za-z_]\w*Window)\s*\(", RegexOptions.Compiled);

    public BindingExtractionResult Extract(
        IReadOnlyDictionary<string, string> fileContents,
        IReadOnlyList<ScanSymbol> symbols)
    {
        var bindings = new List<ViewModelBinding>();
        var diagnostics = new List<Diagnostic>();
        var classLookup = symbols
            .Where(static symbol => symbol.Kind is SymbolKind.Class or SymbolKind.Window)
            .GroupBy(static symbol => symbol.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        var startupViewPaths = FindStartupViewPaths(fileContents, symbols);

        foreach (var (path, content) in fileContents.Where(static pair => pair.Key.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                AddDataTemplateBindings(path, content, diagnostics, bindings);
            }
            catch (Exception ex)
            {
                diagnostics.Add(CreateDegradedDiagnostic(path, "XAML 解析を部分継続しました", ex));
            }
        }

        foreach (var (path, content) in fileContents.Where(static pair => pair.Key.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var binding = BuildCodeBehindBinding(path, content, fileContents, classLookup, startupViewPaths);
                if (binding is not null)
                {
                    bindings.Add(binding);
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add(CreateDegradedDiagnostic(path, "Code-behind 解析を部分継続しました", ex));
            }
        }

        return new BindingExtractionResult(
            bindings
                .OrderBy(static binding => binding.ViewPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static binding => binding.ViewModelSymbol, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            diagnostics
                .OrderBy(static diagnostic => diagnostic.FilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static diagnostic => diagnostic.Code, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static void AddDataTemplateBindings(
        string path,
        string content,
        ICollection<Diagnostic> diagnostics,
        ICollection<ViewModelBinding> bindings)
    {
        var normalizedContent = RemoveXmlComments(content);
        var xamlClass = XamlClassRegex.Match(normalizedContent).Groups["class"].Value;
        if (string.IsNullOrWhiteSpace(xamlClass))
        {
            return;
        }

        var aliasMap = BuildAliasMap(normalizedContent, path, diagnostics);
        foreach (Match match in DataTemplateRegex.Matches(normalizedContent))
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
                SourceSpan: SourceSpanFactory.FromMatch(normalizedContent, match.Index, match.Length),
                Confidence: ConfidenceLevel.High,
                Candidates: []));
        }
    }

    private static ViewModelBinding? BuildCodeBehindBinding(
        string path,
        string content,
        IReadOnlyDictionary<string, string> fileContents,
        IReadOnlyDictionary<string, ScanSymbol[]> classLookup,
        IReadOnlySet<string> startupViewPaths)
    {
        var bindingCandidate = ResolveBindingCandidate(content, path, fileContents, classLookup);
        if (bindingCandidate is null)
        {
            return null;
        }

        var xamlPath = path[..^3];
        var viewSymbol = fileContents.TryGetValue(xamlPath, out var xamlContent)
            ? XamlClassRegex.Match(RemoveXmlComments(xamlContent)).Groups["class"].Value
            : Path.GetFileNameWithoutExtension(xamlPath);
        var bindingKind = IsRootDataContextPath(path, xamlPath, startupViewPaths)
            ? ViewModelBindingKind.RootDataContext
            : ViewModelBindingKind.ViewDataContext;

        return new ViewModelBinding(
            ViewPath: xamlPath,
            ViewSymbol: viewSymbol,
            ViewModelSymbol: bindingCandidate.Value.ViewModelSymbol,
            BindingKind: bindingKind,
            SourcePath: path,
            SourceSpan: bindingCandidate.Value.SourceSpan,
            Confidence: bindingCandidate.Value.Confidence,
            Candidates: bindingCandidate.Value.Candidates);
    }

    private static (string ViewModelSymbol, SourceSpan SourceSpan, ConfidenceLevel Confidence, IReadOnlyList<string> Candidates)? ResolveBindingCandidate(
        string content,
        string path,
        IReadOnlyDictionary<string, string> fileContents,
        IReadOnlyDictionary<string, ScanSymbol[]> classLookup)
    {
        var selfAssignmentMatch = DataContextThisAssignmentRegex.Match(content);
        if (selfAssignmentMatch.Success)
        {
            var xamlPath = path[..^3];
            var xamlClass = fileContents.TryGetValue(xamlPath, out var xamlContent)
                ? XamlClassRegex.Match(RemoveXmlComments(xamlContent)).Groups["class"].Value
                : Path.GetFileNameWithoutExtension(xamlPath);
            if (!string.IsNullOrWhiteSpace(xamlClass))
            {
                return (
                    xamlClass,
                    SourceSpanFactory.FromMatch(content, selfAssignmentMatch.Index, selfAssignmentMatch.Length),
                    ConfidenceLevel.High,
                    []);
            }
        }

        var newAssignmentMatch = DataContextNewAssignmentRegex.Match(content);
        if (newAssignmentMatch.Success)
        {
            return ResolveTypeBinding(
                newAssignmentMatch.Groups["type"].Value,
                content,
                newAssignmentMatch.Index,
                newAssignmentMatch.Length,
                classLookup);
        }

        var dataContextMatch = DataContextAssignmentRegex.Match(content);
        if (!dataContextMatch.Success)
        {
            return null;
        }

        var variable = dataContextMatch.Groups["variable"].Value;
        if (string.Equals(variable, "this", StringComparison.Ordinal))
        {
            return null;
        }

        var parameterType = ResolveParameterType(content, variable);
        if (string.IsNullOrWhiteSpace(parameterType))
        {
            return null;
        }

        return ResolveTypeBinding(parameterType, content, dataContextMatch.Index, dataContextMatch.Length, classLookup);
    }

    private static (string ViewModelSymbol, SourceSpan SourceSpan, ConfidenceLevel Confidence, IReadOnlyList<string> Candidates) ResolveTypeBinding(
        string typeName,
        string content,
        int matchIndex,
        int matchLength,
        IReadOnlyDictionary<string, ScanSymbol[]> classLookup)
    {
        var candidates = classLookup.TryGetValue(typeName, out var matchingSymbols)
            ? matchingSymbols
            : [];
        var resolvedSymbol = candidates.Length == 1 ? candidates[0].QualifiedName : typeName;
        return (
            resolvedSymbol,
            SourceSpanFactory.FromMatch(content, matchIndex, matchLength),
            candidates.Length == 1 ? ConfidenceLevel.High : ConfidenceLevel.Medium,
            candidates.Select(static symbol => symbol.QualifiedName).ToArray());
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

    private static Dictionary<string, string> BuildAliasMap(
        string normalizedContent,
        string path,
        ICollection<Diagnostic> diagnostics)
    {
        var aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in XmlnsRegex.Matches(normalizedContent))
        {
            var alias = match.Groups["alias"].Value;
            var namespaceName = match.Groups["namespace"].Value;
            if (aliasMap.TryGetValue(alias, out var existingNamespace))
            {
                if (!string.Equals(existingNamespace, namespaceName, StringComparison.OrdinalIgnoreCase))
                {
                    diagnostics.Add(new Diagnostic(
                        "xaml.parse-degraded",
                        $"XAML namespace alias が競合したため先頭定義を採用しました: {path} ({alias})",
                        path,
                        DiagnosticSeverity.Warning,
                        [alias, existingNamespace, namespaceName]));
                }

                continue;
            }

            aliasMap[alias] = namespaceName;
        }

        return aliasMap;
    }

    private static HashSet<string> FindStartupViewPaths(
        IReadOnlyDictionary<string, string> fileContents,
        IReadOnlyList<ScanSymbol> symbols)
    {
        var startupViewPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, content) in fileContents)
        {
            var fileName = Path.GetFileName(path);
            if (fileName.Equals("App.xaml", StringComparison.OrdinalIgnoreCase))
            {
                var normalizedContent = RemoveXmlComments(content);
                foreach (Match match in StartupUriRegex.Matches(normalizedContent))
                {
                    startupViewPaths.Add(NormalizeStartupViewPath(path, match.Groups["path"].Value));
                }
            }

            if (!fileName.Equals("App.xaml.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (Match match in WindowCreationRegex.Matches(content))
            {
                foreach (var candidatePath in ResolveWindowXamlPaths(match.Groups["window"].Value, fileContents, symbols))
                {
                    startupViewPaths.Add(candidatePath);
                }
            }
        }

        return startupViewPaths;
    }

    private static IEnumerable<string> ResolveWindowXamlPaths(
        string windowName,
        IReadOnlyDictionary<string, string> fileContents,
        IReadOnlyList<ScanSymbol> symbols)
    {
        foreach (var symbol in symbols.Where(symbol =>
                     string.Equals(symbol.DisplayName, windowName, StringComparison.OrdinalIgnoreCase)
                     && symbol.FilePath.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)))
        {
            yield return symbol.FilePath[..^3];
        }

        foreach (var candidatePath in fileContents.Keys.Where(path =>
                     path.EndsWith($"/{windowName}.xaml", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(Path.GetFileName(path), $"{windowName}.xaml", StringComparison.OrdinalIgnoreCase)))
        {
            yield return candidatePath;
        }
    }

    private static string NormalizeStartupViewPath(string appXamlPath, string startupUri)
    {
        var baseDirectory = Path.GetDirectoryName(appXamlPath)?.Replace('\\', '/') ?? string.Empty;
        var normalizedStartupUri = startupUri.Replace('\\', '/').TrimStart('/');
        return string.IsNullOrWhiteSpace(baseDirectory)
            ? normalizedStartupUri
            : Path.Combine(baseDirectory, normalizedStartupUri).Replace('\\', '/');
    }

    private static bool IsRootDataContextPath(string path, string xamlPath, IReadOnlySet<string> startupViewPaths)
    {
        if (startupViewPaths.Contains(xamlPath))
        {
            return true;
        }

        return Path.GetFileName(path).Equals("MainWindow.xaml.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveXmlComments(string content) =>
        XmlCommentRegex.Replace(content, string.Empty);

    private static Diagnostic CreateDegradedDiagnostic(string path, string messagePrefix, Exception ex) =>
        new(
            "xaml.parse-degraded",
            $"{messagePrefix}: {path} ({ex.GetType().Name})",
            path,
            DiagnosticSeverity.Warning,
            []);

    internal sealed record BindingExtractionResult(
        IReadOnlyList<ViewModelBinding> Bindings,
        IReadOnlyList<Diagnostic> Diagnostics);
}
