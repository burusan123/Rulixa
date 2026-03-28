using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class PersistencePackSectionBuilder
{
    private const int MaxPersistenceLines = 6;
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
        ICollection<PackDecisionTrace> decisionTraces,
        ICollection<Diagnostic> unknowns,
        CancellationToken cancellationToken)
    {
        var discovery = await DiscoverLinksAsync(workspaceRoot, scanResult, relevantContext, cancellationToken).ConfigureAwait(false);
        var analyses = AnalyzeLinks(scanResult, relevantContext, discovery.Links);
        var compression = CompressAnalyses(analyses);
        AddDecisionTraces(analyses, compression.DecisionKinds, decisionTraces);

        var selected = compression.Selected.ToArray();
        if (selected.Length == 0)
        {
            AddUnknowns(relevantContext, discovery, analyses, unknowns, decisionTraces);
            return;
        }

        indexes.Add(new IndexSection("Persistence", selected.Select(static analysis => analysis.ToIndexLine()).ToArray()));
        contracts.Add(new Contract(
            ContractKind.DependencyInjection,
            "Persistence",
            BuildSummary(selected),
            selected.SelectMany(static analysis => analysis.Link.FilePaths).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            selected.SelectMany(static analysis => analysis.Link.RelatedSymbols).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));

        foreach (var analysis in selected)
        {
            foreach (var filePath in analysis.Link.FilePaths)
            {
                fileCandidates.Add(new FileSelectionCandidate(filePath, "persistence", analysis.Evaluation.ToPriority(26), false));
            }
        }

        AddUnknowns(relevantContext, discovery, analyses, unknowns, decisionTraces);
        await AddSnippetsAsync(workspaceRoot, scanResult, relevantContext, selected, snippetCandidates, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<PersistenceDiscoveryResult> DiscoverLinksAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        CancellationToken cancellationToken)
    {
        var links = new List<PersistenceLink>();
        var ambiguousCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ownerCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rootSymbol in relevantContext.ViewModelSymbols.OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase))
        {
            var owners = await DiscoverOwnerSymbolsAsync(
                    workspaceRoot,
                    scanResult,
                    relevantContext,
                    rootSymbol,
                    cancellationToken)
                .ConfigureAwait(false);
            ownerCandidates.UnionWith(owners.Symbols);
            ambiguousCandidates.UnionWith(owners.AmbiguousCandidates);

            foreach (var ownerSymbol in owners.Symbols)
            {
                var persistenceDiscovery = await DiscoverPersistenceCandidatesAsync(
                        workspaceRoot,
                        scanResult,
                        relevantContext,
                        ownerSymbol,
                        cancellationToken)
                    .ConfigureAwait(false);
                ambiguousCandidates.UnionWith(persistenceDiscovery.AmbiguousCandidates);

                if (persistenceDiscovery.Candidates.Count == 0
                    && PackAnalysisHelpers.IsPersistenceLikeName(PackExtractionConventions.GetSimpleTypeName(ownerSymbol))
                    && !string.Equals(rootSymbol, ownerSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    persistenceDiscovery = new PersistenceCandidateDiscovery(
                        [new PersistenceCandidate(ownerSymbol, false)],
                        persistenceDiscovery.AmbiguousCandidates);
                }

                foreach (var persistenceCandidate in persistenceDiscovery.Candidates)
                {
                    var filePaths = PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, rootSymbol)
                        .Concat(PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, ownerSymbol))
                        .Concat(PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, persistenceCandidate.Symbol))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    links.Add(new PersistenceLink(
                        rootSymbol,
                        ownerSymbol,
                        persistenceCandidate.Symbol,
                        filePaths,
                        persistenceCandidate.IsConstructorMatch));
                }
            }
        }

        return new PersistenceDiscoveryResult(
            links,
            ownerCandidates.ToArray(),
            ambiguousCandidates.ToArray());
    }

    private async Task<OwnerDiscovery> DiscoverOwnerSymbolsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        string rootSymbol,
        CancellationToken cancellationToken)
    {
        var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootSymbol };
        var ambiguousCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, rootSymbol))
        {
            var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
            foreach (var dependency in PackAnalysisHelpers.ExtractConstructorDependencyTypeNames(source, PackExtractionConventions.GetSimpleTypeName(rootSymbol)))
            {
                var resolved = PackAnalysisHelpers.ResolveTypeSymbol(scanResult, dependency);
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    symbols.Add(resolved);
                    continue;
                }

                foreach (var candidate in PackAnalysisHelpers.FindTypeSymbolsBySimpleName(scanResult, PackExtractionConventions.GetSimpleTypeName(dependency)))
                {
                    ambiguousCandidates.Add(candidate);
                }
            }

            foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeCandidates(
                         scanResult,
                         source,
                         static name => PackAnalysisHelpers.IsWorkflowLikeName(name) || PackAnalysisHelpers.IsPersistenceLikeName(name)))
            {
                if (referenced.IsAmbiguous)
                {
                    foreach (var candidate in referenced.CandidateSymbols)
                    {
                        ambiguousCandidates.Add(candidate);
                    }

                    continue;
                }

                if (!string.IsNullOrWhiteSpace(referenced.ResolvedSymbol))
                {
                    symbols.Add(referenced.ResolvedSymbol);
                }
            }
        }

        return new OwnerDiscovery(
            symbols.OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase).ToArray(),
            ambiguousCandidates.OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private async Task<PersistenceCandidateDiscovery> DiscoverPersistenceCandidatesAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        string ownerSymbol,
        CancellationToken cancellationToken)
    {
        var candidates = new Dictionary<string, PersistenceCandidate>(StringComparer.OrdinalIgnoreCase);
        var ambiguousCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, ownerSymbol))
        {
            var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
            foreach (var dependency in PackAnalysisHelpers.ExtractConstructorDependencyTypeNames(source, PackExtractionConventions.GetSimpleTypeName(ownerSymbol)))
            {
                if (!PackAnalysisHelpers.IsPersistenceLikeName(PackExtractionConventions.GetSimpleTypeName(dependency)))
                {
                    continue;
                }

                var resolved = PackAnalysisHelpers.ResolveTypeSymbol(scanResult, dependency);
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    candidates[resolved] = new PersistenceCandidate(resolved, true);
                    continue;
                }

                foreach (var candidate in PackAnalysisHelpers.FindTypeSymbolsBySimpleName(scanResult, PackExtractionConventions.GetSimpleTypeName(dependency)))
                {
                    ambiguousCandidates.Add(candidate);
                }
            }

            foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeCandidates(
                         scanResult,
                         source,
                         static name => PackAnalysisHelpers.IsPersistenceLikeName(name)))
            {
                if (referenced.IsAmbiguous)
                {
                    foreach (var candidate in referenced.CandidateSymbols)
                    {
                        ambiguousCandidates.Add(candidate);
                    }

                    continue;
                }

                if (!string.IsNullOrWhiteSpace(referenced.ResolvedSymbol)
                    && !string.Equals(referenced.ResolvedSymbol, ownerSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.TryAdd(referenced.ResolvedSymbol, new PersistenceCandidate(referenced.ResolvedSymbol, false));
                }
            }
        }

        return new PersistenceCandidateDiscovery(
            candidates.Values.OrderBy(static candidate => candidate.Symbol, StringComparer.OrdinalIgnoreCase).ToArray(),
            ambiguousCandidates.OrderBy(static candidate => candidate, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private IReadOnlyList<PersistenceAnalysis> AnalyzeLinks(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IReadOnlyList<PersistenceLink> links)
    {
        var analyses = links
            .Select(link => new PersistenceAnalysis(
                link,
                HighSignalSelectionSupport.Evaluate(
                    relevantContext.GoalProfile,
                    BuildTextEvidence(link),
                    BuildEvidence(scanResult, relevantContext, link))))
            .OrderByDescending(static analysis => analysis.Evaluation.Score)
            .ThenBy(static analysis => analysis.Link.PersistenceDisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        for (var index = 0; index < analyses.Length; index++)
        {
            analyses[index] = analyses[index] with { Rank = index + 1, CandidateCount = analyses.Length };
        }

        return analyses;
    }

    private static IReadOnlyList<SectionTextEvidence> BuildTextEvidence(PersistenceLink link) =>
    [
        new("root-symbol", [link.RootSymbol]),
        new("owner-symbol", [link.OwnerSymbol]),
        new("persistence-symbol", [link.PersistenceSymbol])
    ];

    private static SectionSignalEvidence BuildEvidence(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        PersistenceLink link)
    {
        var involvedSymbols = link.RelatedSymbols;
        var goalCategoryMatches =
            relevantContext.GoalProfile.HasCategory("project") || relevantContext.GoalProfile.HasCategory("system")
                ? 1
                : 0;

        return new SectionSignalEvidence(
            ConstructorDependencyMatches: link.IsConstructorMatch ? 1 : 0,
            ServiceRegistrationMatches: PackAnalysisHelpers.CountServiceRegistrationMatches(scanResult, involvedSymbols),
            PartialSymbolMatches: involvedSymbols.Count(symbol =>
                PackAnalysisHelpers.ResolveAggregate(scanResult, relevantContext, symbol)?.FilePaths.Count > 1),
            FileKindMatches: PackAnalysisHelpers.CountFileKindMatches(scanResult, link.FilePaths, ScanFileKind.Service, ScanFileKind.ViewModel),
            GoalCategoryMatches: goalCategoryMatches,
            SemanticSignalCount: 1,
            DownstreamCount: 1);
    }

    private static void AddDecisionTraces(
        IReadOnlyList<PersistenceAnalysis> analyses,
        IReadOnlyDictionary<string, string> decisionKinds,
        ICollection<PackDecisionTrace> decisionTraces)
    {
        foreach (var analysis in analyses)
        {
            var decisionKind = decisionKinds.TryGetValue(analysis.Link.Key, out var resolvedDecision)
                ? resolvedDecision
                : "omitted-low-score";
            decisionTraces.Add(HighSignalSelectionSupport.BuildDecisionTrace(
                "persistence-selection",
                analysis.Link.Key,
                decisionKind,
                $"{analysis.Evaluation.ConfidenceLabel}: {analysis.Link.Route}",
                analysis.Evaluation,
                analysis.Rank,
                analysis.CandidateCount));
        }
    }

    private static SectionCompressionResult<PersistenceAnalysis> CompressAnalyses(IReadOnlyList<PersistenceAnalysis> analyses)
    {
        var candidates = analyses
            .Select(analysis => new SectionCompressionCandidate<PersistenceAnalysis>(
                analysis,
                analysis.Link.Key,
                BuildCanonicalKey(analysis.Link),
                analysis.Evaluation,
                IsUiBoundaryCandidate(analysis.Link),
                IsSelfLoopCandidate(analysis.Link)))
            .ToArray();
        return SectionCompressionSupport.Compress(candidates, MaxPersistenceLines);
    }

    private static void AddUnknowns(
        RelevantPackContext relevantContext,
        PersistenceDiscoveryResult discovery,
        IReadOnlyList<PersistenceAnalysis> analyses,
        ICollection<Diagnostic> unknowns,
        ICollection<PackDecisionTrace> decisionTraces)
    {
        if (analyses.Any(static analysis => analysis.Evaluation.IsSelectable))
        {
            return;
        }

        if (discovery.OwnerCandidates.Count > 0 || discovery.AmbiguousCandidates.Count > 0)
        {
            var diagnostic = HighSignalSelectionSupport.BuildDiagnostic(
                "persistence.missing-owner",
                "Persistence-related symbols were seen, but no representative owner-to-persistence boundary survived compression with strong evidence.",
                null,
                DiagnosticSeverity.Info,
                discovery.AmbiguousCandidates.Concat(discovery.OwnerCandidates));
            unknowns.Add(diagnostic);
            decisionTraces.Add(HighSignalSelectionSupport.BuildDecisionTrace(
                "persistence-selection",
                "persistence.missing-owner",
                "unknown-raised",
                diagnostic.Message,
                new SectionSelectionEvaluation(0, SectionConfidence.Low, relevantContext.GoalProfile.Terms, [], [], new SectionSignalEvidence()),
                0,
                analyses.Count));
        }
    }

    private async Task AddSnippetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IReadOnlyList<PersistenceAnalysis> analyses,
        ICollection<SnippetSelectionCandidate> snippetCandidates,
        CancellationToken cancellationToken)
    {
        foreach (var analysis in analyses)
        {
            var filePath = PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, analysis.Link.PersistenceSymbol).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(filePath) || !PackExtractionConventions.ShouldCreateSnippet(scanResult, filePath))
            {
                continue;
            }

            var className = PackExtractionConventions.GetSimpleTypeName(analysis.Link.PersistenceSymbol);
            var snippet = await snippetFactory
                .CreateConstructorSnippetAsync(
                    workspaceRoot,
                    filePath,
                    className,
                    "persistence",
                    analysis.Evaluation.ToPriority(26),
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

    private static string BuildSummary(IReadOnlyList<PersistenceAnalysis> analyses)
    {
        var families = analyses
            .Select(static analysis => PackAnalysisHelpers.ClassifyPersistenceFamily(analysis.Link.PersistenceSymbol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static family => family, StringComparer.Ordinal)
            .ToArray();
        return $"This screen crosses {analyses.Count} representative persistence boundaries: {string.Join(", ", families)}.";
    }

    private static string BuildCanonicalKey(PersistenceLink link) =>
        string.Join(
            "|",
            link.RootSymbol,
            PackAnalysisHelpers.ClassifyWorkflowFamily(link.OwnerSymbol),
            PackAnalysisHelpers.ClassifyPersistenceFamily(link.PersistenceSymbol));

    private static bool IsUiBoundaryCandidate(PersistenceLink link) =>
        PackAnalysisHelpers.IsUiBoundaryLikeName(PackExtractionConventions.GetSimpleTypeName(link.OwnerSymbol))
        || PackAnalysisHelpers.IsUiBoundaryLikeName(PackExtractionConventions.GetSimpleTypeName(link.PersistenceSymbol));

    private static bool IsSelfLoopCandidate(PersistenceLink link) =>
        PackAnalysisHelpers.HasSameTypeIdentity(link.OwnerSymbol, link.PersistenceSymbol);

    private async Task<string> ReadSourceAsync(
        string workspaceRoot,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
    }

    private sealed record OwnerDiscovery(
        IReadOnlyList<string> Symbols,
        IReadOnlyList<string> AmbiguousCandidates);

    private sealed record PersistenceCandidate(
        string Symbol,
        bool IsConstructorMatch);

    private sealed record PersistenceCandidateDiscovery(
        IReadOnlyList<PersistenceCandidate> Candidates,
        IReadOnlyList<string> AmbiguousCandidates);

    private sealed record PersistenceDiscoveryResult(
        IReadOnlyList<PersistenceLink> Links,
        IReadOnlyList<string> OwnerCandidates,
        IReadOnlyList<string> AmbiguousCandidates);

    private sealed record PersistenceLink(
        string RootSymbol,
        string OwnerSymbol,
        string PersistenceSymbol,
        IReadOnlyList<string> FilePaths,
        bool IsConstructorMatch)
    {
        internal string Key => $"{RootSymbol}|{OwnerSymbol}|{PersistenceSymbol}";

        internal string RootDisplayName => PackExtractionConventions.GetSimpleTypeName(RootSymbol);

        internal string OwnerDisplayName => PackExtractionConventions.GetSimpleTypeName(OwnerSymbol);

        internal string PersistenceDisplayName => PackExtractionConventions.GetSimpleTypeName(PersistenceSymbol);

        internal IReadOnlyList<string> RelatedSymbols => [RootSymbol, OwnerSymbol, PersistenceSymbol];

        internal string Route =>
            string.Equals(RootSymbol, OwnerSymbol, StringComparison.OrdinalIgnoreCase)
                ? $"{RootDisplayName} -> {PersistenceDisplayName}"
                : $"{RootDisplayName} -> {OwnerDisplayName} -> {PersistenceDisplayName}";
    }

    private sealed record PersistenceAnalysis(
        PersistenceLink Link,
        SectionSelectionEvaluation Evaluation)
    {
        internal int Rank { get; init; }

        internal int CandidateCount { get; init; }

        internal string ToIndexLine() => $"{Evaluation.ConfidenceLabel}: {Link.Route}";
    }
}
