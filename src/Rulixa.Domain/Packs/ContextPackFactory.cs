using Rulixa.Domain.Entries;
using Rulixa.Domain.Scanning;

namespace Rulixa.Domain.Packs;

public static class ContextPackFactory
{
    private const int LargeCSharpFileThreshold = 250;
    private const int SnippetMergeGapLines = 3;

    public static ContextPack Create(
        string goal,
        Entry entry,
        ResolvedEntry resolvedEntry,
        PackIngredients ingredients,
        WorkspaceScanResult scanResult,
        Budget budget)
    {
        ArgumentNullException.ThrowIfNull(goal);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(resolvedEntry);
        ArgumentNullException.ThrowIfNull(ingredients);
        ArgumentNullException.ThrowIfNull(scanResult);
        ArgumentNullException.ThrowIfNull(budget);

        var fileLineCounts = scanResult.Files.ToDictionary(
            static file => file.Path,
            static file => file.LineCount,
            StringComparer.OrdinalIgnoreCase);

        var selectedSnippets = SelectSnippets(ingredients.SnippetCandidates, budget);
        var selectedFiles = SelectFiles(ingredients.FileCandidates, fileLineCounts, selectedSnippets, budget);
        var decisionTraces = OrderDecisionTraces(ingredients.DecisionTraces);

        return new ContextPack(
            Goal: goal,
            Entry: entry,
            ResolvedEntry: resolvedEntry,
            Contracts: ingredients.Contracts,
            Indexes: ingredients.Indexes,
            SelectedSnippets: selectedSnippets,
            SelectedFiles: selectedFiles,
            DecisionTraces: decisionTraces,
            Unknowns: ingredients.Unknowns);
    }

