using Rulixa.Domain.Entries;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class RelevantPackContextFactory
{
    internal static RelevantPackContext Create(WorkspaceScanResult scanResult, ResolvedEntry resolvedEntry)
    {
        var relevantViewModelSymbols = FindRelevantViewModelSymbols(scanResult, resolvedEntry);
        var relevantBindings = FindRelevantBindings(scanResult, resolvedEntry, relevantViewModelSymbols);
        var primaryBindings = relevantBindings
            .Where(static binding => binding.BindingKind is ViewModelBindingKind.RootDataContext or ViewModelBindingKind.ViewDataContext)
            .ToArray();
        var secondaryBindings = relevantBindings
            .Where(static binding => binding.BindingKind == ViewModelBindingKind.DataTemplate)
            .ToArray();
        var relevantTransitions = FindRelevantTransitions(scanResult, resolvedEntry, relevantViewModelSymbols, relevantBindings);

        return new RelevantPackContext(
            relevantViewModelSymbols,
            relevantBindings,
            primaryBindings,
            secondaryBindings,
            relevantTransitions);
    }

    private static IReadOnlySet<string> FindRelevantViewModelSymbols(
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry)
    {
        var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(resolvedEntry.Symbol))
        {
            symbols.Add(resolvedEntry.Symbol);
        }

        if (string.IsNullOrWhiteSpace(resolvedEntry.ResolvedPath))
        {
            return symbols;
        }

        foreach (var command in scanResult.Commands)
        {
            if (command.BoundViews.Any(view => string.Equals(view, resolvedEntry.ResolvedPath, StringComparison.OrdinalIgnoreCase)))
            {
                symbols.Add(command.ViewModelSymbol);
            }
        }

        var conventionalViewModel = PackExtractionConventions.FindConventionalViewModelSymbol(scanResult, resolvedEntry.ResolvedPath);
        if (!string.IsNullOrWhiteSpace(conventionalViewModel))
        {
            symbols.Add(conventionalViewModel);
        }

        return symbols;
    }

    private static IReadOnlyList<ViewModelBinding> FindRelevantBindings(
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        IReadOnlySet<string> relevantViewModelSymbols)
    {
        return scanResult.ViewModelBindings
            .Where(binding =>
                string.Equals(binding.ViewPath, resolvedEntry.ResolvedPath, StringComparison.OrdinalIgnoreCase)
                || relevantViewModelSymbols.Contains(binding.ViewModelSymbol))
            .OrderBy(static binding => binding.BindingKind)
            .ThenBy(static binding => binding.ViewPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<NavigationTransition> FindRelevantTransitions(
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        IReadOnlySet<string> relevantViewModelSymbols,
        IReadOnlyList<ViewModelBinding> relevantBindings)
    {
        var relevantSymbols = new HashSet<string>(relevantViewModelSymbols, StringComparer.OrdinalIgnoreCase);
        foreach (var binding in relevantBindings)
        {
            if (!string.IsNullOrWhiteSpace(binding.ViewModelSymbol))
            {
                relevantSymbols.Add(binding.ViewModelSymbol);
            }
        }

        return scanResult.NavigationTransitions
            .Where(transition =>
                relevantSymbols.Contains(transition.ViewModelSymbol)
                || string.Equals(transition.SourceFilePath, resolvedEntry.ResolvedPath, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static transition => transition.SourceFilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static transition => transition.SourceSpan.StartLine)
            .ToArray();
    }
}
