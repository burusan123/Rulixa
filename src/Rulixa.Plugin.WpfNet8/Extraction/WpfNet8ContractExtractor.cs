using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

public sealed class WpfNet8ContractExtractor : IContractExtractor
{
    private readonly IWorkspaceFileSystem workspaceFileSystem;
    private readonly DependencyInjectionPackSectionBuilder dependencyInjectionBuilder;
    private readonly WorkflowPackSectionBuilder workflowBuilder;
    private readonly PersistencePackSectionBuilder persistenceBuilder;
    private readonly HubObjectPackSectionBuilder hubObjectBuilder;
    private readonly ExternalAssetPackSectionBuilder externalAssetBuilder;
    private readonly ArchitectureTestPackSectionBuilder architectureTestBuilder;
    private readonly NavigationPackSectionBuilder navigationBuilder;
    private readonly DialogPackSectionBuilder dialogBuilder;
    private readonly CSharpSnippetCandidateFactory snippetFactory;

    public WpfNet8ContractExtractor(IWorkspaceFileSystem workspaceFileSystem)
    {
        ArgumentNullException.ThrowIfNull(workspaceFileSystem);

        this.workspaceFileSystem = workspaceFileSystem;
        snippetFactory = new CSharpSnippetCandidateFactory(workspaceFileSystem);
        var xamlSnippetFactory = new XamlSnippetCandidateFactory(workspaceFileSystem);
        dependencyInjectionBuilder = new DependencyInjectionPackSectionBuilder(workspaceFileSystem, snippetFactory);
        workflowBuilder = new WorkflowPackSectionBuilder(workspaceFileSystem, snippetFactory);
        persistenceBuilder = new PersistencePackSectionBuilder(workspaceFileSystem, snippetFactory);
        hubObjectBuilder = new HubObjectPackSectionBuilder(workspaceFileSystem, snippetFactory);
        externalAssetBuilder = new ExternalAssetPackSectionBuilder(workspaceFileSystem);
        architectureTestBuilder = new ArchitectureTestPackSectionBuilder(workspaceFileSystem);
        navigationBuilder = new NavigationPackSectionBuilder(workspaceFileSystem, snippetFactory, xamlSnippetFactory);
        dialogBuilder = new DialogPackSectionBuilder(workspaceFileSystem, snippetFactory);
    }

    public async Task<PackIngredients> ExtractAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        string goal,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentNullException.ThrowIfNull(scanResult);
        ArgumentNullException.ThrowIfNull(resolvedEntry);
        ArgumentException.ThrowIfNullOrWhiteSpace(goal);

        var contracts = new List<Contract>();
        var indexes = new List<IndexSection>();
        var snippetCandidates = new List<SnippetSelectionCandidate>();
        var fileCandidates = new List<FileSelectionCandidate>();
        var decisionTraces = new List<PackDecisionTrace>();
        var unknowns = new List<Diagnostic>();

        if (resolvedEntry.ResolvedKind == ResolvedEntryKind.Unresolved)
        {
            unknowns.Add(BuildUnresolvedDiagnostic(resolvedEntry));
            return new PackIngredients(contracts, indexes, snippetCandidates, fileCandidates, decisionTraces, unknowns);
        }

        AddResolvedEntryFileCandidate(resolvedEntry, fileCandidates);

        var relevantContext = await RelevantPackContextFactory
            .CreateAsync(workspaceRoot, workspaceFileSystem, scanResult, resolvedEntry, goal, cancellationToken)
            .ConfigureAwait(false);

