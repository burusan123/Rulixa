using Rulixa.Application.Ports;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class RelevantPackContextFactory
{
    internal static async Task<RelevantPackContext> CreateAsync(
        string workspaceRoot,
        IWorkspaceFileSystem workspaceFileSystem,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        string goal,
        CancellationToken cancellationToken)
    {
        var goalProfile = GoalDrivenExpansionPlanner.Analyze(goal);
        var relevantViewModelSymbols = FindRelevantViewModelSymbols(scanResult, resolvedEntry);
        var systemPack = await new SystemExpansionPlanner(workspaceFileSystem)
            .PlanAsync(workspaceRoot, scanResult, resolvedEntry, relevantViewModelSymbols, cancellationToken)
            .ConfigureAwait(false);
        var explorationRootSymbols = systemPack?.ExplorationRootSymbols ?? relevantViewModelSymbols;
        var relevantBindings = FindRelevantBindings(scanResult, resolvedEntry, explorationRootSymbols);
        var primaryBindings = relevantBindings
            .Where(static binding => binding.BindingKind is ViewModelBindingKind.RootDataContext or ViewModelBindingKind.ViewDataContext)
            .ToArray();
        var secondaryBindings = relevantBindings
            .Where(static binding => binding.BindingKind == ViewModelBindingKind.DataTemplate)
            .ToArray();
        var relevantTransitions = FindRelevantTransitions(scanResult, resolvedEntry, explorationRootSymbols, relevantBindings);
        var aggregateSymbols = relevantViewModelSymbols
            .Concat(explorationRootSymbols)
            .Concat(relevantBindings.Select(static binding => binding.ViewModelSymbol))
            .Concat(relevantTransitions.Select(static transition => transition.ViewModelSymbol))
            .Concat(systemPack is null ? Array.Empty<string>() : systemPack.RelatedSymbols)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var symbolAggregates = PartialSymbolAggregateResolver.Build(scanResult, aggregateSymbols);
        var relatedSymbols = aggregateSymbols.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new RelevantPackContext(
            goalProfile,
            relevantViewModelSymbols,
            explorationRootSymbols,
            relatedSymbols,
            symbolAggregates,
            relevantBindings,
            primaryBindings,
            secondaryBindings,
            relevantTransitions,
            systemPack);
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
