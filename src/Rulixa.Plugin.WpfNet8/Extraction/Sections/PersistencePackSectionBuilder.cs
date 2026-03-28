using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class PersistencePackSectionBuilder
{
    private readonly IWorkspaceFileSystem workspaceFileSystem;
    private readonly CSharpSnippetCandidateFactory snippetFactory;

    internal PersistencePackSectionBuilder(
        IWorkspaceFileSystem workspaceFileSystem,
        CSharpSnippetCandidateFactory snippetFactory)
    {
        this.workspaceFileSystem = workspaceFileSystem ?? throw new ArgumentNullException(nameof(workspaceFileSystem));
        this.snippetFactory = snippetFactory ?? throw new ArgumentNullException(nameof(snippetFactory));
    }

    internal async Task AddAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        ICollection<Contract> contracts,
        ICollection<IndexSection> indexes,
        ICollection<SnippetSelectionCandidate> snippetCandidates,
        ICollection<FileSelectionCandidate> fileCandidates,
        ICollection<Diagnostic> unknowns,
        CancellationToken cancellationToken)
    {
        var links = await DiscoverLinksAsync(workspaceRoot, scanResult, relevantContext, cancellationToken).ConfigureAwait(false);
        if (links.Count == 0)
        {
            if (relevantContext.GoalProfile.HasCategory("system") || relevantContext.GoalProfile.HasCategory("project"))
            {
                unknowns.Add(new Diagnostic(
                    "persistence.unresolved",
                    "Persistence-related symbols were not resolved within the current expansion budget.",
                    null,
                    DiagnosticSeverity.Info,
                    relevantContext.ViewModelSymbols.ToArray()));
            }

            return;
        }

        indexes.Add(new IndexSection("Persistence", links.Select(static link => link.ToIndexLine()).ToArray()));
        contracts.Add(new Contract(
            ContractKind.DependencyInjection,
            "Persistence",
            BuildSummary(links),
            links.SelectMany(static link => link.FilePaths).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            links.SelectMany(static link => link.RelatedSymbols).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));

        foreach (var filePath in links.SelectMany(static link => link.FilePaths).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            fileCandidates.Add(new FileSelectionCandidate(filePath, "persistence", 26, false));
        }

        await AddSnippetsAsync(workspaceRoot, scanResult, relevantContext, links, snippetCandidates, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<PersistenceLink>> DiscoverLinksAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        CancellationToken cancellationToken)
    {
        var links = new List<PersistenceLink>();

        foreach (var rootSymbol in relevantContext.ViewModelSymbols.OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase))
        {
            var firstHopSymbols = await DiscoverFirstHopSymbolsAsync(
                    workspaceRoot,
                    scanResult,
                    relevantContext,
                    rootSymbol,
                    cancellationToken)
                .ConfigureAwait(false);
            foreach (var firstHopSymbol in firstHopSymbols)
            {
                var persistenceSymbols = await DiscoverPersistenceSymbolsAsync(
                        workspaceRoot,
                        scanResult,
                        relevantContext,
                        firstHopSymbol,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (persistenceSymbols.Count == 0 && PackAnalysisHelpers.IsPersistenceLikeName(PackExtractionConventions.GetSimpleTypeName(firstHopSymbol)))
                {
                    persistenceSymbols = [firstHopSymbol];
                }

                foreach (var persistenceSymbol in persistenceSymbols.OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase))
                {
                    var filePaths = PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, rootSymbol)
                        .Concat(PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, firstHopSymbol))
                        .Concat(PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, persistenceSymbol))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    links.Add(new PersistenceLink(rootSymbol, firstHopSymbol, persistenceSymbol, filePaths));
                }
            }
        }

        return links
            .DistinctBy(static link => link.Key, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static link => link.RootDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static link => link.PersistenceDisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<HashSet<string>> DiscoverFirstHopSymbolsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        string symbol,
        CancellationToken cancellationToken)
    {
        var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { symbol };
        foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, symbol))
        {
            var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
            foreach (var dependency in PackAnalysisHelpers.ExtractConstructorDependencyTypeNames(source, PackExtractionConventions.GetSimpleTypeName(symbol)))
            {
                AddIfResolvable(scanResult, symbols, dependency);
            }

            foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeSymbols(
                         scanResult,
                         source,
                         static name => PackAnalysisHelpers.IsWorkflowLikeName(name) || PackAnalysisHelpers.IsPersistenceLikeName(name)))
            {
                symbols.Add(referenced);
            }
        }

        return symbols;
    }

    private async Task<List<string>> DiscoverPersistenceSymbolsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        string symbol,
        CancellationToken cancellationToken)
    {
        var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, symbol))
        {
            var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
            foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeSymbols(
                         scanResult,
                         source,
                         static name => PackAnalysisHelpers.IsPersistenceLikeName(name)))
            {
                if (!string.Equals(referenced, symbol, StringComparison.OrdinalIgnoreCase))
                {
                    symbols.Add(referenced);
                }
            }
        }

        return symbols.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task AddSnippetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IReadOnlyList<PersistenceLink> links,
        ICollection<SnippetSelectionCandidate> snippetCandidates,
        CancellationToken cancellationToken)
    {
        foreach (var symbol in links
                     .Select(static link => link.PersistenceSymbol)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var filePath = PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, symbol).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(filePath) || !PackExtractionConventions.ShouldCreateSnippet(scanResult, filePath))
            {
                continue;
            }

            var className = PackExtractionConventions.GetSimpleTypeName(symbol);
            var snippet = await snippetFactory
                .CreateConstructorSnippetAsync(
                    workspaceRoot,
                    filePath,
                    className,
                    "persistence",
                    26,
                    false,
                    $"{className}(...)",
                    cancellationToken)
                .ConfigureAwait(false);
            if (snippet is not null)
            {
                snippetCandidates.Add(snippet);
            }
        }
    }

    private static void AddIfResolvable(
        WorkspaceScanResult scanResult,
        ISet<string> symbols,
        string targetTypeName)
    {
        var resolved = PackAnalysisHelpers.ResolveTypeSymbol(scanResult, targetTypeName);
        if (!string.IsNullOrWhiteSpace(resolved))
        {
            symbols.Add(resolved);
        }
    }

    private static string BuildSummary(IReadOnlyList<PersistenceLink> links)
    {
        var samples = links.Take(3).Select(static link => link.ToIndexLine()).ToArray();
        return $"Discovered {links.Count} persistence links. Examples: {string.Join(" / ", samples)}";
    }

    private async Task<string> ReadSourceAsync(
        string workspaceRoot,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
    }

    private sealed record PersistenceLink(
        string RootSymbol,
        string OwnerSymbol,
        string PersistenceSymbol,
        IReadOnlyList<string> FilePaths)
    {
        internal string Key => $"{RootSymbol}|{OwnerSymbol}|{PersistenceSymbol}";

        internal string RootDisplayName => PackExtractionConventions.GetSimpleTypeName(RootSymbol);

        internal string OwnerDisplayName => PackExtractionConventions.GetSimpleTypeName(OwnerSymbol);

        internal string PersistenceDisplayName => PackExtractionConventions.GetSimpleTypeName(PersistenceSymbol);

        internal IReadOnlyList<string> RelatedSymbols => [RootSymbol, OwnerSymbol, PersistenceSymbol];

        internal string ToIndexLine() =>
            string.Equals(RootSymbol, OwnerSymbol, StringComparison.OrdinalIgnoreCase)
                ? $"{RootDisplayName} -> {PersistenceDisplayName}"
                : $"{RootDisplayName} -> {OwnerDisplayName} -> {PersistenceDisplayName}";
    }
}
