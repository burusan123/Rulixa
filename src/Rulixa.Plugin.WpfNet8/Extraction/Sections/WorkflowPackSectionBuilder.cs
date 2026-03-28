using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class WorkflowPackSectionBuilder
{
    private readonly IWorkspaceFileSystem workspaceFileSystem;
    private readonly CSharpSnippetCandidateFactory snippetFactory;

    internal WorkflowPackSectionBuilder(
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
        ICollection<Diagnostic> unknowns,
        CancellationToken cancellationToken)
    {
        var chains = await DiscoverChainsAsync(
                workspaceRoot,
                scanResult,
                resolvedEntry,
                relevantContext,
                cancellationToken)
            .ConfigureAwait(false);
        if (chains.Count == 0)
        {
            if (ShouldReportUnknown(relevantContext))
            {
                unknowns.Add(new Diagnostic(
                    "workflow.unresolved",
                    "Workflow chain could not be expanded within two hops.",
                    resolvedEntry.ResolvedPath,
                    DiagnosticSeverity.Info,
                    relevantContext.ViewModelSymbols.ToArray()));
            }

            return;
        }

        indexes.Add(new IndexSection("Workflow", chains.Select(static chain => chain.ToIndexLine()).ToArray()));
        contracts.Add(new Contract(
            ContractKind.Command,
            "Workflow",
            BuildSummary(chains),
            chains.SelectMany(static chain => chain.FilePaths).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            chains.SelectMany(static chain => chain.RelatedSymbols).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));

        foreach (var filePath in chains.SelectMany(static chain => chain.FilePaths).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            fileCandidates.Add(new FileSelectionCandidate(filePath, "workflow", 24, false));
        }

        await AddSnippetsAsync(workspaceRoot, scanResult, relevantContext, chains, snippetCandidates, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<WorkflowChain>> DiscoverChainsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        RelevantPackContext relevantContext,
        CancellationToken cancellationToken)
    {
        var commandDetails = await new CommandImpactAnalyzer(workspaceFileSystem)
            .AnalyzeAsync(
                workspaceRoot,
                scanResult,
                CommandPackSectionBuilder.FindCommands(scanResult, resolvedEntry),
                cancellationToken)
            .ConfigureAwait(false);
        var directTargets = commandDetails
            .SelectMany(static details => details.DirectImpacts)
            .Select(TryExtractTypeSymbol)
            .Where(static symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(static symbol => symbol!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var chains = new List<WorkflowChain>();

        foreach (var rootSymbol in relevantContext.ViewModelSymbols.OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase))
        {
            var firstHopSymbols = await DiscoverWorkflowTargetsAsync(
                    workspaceRoot,
                    scanResult,
                    relevantContext,
                    rootSymbol,
                    cancellationToken)
                .ConfigureAwait(false);
            foreach (var directTarget in directTargets)
            {
                firstHopSymbols.Add(directTarget);
            }

            foreach (var firstHopSymbol in firstHopSymbols.OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase))
            {
                var nextSymbols = await DiscoverSecondHopTargetsAsync(
                        workspaceRoot,
                        scanResult,
                        relevantContext,
                        firstHopSymbol,
                        cancellationToken)
                    .ConfigureAwait(false);
                var filePaths = PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, rootSymbol)
                    .Concat(PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, firstHopSymbol))
                    .Concat(nextSymbols.SelectMany(symbol => PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, symbol)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                chains.Add(new WorkflowChain(rootSymbol, firstHopSymbol, nextSymbols, filePaths));
            }
        }

        return chains
            .DistinctBy(static chain => chain.Key, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static chain => chain.RootDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static chain => chain.FirstHopDisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<HashSet<string>> DiscoverWorkflowTargetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        string symbol,
        CancellationToken cancellationToken)
    {
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, symbol))
        {
            var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
            foreach (var dependency in PackAnalysisHelpers.ExtractConstructorDependencyTypeNames(source, PackExtractionConventions.GetSimpleTypeName(symbol)))
            {
                AddIfWorkflowLike(scanResult, targets, dependency);
            }

            foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeSymbols(
                         scanResult,
                         source,
                         static name => PackAnalysisHelpers.IsWorkflowLikeName(name) || PackAnalysisHelpers.IsHubObjectLikeName(name)))
            {
                if (!string.Equals(referenced, symbol, StringComparison.OrdinalIgnoreCase))
                {
                    targets.Add(referenced);
                }
            }
        }

        return targets;
    }

    private async Task<IReadOnlyList<string>> DiscoverSecondHopTargetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        string symbol,
        CancellationToken cancellationToken)
    {
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, symbol))
        {
            var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
            foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeSymbols(
                         scanResult,
                         source,
                         static name =>
                             PackAnalysisHelpers.IsPersistenceLikeName(name)
                             || PackAnalysisHelpers.IsHubObjectLikeName(name)
                             || name.EndsWith("AlgorithmRunner", StringComparison.Ordinal)
                             || name.EndsWith("Algorithm", StringComparison.Ordinal)
                             || name.EndsWith("Analyzer", StringComparison.Ordinal)))
            {
                if (!string.Equals(referenced, symbol, StringComparison.OrdinalIgnoreCase))
                {
                    targets.Add(referenced);
                }
            }
        }

        return targets.OrderBy(static target => target, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private async Task AddSnippetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IReadOnlyList<WorkflowChain> chains,
        ICollection<SnippetSelectionCandidate> snippetCandidates,
        CancellationToken cancellationToken)
    {
        foreach (var symbol in chains
                     .Select(static chain => chain.FirstHopSymbol)
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
                    "workflow",
                    24,
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

    private static bool ShouldReportUnknown(RelevantPackContext relevantContext) =>
        relevantContext.GoalProfile.HasCategory("system")
        || relevantContext.GoalProfile.HasCategory("project")
        || relevantContext.GoalProfile.HasCategory("drafting");

    private static string BuildSummary(IReadOnlyList<WorkflowChain> chains)
    {
        var samples = chains
            .Take(3)
            .Select(static chain => chain.ToIndexLine())
            .ToArray();
        return $"Discovered {chains.Count} workflow chains. Examples: {string.Join(" / ", samples)}";
    }

    private static string? TryExtractTypeSymbol(DirectCommandImpact impact)
    {
        var display = impact.DisplaySymbol.Replace("(...)","", StringComparison.Ordinal);
        var lastDot = display.LastIndexOf('.');
        return lastDot <= 0 ? null : display[..lastDot];
    }

    private static void AddIfWorkflowLike(
        WorkspaceScanResult scanResult,
        ISet<string> targets,
        string targetTypeName)
    {
        var resolved = PackAnalysisHelpers.ResolveTypeSymbol(scanResult, targetTypeName);
        if (string.IsNullOrWhiteSpace(resolved))
        {
            return;
        }

        var simpleName = PackExtractionConventions.GetSimpleTypeName(resolved);
        if (PackAnalysisHelpers.IsWorkflowLikeName(simpleName) || PackAnalysisHelpers.IsHubObjectLikeName(simpleName))
        {
            targets.Add(resolved);
        }
    }

    private async Task<string> ReadSourceAsync(
        string workspaceRoot,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
    }

    private sealed record WorkflowChain(
        string RootSymbol,
        string FirstHopSymbol,
        IReadOnlyList<string> NextSymbols,
        IReadOnlyList<string> FilePaths)
    {
        internal string Key => $"{RootSymbol}|{FirstHopSymbol}|{string.Join("|", NextSymbols)}";

        internal string RootDisplayName => PackExtractionConventions.GetSimpleTypeName(RootSymbol);

        internal string FirstHopDisplayName => PackExtractionConventions.GetSimpleTypeName(FirstHopSymbol);

        internal IReadOnlyList<string> RelatedSymbols =>
            [RootSymbol, FirstHopSymbol, .. NextSymbols];

        internal string ToIndexLine()
        {
            var route = $"{RootDisplayName} -> {FirstHopDisplayName}";
            if (NextSymbols.Count == 0)
            {
                return route;
            }

            var next = string.Join(" / ", NextSymbols.Select(PackExtractionConventions.GetSimpleTypeName));
            return $"{route} -> {next}";
        }
    }
}
