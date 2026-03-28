using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

public sealed class WpfNet8ContractExtractor : IContractExtractor
{
    private const int CommandSummaryThreshold = 6;
    private readonly IWorkspaceFileSystem workspaceFileSystem;
    private readonly NavigationContractExtractor navigationContractExtractor = new();

    public WpfNet8ContractExtractor(IWorkspaceFileSystem workspaceFileSystem)
    {
        this.workspaceFileSystem = workspaceFileSystem ?? throw new ArgumentNullException(nameof(workspaceFileSystem));
    }

    public async Task<PackIngredients> ExtractAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        CancellationToken cancellationToken = default)
    {
        var contracts = new List<Contract>();
        var indexes = new List<IndexSection>();
        var fileCandidates = new List<FileSelectionCandidate>();
        var unknowns = new List<Diagnostic>();

        if (resolvedEntry.ResolvedKind == ResolvedEntryKind.Unresolved)
        {
            unknowns.Add(new Diagnostic(
                "entry.unresolved",
                "エントリを一意に解決できませんでした。",
                null,
                DiagnosticSeverity.Warning,
                resolvedEntry.Candidates
                    .Select(static candidate => candidate.Path ?? candidate.Symbol ?? string.Empty)
                    .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
                    .ToArray()));

            return new PackIngredients(contracts, indexes, fileCandidates, unknowns);
        }

        if (!string.IsNullOrWhiteSpace(resolvedEntry.ResolvedPath))
        {
            fileCandidates.Add(new FileSelectionCandidate(resolvedEntry.ResolvedPath, "entry", 0, true));
        }

        var relevantViewModelSymbols = FindRelevantViewModelSymbols(scanResult, resolvedEntry);
        var relevantBindings = FindRelevantBindings(scanResult, resolvedEntry, relevantViewModelSymbols);
        var primaryBindings = relevantBindings
            .Where(static binding => binding.BindingKind is ViewModelBindingKind.RootDataContext or ViewModelBindingKind.ViewDataContext)
            .ToArray();
        var secondaryBindings = relevantBindings
            .Where(static binding => binding.BindingKind == ViewModelBindingKind.DataTemplate)
            .ToArray();
        var relevantTransitions = FindRelevantTransitions(scanResult, resolvedEntry, relevantViewModelSymbols, relevantBindings);

        AddStartupContracts(scanResult, contracts, fileCandidates);
        AddDependencyInjectionContracts(scanResult, contracts, fileCandidates);
        AddBindingContracts(scanResult, primaryBindings, true, contracts, fileCandidates);
        AddConventionalViewFiles(scanResult, resolvedEntry, fileCandidates);
        await AddNavigationContractsAsync(
                workspaceRoot,
                scanResult,
                resolvedEntry,
                relevantBindings,
                relevantTransitions,
                contracts,
                indexes,
                fileCandidates,
                cancellationToken)
            .ConfigureAwait(false);
        AddDataTemplateSummaryContract(secondaryBindings, contracts);
        AddCommandContracts(scanResult, resolvedEntry, contracts, fileCandidates);
        await AddReachableDialogContractsAsync(
                workspaceRoot,
                scanResult,
                resolvedEntry,
                contracts,
                fileCandidates,
                cancellationToken)
            .ConfigureAwait(false);

        indexes.Add(BuildStartupIndex(scanResult));
        indexes.Add(BuildViewModelIndex(primaryBindings, secondaryBindings));
        indexes.Add(BuildCommandIndex(scanResult, resolvedEntry));

        return new PackIngredients(
            Contracts: contracts.Distinct().ToArray(),
            Indexes: indexes.Where(static index => index.Lines.Count > 0).ToArray(),
            FileCandidates: fileCandidates
                .GroupBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
                .Select(static group => group
                    .OrderByDescending(static candidate => candidate.IsRequired)
                    .ThenBy(static candidate => candidate.Priority)
                    .First())
                .OrderByDescending(static candidate => candidate.IsRequired)
                .ThenBy(static candidate => candidate.Priority)
                .ThenBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Unknowns: unknowns);
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

        var conventionalViewModel = FindConventionalViewModelSymbol(scanResult, resolvedEntry.ResolvedPath);
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
            .ThenBy(static transition => transition.StartLine)
            .ToArray();
    }

    private static void AddStartupContracts(
        WorkspaceScanResult scanResult,
        ICollection<Contract> contracts,
        ICollection<FileSelectionCandidate> fileCandidates)
    {
        var startupFiles = scanResult.Files
            .Where(static file => file.Kind == ScanFileKind.Startup)
            .OrderBy(static file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (startupFiles.Length == 0)
        {
            return;
        }

        contracts.Add(new Contract(
            ContractKind.Startup,
            "起動経路",
            "App と MainWindow がルート ViewModel を構成します。",
            startupFiles.Select(static file => file.Path).ToArray(),
            scanResult.ProjectSummary.RootViewModels));

        foreach (var startupFile in startupFiles)
        {
            fileCandidates.Add(new FileSelectionCandidate(startupFile.Path, "startup", 10, true));
        }
    }

    private static void AddDependencyInjectionContracts(
        WorkspaceScanResult scanResult,
        ICollection<Contract> contracts,
        ICollection<FileSelectionCandidate> fileCandidates)
    {
        var registrationFiles = scanResult.ServiceRegistrations
            .Select(static registration => registration.RegistrationFile)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (registrationFiles.Length == 0)
        {
            return;
        }

        contracts.Add(new Contract(
            ContractKind.DependencyInjection,
            "DI 登録",
            "ViewModel と Service は DI 登録を通じて構成されます。",
            registrationFiles,
            scanResult.ServiceRegistrations.Select(static registration => registration.ServiceType).ToArray()));

        foreach (var registrationFile in registrationFiles)
        {
            fileCandidates.Add(new FileSelectionCandidate(registrationFile, "dependency-injection", 20, true));
        }
    }

    private static void AddBindingContracts(
        WorkspaceScanResult scanResult,
        IReadOnlyList<ViewModelBinding> bindings,
        bool required,
        ICollection<Contract> contracts,
        ICollection<FileSelectionCandidate> fileCandidates)
    {
        foreach (var binding in bindings)
        {
            var priority = binding.BindingKind == ViewModelBindingKind.DataTemplate ? 40 : 5;
            var title = binding.BindingKind switch
            {
                ViewModelBindingKind.RootDataContext => "ルート DataContext",
                ViewModelBindingKind.ViewDataContext => "View DataContext",
                ViewModelBindingKind.DataTemplate => "DataTemplate",
                _ => binding.BindingKind.ToString()
            };

            var summary = binding.BindingKind switch
            {
                ViewModelBindingKind.RootDataContext =>
                    $"{Path.GetFileName(binding.SourcePath)} が {binding.ViewPath} の DataContext に {binding.ViewModelSymbol} を設定します。",
                ViewModelBindingKind.ViewDataContext =>
                    $"{Path.GetFileName(binding.SourcePath)} が {binding.ViewPath} の DataContext に {binding.ViewModelSymbol} を設定します。",
                ViewModelBindingKind.DataTemplate =>
                    $"{binding.ViewPath} は {binding.ViewModelSymbol} 向けの DataTemplate を定義します。",
                _ =>
                    $"{binding.ViewPath} は {binding.ViewModelSymbol} に対応します。"
            };

            contracts.Add(new Contract(
                ContractKind.ViewModelBinding,
                title,
                summary,
                [binding.ViewPath, binding.SourcePath],
                [binding.ViewSymbol, binding.ViewModelSymbol]));

            var reason = binding.BindingKind switch
            {
                ViewModelBindingKind.RootDataContext => "root-binding",
                ViewModelBindingKind.ViewDataContext => "view-binding",
                ViewModelBindingKind.DataTemplate => "data-template",
                _ => "view-binding"
            };
            var sourceReason = binding.BindingKind switch
            {
                ViewModelBindingKind.RootDataContext => "root-binding-source",
                ViewModelBindingKind.ViewDataContext => "view-binding-source",
                ViewModelBindingKind.DataTemplate => "data-template-source",
                _ => "view-binding-source"
            };

            fileCandidates.Add(new FileSelectionCandidate(binding.ViewPath, reason, priority, required));
            fileCandidates.Add(new FileSelectionCandidate(
                binding.SourcePath,
                sourceReason,
                priority + 5,
                required && binding.BindingKind != ViewModelBindingKind.RootDataContext));

            var viewModelFile = scanResult.Symbols.FirstOrDefault(symbol =>
                string.Equals(symbol.QualifiedName, binding.ViewModelSymbol, StringComparison.OrdinalIgnoreCase))?.FilePath;
            if (!string.IsNullOrWhiteSpace(viewModelFile))
            {
                fileCandidates.Add(new FileSelectionCandidate(viewModelFile, reason, priority, required));
            }
        }
    }

    private static void AddDataTemplateSummaryContract(
        IReadOnlyList<ViewModelBinding> dataTemplateBindings,
        ICollection<Contract> contracts)
    {
        if (dataTemplateBindings.Count == 0)
        {
            return;
        }

        var firstBinding = dataTemplateBindings[0];
        var sampleNames = dataTemplateBindings
            .Select(static binding => binding.ViewModelSymbol.Split('.').Last())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();
        var summary = $"{firstBinding.ViewPath} には {dataTemplateBindings.Count} 件の DataTemplate 二次文脈があります。例: {string.Join(", ", sampleNames)}。";

        contracts.Add(new Contract(
            ContractKind.ViewModelBinding,
            "DataTemplate 二次文脈",
            summary,
            [firstBinding.ViewPath],
            dataTemplateBindings
                .Select(static binding => binding.ViewModelSymbol)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()));
    }

    private static void AddConventionalViewFiles(
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        ICollection<FileSelectionCandidate> fileCandidates)
    {
        if (resolvedEntry.ResolvedKind != ResolvedEntryKind.Symbol || string.IsNullOrWhiteSpace(resolvedEntry.Symbol))
        {
            if (!string.IsNullOrWhiteSpace(resolvedEntry.ResolvedPath)
                && resolvedEntry.ResolvedPath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                AddCodeBehindIfPresent(scanResult, resolvedEntry.ResolvedPath, fileCandidates, 1, true);
            }

            return;
        }

        var viewName = BuildConventionalViewName(resolvedEntry.Symbol);
        if (string.IsNullOrWhiteSpace(viewName))
        {
            return;
        }

        var viewPath = scanResult.Files
            .Where(static file => file.Kind == ScanFileKind.Xaml)
            .Select(static file => file.Path)
            .FirstOrDefault(path => Path.GetFileNameWithoutExtension(path).Equals(viewName, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(viewPath))
        {
            return;
        }

        fileCandidates.Add(new FileSelectionCandidate(viewPath, "conventional-view", 2, true));
        AddCodeBehindIfPresent(scanResult, viewPath, fileCandidates, 3, true);
    }

    private static string? BuildConventionalViewName(string symbol)
    {
        var displayName = symbol.Split('.').Last();
        return displayName.EndsWith("ViewModel", StringComparison.Ordinal)
            ? $"{displayName[..^"ViewModel".Length]}View"
            : null;
    }

    private static string? FindConventionalViewModelSymbol(WorkspaceScanResult scanResult, string resolvedPath)
    {
        if (!resolvedPath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var fileName = Path.GetFileNameWithoutExtension(resolvedPath);
        if (!fileName.EndsWith("View", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var viewModelName = $"{fileName[..^"View".Length]}ViewModel";
        return scanResult.Symbols
            .FirstOrDefault(symbol =>
                symbol.Kind == SymbolKind.Class
                && string.Equals(symbol.DisplayName, viewModelName, StringComparison.OrdinalIgnoreCase))
            ?.QualifiedName;
    }

    private static void AddCodeBehindIfPresent(
        WorkspaceScanResult scanResult,
        string viewPath,
        ICollection<FileSelectionCandidate> fileCandidates,
        int priority,
        bool required)
    {
        var codeBehindPath = $"{viewPath}.cs";
        if (scanResult.Files.Any(file => string.Equals(file.Path, codeBehindPath, StringComparison.OrdinalIgnoreCase)))
        {
            fileCandidates.Add(new FileSelectionCandidate(codeBehindPath, "code-behind", priority, required));
        }
    }

    private async Task AddNavigationContractsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        IReadOnlyList<ViewModelBinding> relevantBindings,
        IReadOnlyList<NavigationTransition> relevantTransitions,
        ICollection<Contract> contracts,
        ICollection<IndexSection> indexes,
        ICollection<FileSelectionCandidate> fileCandidates,
        CancellationToken cancellationToken)
    {
        var candidateViews = GetNavigationCandidateViews(scanResult, resolvedEntry, relevantBindings);
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

        var navigationCauseSummary = BuildNavigationCauseSummary(navigationBindings, relevantTransitions);
        if (navigationCauseSummary is not null)
        {
            contracts.Add(navigationCauseSummary.Value.Contract);
            indexes.Add(navigationCauseSummary.Value.Index);
        }

        var transitionGroups = relevantTransitions
            .GroupBy(static transition => new { transition.ViewModelSymbol, transition.SourceFilePath, transition.UpdateMethodName })
            .OrderBy(static group => group.Key.SourceFilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static group => group.Min(transition => transition.StartLine))
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
        }

        if (relevantTransitions.Count > 0)
        {
            indexes.Add(BuildNavigationUpdateIndex(relevantTransitions));
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
            var viewName = BuildConventionalViewName(resolvedEntry.Symbol);
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
        var symbols = new List<string>
        {
            transition.ViewModelSymbol,
            transition.UpdateMethodName
        };

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

    private static void AddCommandContracts(
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        ICollection<Contract> contracts,
        ICollection<FileSelectionCandidate> fileCandidates)
    {
        var commands = scanResult.Commands
            .Where(command =>
                string.Equals(command.ViewModelSymbol, resolvedEntry.Symbol, StringComparison.OrdinalIgnoreCase)
                || command.BoundViews.Any(view => string.Equals(view, resolvedEntry.ResolvedPath, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(static command => command.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (commands.Length > CommandSummaryThreshold)
        {
            var sampleNames = commands
                .Select(static command => command.PropertyName)
                .Take(3)
                .ToArray();
            var summary = $"{commands[0].ViewModelSymbol} には {commands.Length} 件のコマンド導線があります。例: {string.Join(", ", sampleNames)}。";
            contracts.Add(new Contract(
                ContractKind.Command,
                "コマンド導線の要約",
                summary,
                commands.SelectMany(static command => command.BoundViews).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                commands.Select(static command => command.ExecuteSymbol).ToArray()));
        }
        else
        {
            foreach (var command in commands)
            {
                contracts.Add(new Contract(
                    ContractKind.Command,
                    command.PropertyName,
                    $"{command.PropertyName} が {command.ExecuteSymbol} を実行します。",
                    command.BoundViews,
                    [command.ViewModelSymbol, command.ExecuteSymbol]));
            }
        }

        foreach (var command in commands)
        {
            var viewModelFile = scanResult.Symbols.FirstOrDefault(symbol =>
                string.Equals(symbol.QualifiedName, command.ViewModelSymbol, StringComparison.OrdinalIgnoreCase))?.FilePath;
            if (!string.IsNullOrWhiteSpace(viewModelFile))
            {
                fileCandidates.Add(new FileSelectionCandidate(viewModelFile, "command-viewmodel", 6, true));
            }

            foreach (var boundView in command.BoundViews)
            {
                fileCandidates.Add(new FileSelectionCandidate(boundView, "command-bound-view", 4, true));
                AddCodeBehindIfPresent(scanResult, boundView, fileCandidates, 5, true);
            }
        }

        var delegateCommandFile = scanResult.Files.FirstOrDefault(file =>
            Path.GetFileName(file.Path).Equals("DelegateCommand.cs", StringComparison.OrdinalIgnoreCase));
        if (delegateCommandFile is not null && commands.Length > 0)
        {
            fileCandidates.Add(new FileSelectionCandidate(delegateCommandFile.Path, "command-support", 30, true));
        }
    }

    private async Task AddReachableDialogContractsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        ICollection<Contract> contracts,
        ICollection<FileSelectionCandidate> fileCandidates,
        CancellationToken cancellationToken)
    {
        if (resolvedEntry.ResolvedKind != ResolvedEntryKind.Symbol || string.IsNullOrWhiteSpace(resolvedEntry.ResolvedPath))
        {
            return;
        }

        var absolutePath = Path.Combine(workspaceRoot, resolvedEntry.ResolvedPath.Replace('/', Path.DirectorySeparatorChar));
        var source = await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);

        var reachableImplementations = scanResult.ServiceRegistrations
            .Where(registration =>
                SourceContainsIdentifier(source, registration.ServiceType)
                || SourceContainsIdentifier(source, registration.ImplementationType))
            .Select(static registration => registration.ImplementationType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var activations = scanResult.WindowActivations
            .Where(activation => reachableImplementations.Any(implementation =>
                activation.ServiceSymbol.EndsWith($".{implementation}", StringComparison.OrdinalIgnoreCase)
                || activation.CallerSymbol.Contains(implementation, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(static activation => activation.WindowSymbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var activation in activations)
        {
            contracts.Add(new Contract(
                ContractKind.DialogActivation,
                activation.WindowSymbol,
                $"{activation.CallerSymbol} から {activation.WindowSymbol} が起動されます。",
                [],
                [activation.CallerSymbol, activation.ServiceSymbol, activation.WindowSymbol]));

            var serviceFile = scanResult.Symbols.FirstOrDefault(symbol =>
                string.Equals(symbol.QualifiedName, activation.ServiceSymbol, StringComparison.OrdinalIgnoreCase))?.FilePath;
            if (!string.IsNullOrWhiteSpace(serviceFile))
            {
                fileCandidates.Add(new FileSelectionCandidate(serviceFile, "dialog-service", 12, false));
            }
        }
    }

    private static bool SourceContainsIdentifier(string source, string identifier)
    {
        var simpleName = identifier.Split('.').Last();
        return source.Contains(simpleName, StringComparison.Ordinal);
    }

    private static IndexSection BuildStartupIndex(WorkspaceScanResult scanResult) =>
        new(
            "起動経路",
            scanResult.ProjectSummary.EntryPoints
                .Select(entryPoint => $"{entryPoint} -> {string.Join(", ", scanResult.ProjectSummary.RootViewModels)}")
                .ToArray());

    private static IndexSection BuildViewModelIndex(
        IReadOnlyList<ViewModelBinding> primaryBindings,
        IReadOnlyList<ViewModelBinding> secondaryBindings)
    {
        var lines = primaryBindings
            .Select(binding => $"{binding.ViewPath} <-> {binding.ViewModelSymbol} ({DescribeBindingKind(binding.BindingKind)}: {binding.SourcePath})")
            .ToList();

        if (secondaryBindings.Count > 0)
        {
            var firstBinding = secondaryBindings[0];
            var sampleNames = secondaryBindings
                .Select(static binding => binding.ViewModelSymbol.Split('.').Last())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToArray();
            lines.Add($"{firstBinding.ViewPath} <-> DataTemplate 二次文脈 {secondaryBindings.Count}件 (例: {string.Join(", ", sampleNames)})");
        }

        return new IndexSection("View-ViewModel", lines);
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
                    $"{transition.ViewModelSymbol}.{transition.UpdateMethodName}(...) -> {transition.UpdateExpressionSummary} (line: {transition.StartLine})")
                .ToArray());

    private static IndexSection BuildCommandIndex(WorkspaceScanResult scanResult, ResolvedEntry resolvedEntry) =>
        BuildCommandIndex(
            scanResult.Commands
                .Where(command =>
                    string.Equals(command.ViewModelSymbol, resolvedEntry.Symbol, StringComparison.OrdinalIgnoreCase)
                    || command.BoundViews.Any(view => string.Equals(view, resolvedEntry.ResolvedPath, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(static command => command.PropertyName, StringComparer.OrdinalIgnoreCase)
                .ToArray());

    private static IndexSection BuildCommandIndex(IReadOnlyList<CommandBinding> commands)
    {
        if (commands.Count > CommandSummaryThreshold)
        {
            var sampleNames = commands
                .Select(static command => command.PropertyName)
                .Take(3)
                .ToArray();
            return new IndexSection(
                "コマンド",
                [$"{commands.Count}件のコマンド導線 (例: {string.Join(", ", sampleNames)})"]);
        }

        return new IndexSection(
            "コマンド",
            commands.Select(command => $"{command.PropertyName} -> {command.ExecuteSymbol}").ToArray());
    }

    private static string DescribeBindingKind(ViewModelBindingKind bindingKind) => bindingKind switch
    {
        ViewModelBindingKind.RootDataContext => "ルート DataContext",
        ViewModelBindingKind.ViewDataContext => "View DataContext",
        ViewModelBindingKind.DataTemplate => "DataTemplate",
        _ => bindingKind.ToString()
    };

    private readonly record struct NavigationCauseSummary(Contract Contract, IndexSection Index);
}
