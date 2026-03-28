using Rulixa.Domain.Entries;
using Rulixa.Domain.Scanning;

namespace Rulixa.Domain.Packs;

public static class ContextPackFactory
{
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

        var selectedFiles = SelectFiles(ingredients.FileCandidates, fileLineCounts, budget);

        return new ContextPack(
            Goal: goal,
            Entry: entry,
            ResolvedEntry: resolvedEntry,
            Contracts: ingredients.Contracts,
            Indexes: ingredients.Indexes,
            SelectedFiles: selectedFiles,
            Unknowns: ingredients.Unknowns);
    }

    private static IReadOnlyList<SelectedFile> SelectFiles(
        IReadOnlyList<FileSelectionCandidate> candidates,
        IReadOnlyDictionary<string, int> fileLineCounts,
        Budget budget)
    {
        var orderedCandidates = candidates
            .OrderByDescending(static candidate => candidate.IsRequired)
            .ThenBy(static candidate => candidate.Priority)
            .ThenBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var selected = new List<SelectedFile>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var totalLines = 0;

        foreach (var candidate in orderedCandidates)
        {
            if (!seenPaths.Add(candidate.Path))
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
}
