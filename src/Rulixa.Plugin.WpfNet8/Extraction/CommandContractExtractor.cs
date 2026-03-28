using System.Text.RegularExpressions;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class CommandContractExtractor
{
    private static readonly Regex NamespaceRegex = new(@"namespace\s+(?<namespace>[A-Za-z0-9_\.]+)\s*[;{]", RegexOptions.Compiled);
    private static readonly Regex ClassRegex = new(@"class\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex PropertyRegex = new(@"public\s+(?<type>[A-Za-z0-9_<>\.\?,]+)\s+(?<name>[A-Za-z_]\w*Command)\s*\{\s*get;", RegexOptions.Compiled);
    private static readonly Regex AssignmentRegex = new(@"(?<property>[A-Za-z_]\w*Command)\s*=\s*new\s+(?<commandType>[A-Za-z0-9_<>]+)\((?<args>[^;]+)\);", RegexOptions.Compiled);
    private static readonly Regex ViewModelRegex = new(@"(?<viewName>[A-Za-z_]\w*)ViewModel$", RegexOptions.Compiled);

    public IReadOnlyList<CommandBinding> Extract(
        IReadOnlyDictionary<string, string> fileContents,
        IReadOnlyList<ScanFile> files)
    {
        var viewLookup = files
            .Where(static file => file.Kind == ScanFileKind.Xaml)
            .ToDictionary(
                static file => Path.GetFileNameWithoutExtension(file.Path),
                static file => file.Path,
                StringComparer.OrdinalIgnoreCase);

        var commands = new List<CommandBinding>();

        foreach (var (path, content) in fileContents.Where(static pair => pair.Key.Contains("/ViewModels/", StringComparison.OrdinalIgnoreCase)))
        {
            var namespaceName = NamespaceRegex.Match(content).Groups["namespace"].Value;
            var className = ClassRegex.Match(content).Groups["name"].Value;
            if (string.IsNullOrWhiteSpace(namespaceName) || string.IsNullOrWhiteSpace(className))
            {
                continue;
            }

            var propertyNames = PropertyRegex.Matches(content)
                .Cast<Match>()
                .Select(static match => match.Groups["name"].Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (Match assignment in AssignmentRegex.Matches(content))
            {
                var propertyName = assignment.Groups["property"].Value;
                if (!propertyNames.Contains(propertyName))
                {
                    continue;
                }

                var args = assignment.Groups["args"].Value.Split(',', StringSplitOptions.TrimEntries);
                var executeTarget = args.Length > 0 ? args[0] : propertyName;
                var canExecuteTarget = args.Length > 1 ? ExtractCanExecute(args[1]) : null;

                commands.Add(new CommandBinding(
                    ViewModelSymbol: $"{namespaceName}.{className}",
                    PropertyName: propertyName,
                    CommandType: assignment.Groups["commandType"].Value,
                    ExecuteSymbol: $"{namespaceName}.{className}.{executeTarget}",
                    CanExecuteSymbol: string.IsNullOrWhiteSpace(canExecuteTarget) ? null : $"{namespaceName}.{className}.{canExecuteTarget}",
                    BoundViews: ResolveBoundViews(className, viewLookup)));
            }
        }

        return commands
            .OrderBy(static command => command.ViewModelSymbol, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static command => command.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> ResolveBoundViews(string viewModelName, IReadOnlyDictionary<string, string> viewLookup)
    {
        var match = ViewModelRegex.Match(viewModelName);
        if (!match.Success)
        {
            return [];
        }

        var viewName = $"{match.Groups["viewName"].Value}View";
        return viewLookup.TryGetValue(viewName, out var path) ? [path] : [];
    }

    private static string? ExtractCanExecute(string raw)
    {
        const string lambdaPrefix = "() =>";
        var trimmed = raw.Trim();
        return trimmed.StartsWith(lambdaPrefix, StringComparison.Ordinal)
            ? trimmed[lambdaPrefix.Length..].Trim()
            : trimmed;
    }
}