        AddStartupContracts(scanResult, contracts, fileCandidates);
        foreach (var candidate in SystemPackSummaryBuilder.BuildRepresentativeFiles(relevantContext))
        {
            fileCandidates.Add(candidate);
        }
        await dependencyInjectionBuilder
            .AddAsync(workspaceRoot, scanResult, relevantContext, contracts, indexes, snippetCandidates, fileCandidates, cancellationToken)
            .ConfigureAwait(false);
        await ViewBindingPackSectionBuilder
            .AddBindingContractsAsync(
                workspaceRoot,
                scanResult,
                relevantContext.PrimaryBindings,
                true,
                snippetFactory,
                contracts,
                snippetCandidates,
                fileCandidates,
                cancellationToken)
            .ConfigureAwait(false);
        ViewBindingPackSectionBuilder.AddConventionalViewFiles(scanResult, resolvedEntry, fileCandidates);
        await navigationBuilder
            .AddAsync(workspaceRoot, scanResult, resolvedEntry, relevantContext, contracts, indexes, snippetCandidates, fileCandidates, cancellationToken)
            .ConfigureAwait(false);
        ViewBindingPackSectionBuilder.AddDataTemplateSummaryContract(relevantContext.SecondaryBindings, contracts);
        await workflowBuilder
            .AddAsync(
                workspaceRoot,
                scanResult,
                resolvedEntry,
                relevantContext,
                contracts,
                indexes,
                snippetCandidates,
                fileCandidates,
                decisionTraces,
                unknowns,
                cancellationToken)
            .ConfigureAwait(false);
        await persistenceBuilder
            .AddAsync(
                workspaceRoot,
                scanResult,
                relevantContext,
                contracts,
                indexes,
                snippetCandidates,
                fileCandidates,
                decisionTraces,
                unknowns,
                cancellationToken)
            .ConfigureAwait(false);
        await hubObjectBuilder
            .AddAsync(
                workspaceRoot,
                scanResult,
                relevantContext,
                contracts,
                indexes,
                snippetCandidates,
                fileCandidates,
                decisionTraces,
                unknowns,
                cancellationToken)
            .ConfigureAwait(false);
        await externalAssetBuilder
            .AddAsync(
                workspaceRoot,
                scanResult,
                relevantContext,
                contracts,
                indexes,
                fileCandidates,
                decisionTraces,
                unknowns,
                cancellationToken)
            .ConfigureAwait(false);
        await CommandPackSectionBuilder.AddContractsAsync(
                workspaceRoot,
                scanResult,
                resolvedEntry,
                goal,
                workspaceFileSystem,
                snippetFactory,
                contracts,
                snippetCandidates,
                fileCandidates,
                decisionTraces,
                cancellationToken)
            .ConfigureAwait(false);
        await dialogBuilder
            .AddAsync(workspaceRoot, scanResult, resolvedEntry, relevantContext, contracts, snippetCandidates, fileCandidates, cancellationToken)
            .ConfigureAwait(false);
        await architectureTestBuilder
            .AddAsync(
                workspaceRoot,
                scanResult,
                relevantContext,
                contracts,
                indexes,
                fileCandidates,
                decisionTraces,
                unknowns,
                cancellationToken)
            .ConfigureAwait(false);

        indexes.Add(BuildStartupIndex(scanResult));
        indexes.Add(ViewBindingPackSectionBuilder.BuildViewModelIndex(relevantContext.PrimaryBindings, relevantContext.SecondaryBindings));
        indexes.Add(await CommandPackSectionBuilder.BuildIndexAsync(
            workspaceRoot,
            scanResult,
            resolvedEntry,
            goal,
            workspaceFileSystem,
            cancellationToken).ConfigureAwait(false));
        var systemSummaryContract = SystemPackSummaryBuilder.BuildContract(relevantContext, indexes);
        if (systemSummaryContract is not null)
        {
            contracts.Add(systemSummaryContract);
        }

        return new PackIngredients(
            Contracts: contracts.Distinct().ToArray(),
            Indexes: indexes.Where(static index => index.Lines.Count > 0).ToArray(),
            SnippetCandidates: OrderSnippetCandidates(snippetCandidates),
            FileCandidates: OrderFileCandidates(fileCandidates),
            DecisionTraces: decisionTraces,
            Unknowns: SystemUnknownAggregationSupport.Aggregate(scanResult, relevantContext, unknowns));
    }

    private static Diagnostic BuildUnresolvedDiagnostic(ResolvedEntry resolvedEntry) =>
        new(
            "entry.unresolved",
            "エントリを一意に解決できませんでした。",
            null,
            DiagnosticSeverity.Warning,
            resolvedEntry.Candidates
                .Select(static candidate => candidate.Path ?? candidate.Symbol ?? string.Empty)
                .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
                .ToArray());

    private static void AddResolvedEntryFileCandidate(
        ResolvedEntry resolvedEntry,
        ICollection<FileSelectionCandidate> fileCandidates)
    {
        if (!string.IsNullOrWhiteSpace(resolvedEntry.ResolvedPath))
        {
            fileCandidates.Add(new FileSelectionCandidate(resolvedEntry.ResolvedPath, "entry", 0, true));
        }
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

    private static IndexSection BuildStartupIndex(WorkspaceScanResult scanResult) =>
        new(
            "起動経路",
            scanResult.ProjectSummary.EntryPoints
                .Select(entryPoint => $"{entryPoint} -> {string.Join(", ", scanResult.ProjectSummary.RootViewModels)}")
                .ToArray());

    private static IReadOnlyList<FileSelectionCandidate> OrderFileCandidates(IReadOnlyList<FileSelectionCandidate> fileCandidates) =>
        fileCandidates
            .GroupBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group
                .OrderByDescending(static candidate => candidate.IsRequired)
                .ThenBy(static candidate => candidate.Priority)
                .First())
            .OrderByDescending(static candidate => candidate.IsRequired)
            .ThenBy(static candidate => candidate.Priority)
            .ThenBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IReadOnlyList<SnippetSelectionCandidate> OrderSnippetCandidates(
        IReadOnlyList<SnippetSelectionCandidate> snippetCandidates) =>
        snippetCandidates
            .OrderByDescending(static candidate => candidate.IsRequired)
            .ThenBy(static candidate => candidate.Priority)
            .ThenBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.StartLine)
            .ThenBy(static candidate => candidate.Anchor, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
