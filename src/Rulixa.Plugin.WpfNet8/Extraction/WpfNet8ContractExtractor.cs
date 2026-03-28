using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

public sealed class WpfNet8ContractExtractor : IContractExtractor
{
    private readonly DependencyInjectionPackSectionBuilder dependencyInjectionBuilder;
    private readonly NavigationPackSectionBuilder navigationBuilder;
    private readonly DialogPackSectionBuilder dialogBuilder;

    public WpfNet8ContractExtractor(IWorkspaceFileSystem workspaceFileSystem)
    {
        ArgumentNullException.ThrowIfNull(workspaceFileSystem);

        dependencyInjectionBuilder = new DependencyInjectionPackSectionBuilder(workspaceFileSystem);
        navigationBuilder = new NavigationPackSectionBuilder(workspaceFileSystem);
        dialogBuilder = new DialogPackSectionBuilder(workspaceFileSystem);
    }

    public async Task<PackIngredients> ExtractAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentNullException.ThrowIfNull(scanResult);
        ArgumentNullException.ThrowIfNull(resolvedEntry);

        var contracts = new List<Contract>();
        var indexes = new List<IndexSection>();
        var fileCandidates = new List<FileSelectionCandidate>();
        var unknowns = new List<Diagnostic>();

        if (resolvedEntry.ResolvedKind == ResolvedEntryKind.Unresolved)
        {
            unknowns.Add(BuildUnresolvedDiagnostic(resolvedEntry));
            return new PackIngredients(contracts, indexes, fileCandidates, unknowns);
        }

        AddResolvedEntryFileCandidate(resolvedEntry, fileCandidates);

        var relevantContext = RelevantPackContextFactory.Create(scanResult, resolvedEntry);

        AddStartupContracts(scanResult, contracts, fileCandidates);
        await dependencyInjectionBuilder
            .AddAsync(workspaceRoot, scanResult, relevantContext, contracts, indexes, fileCandidates, cancellationToken)
            .ConfigureAwait(false);
        ViewBindingPackSectionBuilder.AddBindingContracts(scanResult, relevantContext.PrimaryBindings, true, contracts, fileCandidates);
        ViewBindingPackSectionBuilder.AddConventionalViewFiles(scanResult, resolvedEntry, fileCandidates);
        await navigationBuilder
            .AddAsync(workspaceRoot, scanResult, resolvedEntry, relevantContext, contracts, indexes, fileCandidates, cancellationToken)
            .ConfigureAwait(false);
        ViewBindingPackSectionBuilder.AddDataTemplateSummaryContract(relevantContext.SecondaryBindings, contracts);
        CommandPackSectionBuilder.AddContracts(scanResult, resolvedEntry, contracts, fileCandidates);
        await dialogBuilder
            .AddAsync(workspaceRoot, scanResult, resolvedEntry, contracts, fileCandidates, cancellationToken)
            .ConfigureAwait(false);

        indexes.Add(BuildStartupIndex(scanResult));
        indexes.Add(ViewBindingPackSectionBuilder.BuildViewModelIndex(relevantContext.PrimaryBindings, relevantContext.SecondaryBindings));
        indexes.Add(CommandPackSectionBuilder.BuildIndex(scanResult, resolvedEntry));

        return new PackIngredients(
            Contracts: contracts.Distinct().ToArray(),
            Indexes: indexes.Where(static index => index.Lines.Count > 0).ToArray(),
            FileCandidates: OrderFileCandidates(fileCandidates),
            Unknowns: unknowns);
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
}