    private static IReadOnlyList<SelectedSnippet> SelectSnippets(
        IReadOnlyList<SnippetSelectionCandidate> candidates,
        Budget budget)
    {
        if (candidates.Count == 0 || budget.MaxSnippetsPerFile == 0)
        {
            return [];
        }

        return candidates
            .GroupBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => SelectSnippetsForFile(group, budget.MaxSnippetsPerFile))
            .OrderByDescending(static snippet => snippet.IsRequired)
            .ThenBy(static snippet => snippet.Priority)
            .ThenBy(static snippet => snippet.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static snippet => snippet.StartLine)
            .ToArray();
    }

    private static IReadOnlyList<SelectedFile> SelectFiles(
        IReadOnlyList<FileSelectionCandidate> candidates,
        IReadOnlyDictionary<string, int> fileLineCounts,
        IReadOnlyList<SelectedSnippet> selectedSnippets,
        Budget budget)
    {
        var orderedCandidates = candidates
            .OrderByDescending(static candidate => candidate.IsRequired)
            .ThenBy(static candidate => candidate.Priority)
            .ThenBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var snippetPaths = selectedSnippets
            .GroupBy(static snippet => snippet.Path, StringComparer.OrdinalIgnoreCase)
            .Where(group => HasLargeCSharpReplacement(group.Key, fileLineCounts))
            .Select(static group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var selected = new List<SelectedFile>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var totalLines = selectedSnippets.Sum(static snippet => snippet.EndLine - snippet.StartLine + 1);

        foreach (var candidate in orderedCandidates)
        {
            if (!seenPaths.Add(candidate.Path))
            {
                continue;
            }

            if (snippetPaths.Contains(candidate.Path))
            {
                continue;
            }

            var lineCount = fileLineCounts.TryGetValue(candidate.Path, out var knownLineCount)
                ? knownLineCount
                : 1;

            if (!candidate.IsRequired)
            {
                if (budget.MaxFiles > 0 && selected.Count >= budget.MaxFiles)
                {
                    continue;
                }

                if (budget.MaxTotalLines > 0 && totalLines + lineCount > budget.MaxTotalLines)
                {
                    continue;
                }
            }

            totalLines += lineCount;
            selected.Add(new SelectedFile(candidate.Path, candidate.Reason, candidate.Priority, candidate.IsRequired, lineCount));
        }

        return selected;
    }

    private static IReadOnlyList<SelectedSnippet> SelectSnippetsForFile(
        IGrouping<string, SnippetSelectionCandidate> group,
        int maxSnippetsPerFile)
    {
        var selected = new List<SelectedSnippet>();
        foreach (var candidate in group
                     .OrderByDescending(static candidate => candidate.IsRequired)
                     .ThenBy(static candidate => candidate.Priority)
                     .ThenBy(static candidate => candidate.StartLine)
                     .ThenBy(static candidate => candidate.Anchor, StringComparer.OrdinalIgnoreCase))
        {
            var snippet = new SelectedSnippet(
                candidate.Path,
                candidate.Reason,
                candidate.Priority,
                candidate.IsRequired,
                candidate.Anchor,
                candidate.StartLine,
                candidate.EndLine,
                candidate.Content);

            if (TryMerge(selected, snippet))
            {
                continue;
            }

            if (selected.Count >= maxSnippetsPerFile)
            {
                continue;
            }

            selected.Add(snippet);
        }

        return selected
            .OrderBy(static snippet => snippet.Priority)
            .ThenBy(static snippet => snippet.StartLine)
            .ToArray();
    }

    private static IReadOnlyList<PackDecisionTrace> OrderDecisionTraces(
        IReadOnlyList<PackDecisionTrace> decisionTraces) =>
        decisionTraces
            .OrderBy(static trace => trace.Category, StringComparer.Ordinal)
            .ThenBy(static trace => trace.ItemKey, StringComparer.Ordinal)
            .ThenBy(static trace => trace.DecisionKind, StringComparer.Ordinal)
            .ThenByDescending(static trace => trace.Score)
            .ThenBy(static trace => trace.Rank)
            .ToArray();

    private static bool TryMerge(IList<SelectedSnippet> selected, SelectedSnippet candidate)
    {
        for (var index = 0; index < selected.Count; index++)
        {
            var existing = selected[index];
            if (!string.Equals(existing.Path, candidate.Path, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (candidate.StartLine > existing.EndLine + SnippetMergeGapLines
                || existing.StartLine > candidate.EndLine + SnippetMergeGapLines)
            {
                continue;
            }

            selected[index] = Merge(existing, candidate);
            return true;
        }

        return false;
    }

    private static SelectedSnippet Merge(SelectedSnippet existing, SelectedSnippet candidate)
    {
        var startLine = Math.Min(existing.StartLine, candidate.StartLine);
        var endLine = Math.Max(existing.EndLine, candidate.EndLine);
        var anchor = string.Join(
            " / ",
            new[] { existing.Anchor, candidate.Anchor }
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase));
        var content = MergeContent(existing, candidate);

        return new SelectedSnippet(
            existing.Path,
            existing.Priority <= candidate.Priority ? existing.Reason : candidate.Reason,
            Math.Min(existing.Priority, candidate.Priority),
            existing.IsRequired || candidate.IsRequired,
            anchor,
            startLine,
            endLine,
            content);
    }

    private static string MergeContent(SelectedSnippet existing, SelectedSnippet candidate)
    {
        if (candidate.StartLine < existing.StartLine)
        {
            return MergeContent(candidate, existing);
        }

        var existingLines = SplitLines(existing.Content);
        var candidateLines = SplitLines(candidate.Content);
        var overlapStart = Math.Max(existing.StartLine, candidate.StartLine);
        var overlapEnd = Math.Min(existing.EndLine, candidate.EndLine);
        if (overlapStart <= overlapEnd)
        {
            var overlappingLineCount = overlapEnd - overlapStart + 1;
            var additionalLines = candidateLines.Skip(overlappingLineCount);
            return JoinLines(existingLines.Concat(additionalLines));
        }

        return JoinLines(existingLines.Concat(["..."]).Concat(candidateLines));
    }

    private static IReadOnlyList<string> SplitLines(string content) =>
        content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    private static string JoinLines(IEnumerable<string> lines) => string.Join("\n", lines);

    private static bool HasLargeCSharpReplacement(
        string path,
        IReadOnlyDictionary<string, int> fileLineCounts) =>
        path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
        && fileLineCounts.TryGetValue(path, out var lineCount)
        && lineCount > LargeCSharpFileThreshold;
}
