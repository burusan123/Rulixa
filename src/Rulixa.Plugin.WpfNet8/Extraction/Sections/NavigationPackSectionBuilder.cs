using Rulixa.Application.Ports;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class NavigationPackSectionBuilder
{
    private readonly IWorkspaceFileSystem workspaceFileSystem;
    private readonly CSharpSnippetCandidateFactory snippetFactory;
    private readonly NavigationContractExtractor navigationContractExtractor = new();

    internal NavigationPackSectionBuilder(
        IWorkspaceFileSystem workspaceFileSystem,
        CSharpSnippetCandidateFactory snippetFactory)
    {
        this.workspaceFileSystem = workspaceFileSystem ?? throw new ArgumentNullException(nameof(workspaceFileSystem));
        this.snippetFactory = snippetFactory ?? throw new ArgumentNullException(nameof(snippetFactory));
    }

    internal async Task AddAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        RelevantPackContext relevantContext,
        ICollection<Contract> contracts,
        ICollection<IndexSection> indexes,
        ICollection<SnippetSelectionCandidate> snippetCandidates,
        ICollection<FileSelectionCandidate> fileCandidates,
        CancellationToken cancellationToken)
    {
        var candidateViews = GetNavigationCandidateViews(scanResult, resolvedEntry, relevantContext.RelevantBindings);
        var navigationBindings = new List<NavigationBinding>();

        foreach (var viewPath in candidateViews)
        {
            var absolutePath = Path.Combine(workspaceRoot, viewPath.Replace('/', Path.DirectorySeparatorChar));
            var source = await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
            var navigationBinding = navigationContractExtractor.Extract(viewPath, source);
            if (navigationBinding is null)
            {
                continue;
            }

            navigationBindings.Add(navigationBinding);
            contracts.Add(BuildNavigationBindingContract(resolvedEntry, navigationBinding));
            fileCandidates.Add(new FileSelectionCandidate(viewPath, "navigation-view", 8, true));
        }

        if (navigationBindings.Count > 0)
        {
            indexes.Add(BuildNavigationIndex(navigationBindings));
        }

        var navigationCauseSummary = BuildNavigationCauseSummary(navigationBindings, relevantContext.RelevantTransitions);
        if (navigationCauseSummary is not null)
        {
            contracts.Add(navigationCauseSummary.Value.Contract);
            indexes.Add(navigationCauseSummary.Value.Index);
        }

        var transitionGroups = relevantContext.RelevantTransitions
            .GroupBy(static transition => new { transition.ViewModelSymbol, transition.SourceFilePath, transition.UpdateMethodName })
            .OrderBy(static group => group.Key.SourceFilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static group => group.Min(transition => transition.SourceSpan.StartLine))
            .ToArray();

        foreach (var transitionGroup in transitionGroups)
        {
            var firstTransition = transitionGroup.First();
            var expressions = transitionGroup
                .Select(static transition => transition.UpdateExpressionSummary)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var summary = $"{firstTransition.ViewModelSymbol}.{firstTransition.UpdateMethodName}(...) が {string.Join("、", expressions)} を実行します。";

            contracts.Add(new Contract(
                ContractKind.Navigation,
                "ViewModel 更新点",
                summary,
                [firstTransition.SourceFilePath],
                BuildTransitionSymbols(firstTransition, expressions)));

            fileCandidates.Add(new FileSelectionCandidate(firstTransition.SourceFilePath, "navigation-update", -1, true));
            var snippet = await CreateTransitionSnippetAsync(
                    workspaceRoot,
                    scanResult,
                    firstTransition,
                    cancellationToken)
                .ConfigureAwait(false);
            if (snippet is not null)
            {
                snippetCandidates.Add(snippet);
            }
        }

        if (relevantContext.RelevantTransitions.Count > 0)
        {
            indexes.Add(BuildNavigationUpdateIndex(relevantContext.RelevantTransitions));
        }
    }

    private static NavigationCauseSummary? BuildNavigationCauseSummary(
        IReadOnlyList<NavigationBinding> navigationBindings,
        IReadOnlyList<NavigationTransition> relevantTransitions)
    {
        var binding = navigationBindings.FirstOrDefault(binding =>
            !string.IsNullOrWhiteSpace(binding.SelectedItemProperty)
            && !string.IsNullOrWhiteSpace(binding.ContentProperty));
        if (binding is null)
        {
            return null;
        }

        var selectedTransition = relevantTransitions.FirstOrDefault(transition =>
            string.Equals(transition.SelectedItemPropertyName, binding.SelectedItemProperty, StringComparison.Ordinal)
            && transition.UpdateExpressionSummary.StartsWith($"{binding.SelectedItemProperty} =", StringComparison.Ordinal));
        var currentPageTransition = relevantTransitions.FirstOrDefault(transition =>
            string.Equals(transition.CurrentPagePropertyName, binding.ContentProperty, StringComparison.Ordinal)
            && transition.UpdateExpressionSummary.StartsWith($"{binding.ContentProperty} =", StringComparison.Ordinal));
        if (selectedTransition is null || currentPageTransition is null)
        {
            return null;
        }

        var summary = $"{binding.SelectedItemProperty} の選択更新が {binding.ContentProperty} の表示切り替えを駆動します。{selectedTransition.UpdateMethodName}(...) が {selectedTransition.UpdateExpressionSummary}、{currentPageTransition.UpdateMethodName}(...) が {currentPageTransition.UpdateExpressionSummary} を実行します。";
        var contract = new Contract(
            ContractKind.Navigation,
            "選択から表示への因果",
            summary,
            [binding.ViewPath, selectedTransition.SourceFilePath, currentPageTransition.SourceFilePath],
            [
                selectedTransition.ViewModelSymbol,
                binding.SelectedItemProperty!,
                binding.ContentProperty!,
                selectedTransition.UpdateMethodName,
                currentPageTransition.UpdateMethodName,
                selectedTransition.UpdateExpressionSummary,
                currentPageTransition.UpdateExpressionSummary
            ]);
        var index = new IndexSection(
            "選択から表示への因果",
            [$"{binding.SelectedItemProperty} -> {binding.ContentProperty} ({selectedTransition.UpdateMethodName}: {selectedTransition.UpdateExpressionSummary}, {currentPageTransition.UpdateMethodName}: {currentPageTransition.UpdateExpressionSummary})"]);

        return new NavigationCauseSummary(contract, index);
    }

    private static IReadOnlyList<string> GetNavigationCandidateViews(
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        IReadOnlyList<ViewModelBinding> relevantBindings)
    {
        var candidateViews = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in relevantBindings)
        {
            candidateViews.Add(binding.ViewPath);
        }

        if (!string.IsNullOrWhiteSpace(resolvedEntry.ResolvedPath)
            && resolvedEntry.ResolvedPath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
        {
            candidateViews.Add(resolvedEntry.ResolvedPath);
        }

        if (resolvedEntry.ResolvedKind == ResolvedEntryKind.Symbol && !string.IsNullOrWhiteSpace(resolvedEntry.Symbol))
        {
            var viewName = PackExtractionConventions.BuildConventionalViewName(resolvedEntry.Symbol);
            if (!string.IsNullOrWhiteSpace(viewName))
            {
                var viewPath = scanResult.Files
                    .Where(static file => file.Kind == ScanFileKind.Xaml)
                    .Select(static file => file.Path)
                    .FirstOrDefault(path => Path.GetFileNameWithoutExtension(path).Equals(viewName, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(viewPath))
                {
                    candidateViews.Add(viewPath);
                }
            }
        }

        return candidateViews
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Contract BuildNavigationBindingContract(ResolvedEntry resolvedEntry, NavigationBinding binding)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(binding.ItemsSourceProperty))
        {
            parts.Add($"一覧を {binding.ItemsSourceProperty} にバインド");
        }

        if (!string.IsNullOrWhiteSpace(binding.SelectedItemProperty))
        {
            parts.Add($"選択状態を {binding.SelectedItemProperty} にバインド");
        }

        if (!string.IsNullOrWhiteSpace(binding.ContentProperty))
        {
            parts.Add($"表示コンテンツを {binding.ContentProperty} にバインド");
        }

        var summary = $"{binding.ViewPath} では {string.Join("、", parts)} してページ切り替えを表現します。";
        return new Contract(
            ContractKind.Navigation,
            "一覧・選択・表示の対応",
            summary,
            [binding.ViewPath],
            BuildNavigationSymbols(resolvedEntry, binding));
    }

    private static IReadOnlyList<string> BuildNavigationSymbols(ResolvedEntry resolvedEntry, NavigationBinding binding)
    {
        var symbols = new List<string>();
        if (!string.IsNullOrWhiteSpace(resolvedEntry.Symbol))
        {
            symbols.Add(resolvedEntry.Symbol);
        }

        if (!string.IsNullOrWhiteSpace(binding.ItemsSourceProperty))
        {
            symbols.Add(binding.ItemsSourceProperty);
        }

        if (!string.IsNullOrWhiteSpace(binding.SelectedItemProperty))
        {
            symbols.Add(binding.SelectedItemProperty);
        }

        if (!string.IsNullOrWhiteSpace(binding.ContentProperty))
        {
            symbols.Add(binding.ContentProperty);
        }

        return symbols;
    }

    private static IReadOnlyList<string> BuildTransitionSymbols(
        NavigationTransition transition,
        IReadOnlyList<string> expressions)
    {
        var symbols = new List<string> { transition.ViewModelSymbol, transition.UpdateMethodName };
        if (!string.IsNullOrWhiteSpace(transition.SelectedItemPropertyName))
        {
            symbols.Add(transition.SelectedItemPropertyName);
        }

        if (!string.IsNullOrWhiteSpace(transition.CurrentPagePropertyName))
        {
            symbols.Add(transition.CurrentPagePropertyName);
        }

        symbols.AddRange(expressions);
        return symbols;
    }

    private static IndexSection BuildNavigationIndex(IReadOnlyList<NavigationBinding> bindings) =>
        new(
            "ナビゲーション",
            bindings.Select(binding =>
                    $"{binding.ViewPath}: Items={binding.ItemsSourceProperty ?? "-"}, SelectedItem={binding.SelectedItemProperty ?? "-"}, Content={binding.ContentProperty ?? "-"}")
                .ToArray());

    private static IndexSection BuildNavigationUpdateIndex(IReadOnlyList<NavigationTransition> transitions) =>
        new(
            "ナビゲーション更新点",
            transitions.Select(transition =>
                    $"{transition.ViewModelSymbol}.{transition.UpdateMethodName}(...) -> {transition.UpdateExpressionSummary} (line: {transition.SourceSpan.StartLine})")
                .ToArray());

    private async Task<SnippetSelectionCandidate?> CreateTransitionSnippetAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        NavigationTransition transition,
        CancellationToken cancellationToken)
    {
        if (!PackExtractionConventions.ShouldCreateSnippet(scanResult, transition.SourceFilePath))
        {
            return null;
        }

        return await snippetFactory
            .CreateMethodSnippetAsync(
                workspaceRoot,
                transition.SourceFilePath,
                transition.UpdateMethodName,
                "navigation-update",
                10,
                true,
                $"{transition.UpdateMethodName}(...)",
                transition.SourceSpan,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private readonly record struct NavigationCauseSummary(Contract Contract, IndexSection Index);
}
