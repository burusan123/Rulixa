using System.Text.RegularExpressions;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class DialogActivationExtractor
{
    private static readonly Regex NamespaceRegex = new(@"namespace\s+(?<namespace>[A-Za-z0-9_\.]+)\s*[;{]", RegexOptions.Compiled);
    private static readonly Regex ClassRegex = new(@"class\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex MethodRegex = new(@"(?:(?:public|private|internal)\s+[A-Za-z0-9_<>\.\?\s]+\s+(?<name>[A-Za-z_]\w*)\s*\()", RegexOptions.Compiled);
    private static readonly Regex WindowCreationRegex = new(@"(?<variable>[A-Za-z_]\w*)\s*=\s*new\s+(?<window>[A-Za-z_]\w*Window)\s*\(", RegexOptions.Compiled);
    private static readonly Regex WindowShowRegex = new(@"(?<variable>[A-Za-z_]\w*)\.(?<activation>ShowDialog|Show)\s*\(", RegexOptions.Compiled);
    private static readonly Regex DirectWindowShowRegex = new(@"new\s+(?<window>[A-Za-z_]\w*Window)\s*\([^;]*?\)\.(?<activation>ShowDialog|Show)\s*\(", RegexOptions.Compiled);
    private static readonly Regex OwnerAssignmentRegex = new(@"(?<variable>[A-Za-z_]\w*)\.Owner\s*=", RegexOptions.Compiled);
    private static readonly Regex ViewModelCreationRegex = new(@"new\s+(?<viewModel>[A-Za-z_]\w*ViewModel)\s*\(", RegexOptions.Compiled);

    public IReadOnlyList<WindowActivation> Extract(IReadOnlyDictionary<string, string> fileContents)
    {
        var activations = new List<WindowActivation>();

        foreach (var (path, content) in fileContents.Where(static pair => pair.Key.Contains("/Services/", StringComparison.OrdinalIgnoreCase)))
        {
            if (!content.Contains("ShowDialog", StringComparison.Ordinal) && !content.Contains(".Show(", StringComparison.Ordinal))
            {
                continue;
            }

            var namespaceName = NamespaceRegex.Match(content).Groups["namespace"].Value;
            var className = ClassRegex.Match(content).Groups["name"].Value;
            if (string.IsNullOrWhiteSpace(namespaceName) || string.IsNullOrWhiteSpace(className))
            {
                continue;
            }

            foreach (Match methodMatch in MethodRegex.Matches(content))
            {
                activations.AddRange(ExtractMethodActivations(namespaceName, className, content, methodMatch));
            }
        }

        return activations
            .OrderBy(static activation => activation.CallerSymbol, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static activation => activation.WindowSymbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<WindowActivation> ExtractMethodActivations(
        string namespaceName,
        string className,
        string content,
        Match methodMatch)
    {
        var methodName = methodMatch.Groups["name"].Value;
        if (string.IsNullOrWhiteSpace(methodName))
        {
            return [];
        }

        var methodBodyRange = TryFindBodyRange(content, methodMatch.Index);
        if (methodBodyRange is null)
        {
            return [];
        }

        var methodBody = content.Substring(
            methodBodyRange.Value.BodyStartIndex,
            methodBodyRange.Value.EndIndex - methodBodyRange.Value.BodyStartIndex + 1);
        var callerSymbol = $"{namespaceName}.{className}.{methodName}";
        var serviceSymbol = $"{namespaceName}.{className}";
        var activations = new List<WindowActivation>();

        activations.AddRange(ExtractDirectWindowActivations(methodBody, callerSymbol, serviceSymbol));

        var creations = WindowCreationRegex.Matches(methodBody)
            .Cast<Match>()
            .Select(static match => new WindowCreation(
                match.Groups["variable"].Value,
                match.Groups["window"].Value,
                match.Index))
            .ToArray();

        foreach (Match showMatch in WindowShowRegex.Matches(methodBody))
        {
            var variableName = showMatch.Groups["variable"].Value;
            var creation = creations.LastOrDefault(candidate =>
                string.Equals(candidate.VariableName, variableName, StringComparison.Ordinal)
                && candidate.Index <= showMatch.Index);
            if (creation is null)
            {
                continue;
            }

            activations.Add(new WindowActivation(
                CallerSymbol: callerSymbol,
                ServiceSymbol: serviceSymbol,
                WindowSymbol: creation.WindowType,
                WindowViewModelSymbol: FindWindowViewModel(methodBody, creation.Index, showMatch.Index),
                ActivationKind: ToActivationKind(showMatch.Groups["activation"].Value),
                OwnerKind: HasOwnerAssignment(methodBody, variableName, creation.Index, showMatch.Index) ? "main-window" : "none"));
        }

        return activations;
    }

    private static IReadOnlyList<WindowActivation> ExtractDirectWindowActivations(
        string methodBody,
        string callerSymbol,
        string serviceSymbol) =>
        DirectWindowShowRegex.Matches(methodBody)
            .Cast<Match>()
            .Select(match => new WindowActivation(
                CallerSymbol: callerSymbol,
                ServiceSymbol: serviceSymbol,
                WindowSymbol: match.Groups["window"].Value,
                WindowViewModelSymbol: FindWindowViewModel(methodBody, match.Index, match.Index + match.Length),
                ActivationKind: ToActivationKind(match.Groups["activation"].Value),
                OwnerKind: "none"))
            .ToArray();

    private static bool HasOwnerAssignment(string methodBody, string variableName, int startIndex, int endIndex)
    {
        var searchStartIndex = Math.Clamp(startIndex, 0, methodBody.Length);
        var searchEndIndex = Math.Clamp(endIndex, searchStartIndex, methodBody.Length);
        var segment = methodBody[searchStartIndex..searchEndIndex];

        return OwnerAssignmentRegex.Matches(segment)
            .Cast<Match>()
            .Any(match => string.Equals(match.Groups["variable"].Value, variableName, StringComparison.Ordinal));
    }

    private static string? FindWindowViewModel(string methodBody, int startIndex, int endIndex)
    {
        var searchStartIndex = Math.Clamp(startIndex, 0, methodBody.Length);
        var searchEndIndex = Math.Clamp(endIndex + 200, searchStartIndex, methodBody.Length);
        var segment = methodBody[searchStartIndex..searchEndIndex];
        var matches = ViewModelCreationRegex.Matches(segment);
        if (matches.Count == 0)
        {
            return null;
        }

        return matches[^1].Groups["viewModel"].Value;
    }

    private static string ToActivationKind(string rawActivation) =>
        string.Equals(rawActivation, "ShowDialog", StringComparison.Ordinal)
            ? "show-dialog"
            : "show";

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

    private sealed record WindowCreation(string VariableName, string WindowType, int Index);
}
