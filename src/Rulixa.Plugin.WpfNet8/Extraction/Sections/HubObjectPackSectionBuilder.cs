using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class HubObjectPackSectionBuilder
{
    private readonly IWorkspaceFileSystem workspaceFileSystem;
    private readonly CSharpSnippetCandidateFactory snippetFactory;

    internal HubObjectPackSectionBuilder(
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
        var hubObjects = await DiscoverHubObjectsAsync(workspaceRoot, scanResult, relevantContext, cancellationToken)
            .ConfigureAwait(false);
        if (hubObjects.Count == 0)
        {
            if (relevantContext.GoalProfile.HasCategory("system") || relevantContext.GoalProfile.HasCategory("project"))
            {
                unknowns.Add(new Diagnostic(
                    "hub-object.unresolved",
                    "Shared state holder candidates were not identified from the current entry.",
                    null,
                    DiagnosticSeverity.Info,
                    relevantContext.ViewModelSymbols.ToArray()));
            }

            return;
        }

        indexes.Add(new IndexSection("Hub Objects", hubObjects.Select(static item => item.ToIndexLine()).ToArray()));
        contracts.Add(new Contract(
            ContractKind.DependencyInjection,
            "Hub Objects",
            BuildSummary(hubObjects),
            hubObjects.SelectMany(static item => item.FilePaths).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            hubObjects.Select(static item => item.Symbol).ToArray()));

        foreach (var filePath in hubObjects.SelectMany(static item => item.FilePaths).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            fileCandidates.Add(new FileSelectionCandidate(filePath, "hub-object", 28, false));
        }

        await AddSnippetsAsync(workspaceRoot, scanResult, relevantContext, hubObjects, snippetCandidates, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<HubObjectCandidate>> DiscoverHubObjectsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        CancellationToken cancellationToken)
    {
        var candidates = new Dictionary<string, HubObjectCandidate>(StringComparer.OrdinalIgnoreCase);
        var symbolsToInspect = new HashSet<string>(relevantContext.ViewModelSymbols, StringComparer.OrdinalIgnoreCase);

        foreach (var viewModelSymbol in relevantContext.ViewModelSymbols)
        {
            foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, viewModelSymbol))
            {
                var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
                foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeSymbols(
                             scanResult,
                             source,
                             static name => PackAnalysisHelpers.IsWorkflowLikeName(name)
                                 || PackAnalysisHelpers.IsPersistenceLikeName(name)
                                 || PackAnalysisHelpers.IsHubObjectLikeName(name)))
                {
                    symbolsToInspect.Add(referenced);
                }
            }
        }

        foreach (var symbol in symbolsToInspect)
        {
            foreach (var referenced in await DiscoverHubSymbolsFromOwnerAsync(
                         workspaceRoot,
                         scanResult,
                         relevantContext,
                         symbol,
                         cancellationToken))
            {
                var hubFiles = PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, referenced);
                if (hubFiles.Count == 0)
                {
                    continue;
                }

                var firstFile = hubFiles[0];
                var hubSource = await ReadSourceAsync(workspaceRoot, firstFile, cancellationToken).ConfigureAwait(false);
                if (!PackAnalysisHelpers.HasHubObjectSignals(hubSource))
                {
                    continue;
                }

                var signals = ExtractSignals(hubSource);
                candidates[referenced] = new HubObjectCandidate(referenced, signals, hubFiles);
            }
        }

        return candidates.Values
            .OrderBy(static item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<IReadOnlyList<string>> DiscoverHubSymbolsFromOwnerAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        string ownerSymbol,
        CancellationToken cancellationToken)
    {
        var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, ownerSymbol))
        {
            var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
            foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeSymbols(
                         scanResult,
                         source,
                         static name => PackAnalysisHelpers.IsHubObjectLikeName(name)))
            {
                symbols.Add(referenced);
            }
        }

        return symbols.OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private async Task AddSnippetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IReadOnlyList<HubObjectCandidate> hubObjects,
        ICollection<SnippetSelectionCandidate> snippetCandidates,
        CancellationToken cancellationToken)
    {
        foreach (var hubObject in hubObjects)
        {
            var filePath = PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, hubObject.Symbol).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(filePath) || !PackExtractionConventions.ShouldCreateSnippet(scanResult, filePath))
            {
                continue;
            }

            var className = PackExtractionConventions.GetSimpleTypeName(hubObject.Symbol);
            var snippet = await snippetFactory
                .CreateConstructorSnippetAsync(
                    workspaceRoot,
                    filePath,
                    className,
                    "hub-object",
                    28,
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

    private static IReadOnlyList<string> ExtractSignals(string source)
    {
        var signals = new List<string>();
        if (source.Contains("IsDirty", StringComparison.Ordinal))
        {
            signals.Add("IsDirty");
        }

        if (source.Contains("CreateSnapshot", StringComparison.Ordinal))
        {
            signals.Add("CreateSnapshot");
        }

        if (source.Contains("RestoreFromSnapshot", StringComparison.Ordinal))
        {
            signals.Add("RestoreFromSnapshot");
        }

        if (source.Contains("MarkDirty", StringComparison.Ordinal))
        {
            signals.Add("MarkDirty");
        }

        if (source.Contains("MarkSaved", StringComparison.Ordinal))
        {
            signals.Add("MarkSaved");
        }

        return signals;
    }

    private static string BuildSummary(IReadOnlyList<HubObjectCandidate> hubObjects)
    {
        var samples = hubObjects.Take(3).Select(static item => item.ToIndexLine()).ToArray();
        return $"Discovered {hubObjects.Count} shared state holder candidates. Examples: {string.Join(" / ", samples)}";
    }

    private async Task<string> ReadSourceAsync(
        string workspaceRoot,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
    }

    private sealed record HubObjectCandidate(
        string Symbol,
        IReadOnlyList<string> Signals,
        IReadOnlyList<string> FilePaths)
    {
        internal string DisplayName => PackExtractionConventions.GetSimpleTypeName(Symbol);

        internal string ToIndexLine() =>
            Signals.Count == 0
                ? DisplayName
                : $"{DisplayName} ({string.Join(", ", Signals)})";
    }
}
