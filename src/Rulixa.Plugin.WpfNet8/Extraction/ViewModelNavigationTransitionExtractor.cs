using System.Text.RegularExpressions;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class ViewModelNavigationTransitionExtractor
{
    private static readonly Regex NamespaceRegex = new(@"namespace\s+(?<namespace>[A-Za-z0-9_\.]+)\s*[;{]", RegexOptions.Compiled);
    private static readonly Regex ClassRegex = new(@"(?:(?:public|internal)\s+)?(?:sealed\s+|partial\s+|abstract\s+)?class\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex SelectedItemPropertyRegex = new(@"public\s+[A-Za-z0-9_<>\.\?,]+\s+(?<name>SelectedItem)\s*\{", RegexOptions.Compiled);
    private static readonly Regex CurrentPagePropertyRegex = new(@"public\s+[A-Za-z0-9_<>\.\?,]+\s+(?<name>CurrentPage)\s*\{", RegexOptions.Compiled);
    private static readonly Regex AssignmentRegex = new(@"\b(?<property>SelectedItem|CurrentPage)\s*=\s*(?<expression>[^;]+);", RegexOptions.Compiled);

    public IReadOnlyList<NavigationTransition> Extract(IReadOnlyDictionary<string, string> fileContents)
    {
        var transitions = new List<NavigationTransition>();

        foreach (var (path, content) in fileContents.Where(static pair => pair.Key.Contains("/ViewModels/", StringComparison.OrdinalIgnoreCase)))
        {
            var namespaceName = NamespaceRegex.Match(content).Groups["namespace"].Value;
            var className = ClassRegex.Match(content).Groups["name"].Value;
            if (string.IsNullOrWhiteSpace(namespaceName) || string.IsNullOrWhiteSpace(className))
            {
                continue;
            }

            var selectedItemProperty = SelectedItemPropertyRegex.Match(content).Groups["name"].Value;
            var currentPageProperty = CurrentPagePropertyRegex.Match(content).Groups["name"].Value;
            if (string.IsNullOrWhiteSpace(selectedItemProperty) || string.IsNullOrWhiteSpace(currentPageProperty))
            {
                continue;
            }

            var lines = content.Split('\n');
            foreach (var transition in ExtractAssignments(lines, path, $"{namespaceName}.{className}", className, selectedItemProperty, currentPageProperty))
            {
                transitions.Add(transition);
            }
        }

        return transitions
            .OrderBy(static transition => transition.ViewModelSymbol, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static transition => transition.SourceFilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static transition => transition.StartLine)
            .ToArray();
    }

    private static IEnumerable<NavigationTransition> ExtractAssignments(
        IReadOnlyList<string> lines,
        string path,
        string viewModelSymbol,
        string className,
        string selectedItemProperty,
        string currentPageProperty)
    {
        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var match = AssignmentRegex.Match(lines[lineIndex]);
            if (!match.Success)
            {
                continue;
            }

            var propertyName = match.Groups["property"].Value;
            var expression = match.Groups["expression"].Value.Trim();
            var methodName = FindEnclosingMethodName(lines, lineIndex, className);
            if (string.IsNullOrWhiteSpace(methodName))
            {
                continue;
            }

            if (propertyName.Equals(currentPageProperty, StringComparison.Ordinal)
                || propertyName.Equals(selectedItemProperty, StringComparison.Ordinal))
            {
                yield return new NavigationTransition(
                    viewModelSymbol,
                    path,
                    methodName,
                    selectedItemProperty,
                    currentPageProperty,
                    $"{propertyName} = {expression}",
                    lineIndex + 1);
            }
        }
    }

    private static string? FindEnclosingMethodName(IReadOnlyList<string> lines, int assignmentLineIndex, string className)
    {
        for (var lineIndex = assignmentLineIndex; lineIndex >= 0; lineIndex--)
        {
            var line = lines[lineIndex];
            var constructorRegex = new Regex(
                $@"^\s*(?:public|private|protected|internal)\s+(?<name>{Regex.Escape(className)})\s*\(",
                RegexOptions.Compiled);
            var constructorMatch = constructorRegex.Match(line);
            if (constructorMatch.Success)
            {
                return constructorMatch.Groups["name"].Value;
            }

            var methodMatch = Regex.Match(
                line,
                @"^\s*(?:public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?(?:[A-Za-z_][A-Za-z0-9_<>\.\?,\[\]]*\s+)+(?<name>[A-Za-z_]\w*)\s*\(",
                RegexOptions.Compiled);
            if (methodMatch.Success)
            {
                return methodMatch.Groups["name"].Value;
            }
        }

        return null;
    }
}
