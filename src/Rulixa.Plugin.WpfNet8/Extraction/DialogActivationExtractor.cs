using System.Text.RegularExpressions;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class DialogActivationExtractor
{
    private static readonly Regex NamespaceRegex = new(@"namespace\s+(?<namespace>[A-Za-z0-9_\.]+)\s*[;{]", RegexOptions.Compiled);
    private static readonly Regex ClassRegex = new(@"class\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex MethodRegex = new(@"(?:(?:public|private|internal)\s+[A-Za-z0-9_<>\.\?\s]+\s+(?<name>[A-Za-z_]\w*)\s*\()", RegexOptions.Compiled);
    private static readonly Regex WindowRegex = new(@"new\s+(?<window>[A-Za-z_]\w*Window)\s*\(", RegexOptions.Compiled);

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

            foreach (Match windowMatch in WindowRegex.Matches(content))
            {
                activations.Add(new WindowActivation(
                    CallerSymbol: $"{namespaceName}.{className}.{FindContainingMethod(content, windowMatch.Index)}",
                    ServiceSymbol: $"{namespaceName}.{className}",
                    WindowSymbol: windowMatch.Groups["window"].Value,
                    WindowViewModelSymbol: FindWindowViewModel(content, windowMatch.Index),
                    ActivationKind: content.Contains("ShowDialog", StringComparison.Ordinal) ? "show-dialog" : "show",
                    OwnerKind: content.Contains("Owner =", StringComparison.Ordinal) ? "main-window" : "none"));
            }
        }

        return activations
            .OrderBy(static activation => activation.CallerSymbol, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static activation => activation.WindowSymbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string FindContainingMethod(string content, int index)
    {
        var candidates = MethodRegex.Matches(content).Cast<Match>().Where(match => match.Index <= index).ToArray();
        return candidates.LastOrDefault()?.Groups["name"].Value ?? "UnknownMethod";
    }

    private static string? FindWindowViewModel(string content, int index)
    {
        var before = content[..Math.Min(content.Length, index + 300)];
        var marker = before.LastIndexOf("ViewModel", StringComparison.Ordinal);
        if (marker < 0)
        {
            return null;
        }

        var start = marker;
        while (start > 0 && char.IsLetterOrDigit(before[start - 1]))
        {
            start--;
        }

        return before[start..(marker + "ViewModel".Length)];
    }
}
