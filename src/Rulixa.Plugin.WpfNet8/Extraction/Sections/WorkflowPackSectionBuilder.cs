using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class WorkflowPackSectionBuilder
{
    private const int MaxWorkflowLines = 6;
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
        ICollection<PackDecisionTrace> decisionTraces,
        ICollection<Diagnostic> unknowns,
        CancellationToken cancellationToken)
    {
        var discovery = await DiscoverCandidatesAsync(
                workspaceRoot,
                scanResult,
                resolvedEntry,
                relevantContext,
                cancellationToken)
            .ConfigureAwait(false);
        var analyses = AnalyzeCandidates(scanResult, relevantContext, discovery.Candidates);
        var compression = CompressAnalyses(analyses);
        AddDecisionTraces(analyses, compression.DecisionKinds, decisionTraces);

        var selected = compression.Selected.ToArray();

        if (selected.Length == 0)
        {
            AddUnknowns(scanResult, relevantContext, discovery, analyses, unknowns, decisionTraces);
            return;
        }

        indexes.Add(new IndexSection("Workflow", selected.Select(static analysis => analysis.ToIndexLine()).ToArray()));
        contracts.Add(new Contract(
            ContractKind.Command,
            "Workflow",
            BuildSummary(selected),
            selected.SelectMany(static analysis => analysis.Candidate.FilePaths).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            selected.SelectMany(static analysis => analysis.Candidate.RelatedSymbols).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));

        foreach (var analysis in selected)
        {
            foreach (var filePath in analysis.Candidate.FilePaths)
            {
                fileCandidates.Add(new FileSelectionCandidate(filePath, "workflow", analysis.Evaluation.ToPriority(24), false));
            }
        }

        AddUnknowns(scanResult, relevantContext, discovery, analyses, unknowns, decisionTraces);
        await AddSnippetsAsync(workspaceRoot, scanResult, relevantContext, selected, snippetCandidates, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<WorkflowDiscoveryResult> DiscoverCandidatesAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        RelevantPackContext relevantContext,
        CancellationToken cancellationToken)
    {
        var candidates = new List<WorkflowCandidate>();
        var ambiguousTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var missingDownstreamRoutes = new List<MissingDownstreamRoute>();
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

        foreach (var rootSymbol in relevantContext.ViewModelSymbols.OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase))
        {
            var firstHopCandidates = await DiscoverFirstHopCandidatesAsync(
                    workspaceRoot,
                    scanResult,
                    relevantContext,
                    rootSymbol,
                    directTargets,
                    cancellationToken)
                .ConfigureAwait(false);
            ambiguousTargets.UnionWith(firstHopCandidates.AmbiguousCandidates);

            foreach (var firstHopCandidate in firstHopCandidates.Candidates
                         .OrderBy(static candidate => candidate.Symbol, StringComparer.OrdinalIgnoreCase))
            {
                var secondHopDiscovery = await DiscoverSecondHopTargetsAsync(
                        workspaceRoot,
                        scanResult,
                        relevantContext,
                        firstHopCandidate.Symbol,
                        cancellationToken)
                    .ConfigureAwait(false);
                ambiguousTargets.UnionWith(secondHopDiscovery.AmbiguousCandidates);
                if (secondHopDiscovery.Symbols.Count == 0)
                {
                    missingDownstreamRoutes.Add(new MissingDownstreamRoute(
                        rootSymbol,
                        firstHopCandidate.Symbol,
                        firstHopCandidate.IsConstructorMatch,
                        firstHopCandidate.IsDirectCallMatch,
                        secondHopDiscovery.GuidanceCandidates));
                }

                var filePaths = PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, rootSymbol)
                    .Concat(PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, firstHopCandidate.Symbol))
                    .Concat(secondHopDiscovery.Symbols.SelectMany(symbol => PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, symbol)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                candidates.Add(new WorkflowCandidate(
                    RootSymbol: rootSymbol,
                    FirstHopSymbol: firstHopCandidate.Symbol,
                    NextSymbols: secondHopDiscovery.Symbols,
                    FilePaths: filePaths,
                    IsConstructorMatch: firstHopCandidate.IsConstructorMatch,
                    IsDirectCallMatch: firstHopCandidate.IsDirectCallMatch,
                    HasAmbiguousTarget: secondHopDiscovery.AmbiguousCandidates.Count > 0));
            }
        }

        return new WorkflowDiscoveryResult(candidates, ambiguousTargets.ToArray(), missingDownstreamRoutes.ToArray());
    }

    private async Task<FirstHopDiscovery> DiscoverFirstHopCandidatesAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        string symbol,
        IReadOnlySet<string> directTargets,
        CancellationToken cancellationToken)
    {
        var candidates = new Dictionary<string, FirstHopCandidate>(StringComparer.OrdinalIgnoreCase);
        var ambiguousCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directTarget in directTargets)
        {
            candidates[directTarget] = new FirstHopCandidate(directTarget, false, true);
        }

        foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, symbol))
        {
            var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
            foreach (var dependency in PackAnalysisHelpers.ExtractConstructorDependencyTypeNames(source, PackExtractionConventions.GetSimpleTypeName(symbol)))
            {
                var resolved = PackAnalysisHelpers.ResolveTypeSymbol(scanResult, dependency);
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    candidates[resolved] = candidates.TryGetValue(resolved, out var existing)
                        ? existing with { IsConstructorMatch = true }
                        : new FirstHopCandidate(resolved, true, false);
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
                         static name => PackAnalysisHelpers.IsWorkflowLikeName(name) || PackAnalysisHelpers.IsHubObjectLikeName(name)))
            {
                if (referenced.IsAmbiguous)
                {
                    foreach (var candidate in referenced.CandidateSymbols)
                    {
                        ambiguousCandidates.Add(candidate);
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(referenced.ResolvedSymbol)
                    || string.Equals(referenced.ResolvedSymbol, symbol, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                candidates[referenced.ResolvedSymbol] = candidates.TryGetValue(referenced.ResolvedSymbol, out var existing)
                    ? existing
                    : new FirstHopCandidate(referenced.ResolvedSymbol, false, false);
            }
        }

        return new FirstHopDiscovery(candidates.Values.ToArray(), ambiguousCandidates.ToArray());
    }

    private async Task<SecondHopDiscovery> DiscoverSecondHopTargetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        string symbol,
        CancellationToken cancellationToken)
    {
        var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ambiguousCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var guidanceCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var symbolsToInspect = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { symbol };

        foreach (var implementationSymbol in PackAnalysisHelpers.FindImplementationSymbolsForService(scanResult, symbol))
        {
            symbolsToInspect.Add(implementationSymbol);
            guidanceCandidates.Add(implementationSymbol);
        }

        foreach (var symbolToInspect in symbolsToInspect.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, symbolToInspect))
            {
                var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
                foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeCandidates(
                             scanResult,
                             source,
                             static name =>
                                 PackAnalysisHelpers.IsPersistenceLikeName(name)
                                 || PackAnalysisHelpers.IsHubObjectLikeName(name)
                                 || PackAnalysisHelpers.IsAlgorithmLikeName(name)
                                 || PackAnalysisHelpers.IsAnalyzerLikeName(name)))
                {
                    if (referenced.IsAmbiguous)
                    {
                        foreach (var candidate in referenced.CandidateSymbols)
                        {
                            ambiguousCandidates.Add(candidate);
                            guidanceCandidates.Add(candidate);
                        }

                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(referenced.ResolvedSymbol)
                        && !string.Equals(referenced.ResolvedSymbol, symbolToInspect, StringComparison.OrdinalIgnoreCase))
                    {
                        symbols.Add(referenced.ResolvedSymbol);
                        guidanceCandidates.Add(referenced.ResolvedSymbol);
                    }

                    foreach (var candidate in referenced.CandidateSymbols)
                    {
                        guidanceCandidates.Add(candidate);
                    }
                }
            }
        }

        return new SecondHopDiscovery(
            symbols.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
            ambiguousCandidates.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
            guidanceCandidates.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private IReadOnlyList<WorkflowAnalysis> AnalyzeCandidates(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IReadOnlyList<WorkflowCandidate> candidates)
    {
        var analyses = candidates
            .Select(candidate => new WorkflowAnalysis(
                candidate,
                HighSignalSelectionSupport.Evaluate(
                    relevantContext.GoalProfile,
                    BuildTextEvidence(candidate),
                    BuildEvidence(scanResult, relevantContext, candidate))))
            .OrderByDescending(static analysis => analysis.Evaluation.Score)
            .ThenBy(static analysis => analysis.Candidate.FirstHopDisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        for (var index = 0; index < analyses.Length; index++)
        {
            analyses[index] = analyses[index] with { Rank = index + 1, CandidateCount = analyses.Length };
        }

        return analyses;
    }

    private static IReadOnlyList<SectionTextEvidence> BuildTextEvidence(WorkflowCandidate candidate) =>
    [
        new("root-symbol", [candidate.RootSymbol]),
        new("first-hop", [candidate.FirstHopSymbol]),
        new("downstream", candidate.NextSymbols)
    ];

    private static SectionSignalEvidence BuildEvidence(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        WorkflowCandidate candidate)
    {
        var involvedSymbols = candidate.RelatedSymbols;
        var goalCategoryMatches = 0;
        if ((relevantContext.GoalProfile.HasCategory("system") || relevantContext.GoalProfile.HasCategory("project"))
            && candidate.NextSymbols.Any(static symbol =>
                PackAnalysisHelpers.IsPersistenceLikeName(PackExtractionConventions.GetSimpleTypeName(symbol))
                || PackAnalysisHelpers.IsHubObjectLikeName(PackExtractionConventions.GetSimpleTypeName(symbol))))
        {
            goalCategoryMatches++;
        }

        if (relevantContext.GoalProfile.HasCategory("drafting")
            && involvedSymbols.Any(static symbol =>
                PackAnalysisHelpers.IsAlgorithmLikeName(PackExtractionConventions.GetSimpleTypeName(symbol))
                || PackAnalysisHelpers.IsAnalyzerLikeName(PackExtractionConventions.GetSimpleTypeName(symbol))
                || PackExtractionConventions.GetSimpleTypeName(symbol).Contains("Port", StringComparison.Ordinal)
                || PackExtractionConventions.GetSimpleTypeName(symbol).Contains("Adapter", StringComparison.Ordinal)))
        {
            goalCategoryMatches++;
        }

        if (relevantContext.GoalProfile.HasCategory("drafting")
            && (PackExtractionConventions.GetSimpleTypeName(candidate.FirstHopSymbol).EndsWith("Port", StringComparison.Ordinal)
                || PackExtractionConventions.GetSimpleTypeName(candidate.FirstHopSymbol).EndsWith("Adapter", StringComparison.Ordinal)
                || PackExtractionConventions.GetSimpleTypeName(candidate.FirstHopSymbol).EndsWith("Workflow", StringComparison.Ordinal))
            && candidate.NextSymbols.Any(static symbol =>
                PackAnalysisHelpers.IsAlgorithmLikeName(PackExtractionConventions.GetSimpleTypeName(symbol))
                || PackAnalysisHelpers.IsAnalyzerLikeName(PackExtractionConventions.GetSimpleTypeName(symbol))))
        {
            goalCategoryMatches += 2;
        }

        return new SectionSignalEvidence(
            ConstructorDependencyMatches: candidate.IsConstructorMatch ? 1 : 0,
            DirectMethodCallMatches: candidate.IsDirectCallMatch ? 1 : 0,
            ServiceRegistrationMatches: PackAnalysisHelpers.CountServiceRegistrationMatches(scanResult, involvedSymbols),
            PartialSymbolMatches: involvedSymbols.Count(symbol =>
                PackAnalysisHelpers.ResolveAggregate(scanResult, relevantContext, symbol)?.FilePaths.Count > 1),
            FileKindMatches: PackAnalysisHelpers.CountFileKindMatches(scanResult, candidate.FilePaths, ScanFileKind.Service, ScanFileKind.ViewModel),
            GoalCategoryMatches: goalCategoryMatches,
            SemanticSignalCount: candidate.NextSymbols.Count,
            DownstreamCount: candidate.NextSymbols.Count,
            HasAmbiguousCandidates: candidate.HasAmbiguousTarget);
    }

    private static void AddDecisionTraces(
        IReadOnlyList<WorkflowAnalysis> analyses,
        IReadOnlyDictionary<string, string> decisionKinds,
        ICollection<PackDecisionTrace> decisionTraces)
    {
        foreach (var analysis in analyses)
        {
            var decisionKind = decisionKinds.TryGetValue(analysis.Candidate.Key, out var resolvedDecision)
                ? resolvedDecision
                : analysis.Evaluation.Evidence.HasAmbiguousCandidates
                    ? "omitted-ambiguous"
                    : "omitted-low-score";
            var summary = $"{analysis.Evaluation.ConfidenceLabel}: {analysis.Candidate.Route}";
            decisionTraces.Add(HighSignalSelectionSupport.BuildDecisionTrace(
                "workflow-selection",
                analysis.Candidate.Key,
                decisionKind,
                summary,
                analysis.Evaluation,
                analysis.Rank,
                analysis.CandidateCount));
        }
    }

    private static SectionCompressionResult<WorkflowAnalysis> CompressAnalyses(IReadOnlyList<WorkflowAnalysis> analyses)
    {
        var candidates = analyses
            .Select(analysis => new SectionCompressionCandidate<WorkflowAnalysis>(
                analysis,
                analysis.Candidate.Key,
                BuildCanonicalKey(analysis.Candidate),
                analysis.Evaluation,
                IsUiBoundaryCandidate(analysis.Candidate),
                IsSelfLoopCandidate(analysis.Candidate),
                analysis.Candidate.NextSymbols.Count == 0))
            .ToArray();
        return SectionCompressionSupport.Compress(candidates, MaxWorkflowLines);
    }

    private static void AddUnknowns(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        WorkflowDiscoveryResult discovery,
        IReadOnlyList<WorkflowAnalysis> analyses,
        ICollection<Diagnostic> unknowns,
        ICollection<PackDecisionTrace> decisionTraces)
    {
        if (discovery.AmbiguousTargets.Count > 0)
        {
            var diagnostic = HighSignalSelectionSupport.BuildGuidedDiagnostic(
                "workflow.ambiguous-target",
                BuildKnownRange(
                    discovery.MissingDownstreamRoutes.Select(static route => route.FirstHopSymbol),
                    fallback: discovery.AmbiguousTargets),
                "関連する複数候補に分かれたため、一意の型として確定できませんでした",
                null,
                DiagnosticSeverity.Info,
                discovery.AmbiguousTargets);
            unknowns.Add(diagnostic);
            decisionTraces.Add(HighSignalSelectionSupport.BuildGuidedUnknownTrace(
                "workflow-selection",
                "workflow.ambiguous-target",
                $"{diagnostic.Message} 次に見る候補: {FormatCandidates(diagnostic.Candidates)}",
                relevantContext.GoalProfile,
                analyses.Count));
        }

        if (discovery.MissingDownstreamRoutes.Count > 0)
        {
            var supplementalCandidates = relevantContext.GoalProfile.HasCategory("drafting")
                ? scanResult.Symbols
                    .Where(static symbol => symbol.Kind is SymbolKind.Class or SymbolKind.Interface)
                    .Select(static symbol => symbol.QualifiedName)
                    .Where(static symbol =>
                    {
                        var simpleName = PackExtractionConventions.GetSimpleTypeName(symbol);
                        return PackAnalysisHelpers.IsAlgorithmLikeName(simpleName) || PackAnalysisHelpers.IsAnalyzerLikeName(simpleName);
                    })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
                : Array.Empty<string>();
            var rankedCandidates = HighSignalSelectionSupport.RankGuidanceCandidates(
                scanResult,
                relevantContext.GoalProfile,
                discovery.MissingDownstreamRoutes.SelectMany(route =>
                {
                    var fromPortLikeOwner =
                        PackExtractionConventions.GetSimpleTypeName(route.FirstHopSymbol).EndsWith("Port", StringComparison.Ordinal)
                        || PackExtractionConventions.GetSimpleTypeName(route.FirstHopSymbol).EndsWith("Adapter", StringComparison.Ordinal)
                        || PackExtractionConventions.GetSimpleTypeName(route.FirstHopSymbol).EndsWith("Workflow", StringComparison.Ordinal);
                    var routeCandidates = route.GuidanceCandidates.Select(candidate => new GuidanceCandidate(
                        candidate,
                        route.IsDirectCallMatch,
                        route.IsConstructorMatch,
                        PackAnalysisHelpers.FindImplementationSymbolsForService(scanResult, route.FirstHopSymbol)
                            .Contains(candidate, StringComparer.OrdinalIgnoreCase),
                        fromPortLikeOwner));
                    var supplementalRouteCandidates = supplementalCandidates.Select(candidate => new GuidanceCandidate(
                        candidate,
                        FromPortLikeOwner: fromPortLikeOwner));
                    return routeCandidates.Concat(supplementalRouteCandidates);
                }));
            var diagnostic = HighSignalSelectionSupport.BuildGuidedDiagnostic(
                "workflow.missing-downstream",
                BuildKnownRange(
                    discovery.MissingDownstreamRoutes.Select(static route => route.FirstHopSymbol),
                    fallback: Array.Empty<string>()),
                "persistence / hub-object / algorithm / analyzer に到達する経路を 2 hop 以内で確定できませんでした",
                null,
                DiagnosticSeverity.Info,
                rankedCandidates);
            unknowns.Add(diagnostic);
            decisionTraces.Add(HighSignalSelectionSupport.BuildGuidedUnknownTrace(
                "workflow-selection",
                "workflow.missing-downstream",
                $"{diagnostic.Message} 次に見る候補: {FormatCandidates(diagnostic.Candidates)}",
                relevantContext.GoalProfile,
                analyses.Count));
        }
    }

    private async Task AddSnippetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IReadOnlyList<WorkflowAnalysis> analyses,
        ICollection<SnippetSelectionCandidate> snippetCandidates,
        CancellationToken cancellationToken)
    {
        foreach (var analysis in analyses)
        {
            var filePath = PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, analysis.Candidate.FirstHopSymbol).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(filePath) || !PackExtractionConventions.ShouldCreateSnippet(scanResult, filePath))
            {
                continue;
            }

            var className = PackExtractionConventions.GetSimpleTypeName(analysis.Candidate.FirstHopSymbol);
            var snippet = await snippetFactory
                .CreateConstructorSnippetAsync(
                    workspaceRoot,
                    filePath,
                    className,
                    "workflow",
                    analysis.Evaluation.ToPriority(24),
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

    private static string BuildSummary(IReadOnlyList<WorkflowAnalysis> analyses)
    {
        var downstreamFamilies = analyses
            .Select(static analysis => PackAnalysisHelpers.ClassifyDownstreamFamily(analysis.Candidate.NextSymbols))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static family => family, StringComparer.Ordinal)
            .ToArray();
        return $"この画面は {analyses.Count} 本の代表チェーンで処理を流し、{string.Join(" / ", downstreamFamilies)} へ接続します。";
    }

    private static string BuildCanonicalKey(WorkflowCandidate candidate) =>
        string.Join(
            "|",
            candidate.RootSymbol,
            PackAnalysisHelpers.ClassifyWorkflowFamily(candidate.FirstHopSymbol),
            PackAnalysisHelpers.ClassifyDownstreamFamily(candidate.NextSymbols));

    private static bool IsUiBoundaryCandidate(WorkflowCandidate candidate)
    {
        if (PackAnalysisHelpers.IsUiBoundaryLikeName(PackExtractionConventions.GetSimpleTypeName(candidate.FirstHopSymbol)))
        {
            return true;
        }

        return candidate.NextSymbols.All(symbol =>
            PackAnalysisHelpers.IsUiBoundaryLikeName(PackExtractionConventions.GetSimpleTypeName(symbol)));
    }

    private static bool IsSelfLoopCandidate(WorkflowCandidate candidate) =>
        PackAnalysisHelpers.HasSameTypeIdentity(candidate.RootSymbol, candidate.FirstHopSymbol)
        || candidate.NextSymbols.Any(symbol => PackAnalysisHelpers.HasSameTypeIdentity(candidate.FirstHopSymbol, symbol));

    private static string? TryExtractTypeSymbol(DirectCommandImpact impact)
    {
        var display = impact.DisplaySymbol.Replace("(...)", string.Empty, StringComparison.Ordinal);
        var lastDot = display.LastIndexOf('.');
        return lastDot <= 0 ? null : display[..lastDot];
    }

    private async Task<string> ReadSourceAsync(
        string workspaceRoot,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
    }

    private sealed record FirstHopCandidate(
        string Symbol,
        bool IsConstructorMatch,
        bool IsDirectCallMatch);

    private sealed record FirstHopDiscovery(
        IReadOnlyList<FirstHopCandidate> Candidates,
        IReadOnlyList<string> AmbiguousCandidates);

    private sealed record SecondHopDiscovery(
        IReadOnlyList<string> Symbols,
        IReadOnlyList<string> AmbiguousCandidates,
        IReadOnlyList<string> GuidanceCandidates);

    private sealed record WorkflowDiscoveryResult(
        IReadOnlyList<WorkflowCandidate> Candidates,
        IReadOnlyList<string> AmbiguousTargets,
        IReadOnlyList<MissingDownstreamRoute> MissingDownstreamRoutes);

    private sealed record MissingDownstreamRoute(
        string RootSymbol,
        string FirstHopSymbol,
        bool IsConstructorMatch,
        bool IsDirectCallMatch,
        IReadOnlyList<string> GuidanceCandidates);

    private sealed record WorkflowCandidate(
        string RootSymbol,
        string FirstHopSymbol,
        IReadOnlyList<string> NextSymbols,
        IReadOnlyList<string> FilePaths,
        bool IsConstructorMatch,
        bool IsDirectCallMatch,
        bool HasAmbiguousTarget)
    {
        internal string Key => $"{RootSymbol}|{FirstHopSymbol}|{string.Join("|", NextSymbols)}";

        internal string RootDisplayName => PackExtractionConventions.GetSimpleTypeName(RootSymbol);

        internal string FirstHopDisplayName => PackExtractionConventions.GetSimpleTypeName(FirstHopSymbol);

        internal IReadOnlyList<string> RelatedSymbols => [RootSymbol, FirstHopSymbol, .. NextSymbols];

        internal string Route =>
            NextSymbols.Count == 0
                ? $"{RootDisplayName} -> {FirstHopDisplayName}"
                : $"{RootDisplayName} -> {FirstHopDisplayName} -> {string.Join(" / ", NextSymbols.Select(PackExtractionConventions.GetSimpleTypeName))}";
    }

    private sealed record WorkflowAnalysis(
        WorkflowCandidate Candidate,
        SectionSelectionEvaluation Evaluation)
    {
        internal int Rank { get; init; }

        internal int CandidateCount { get; init; }

        internal string ToIndexLine() => $"{Evaluation.ConfidenceLabel}: {Candidate.Route}";
    }

    private static string BuildKnownRange(IEnumerable<string> knownSymbols, IEnumerable<string> fallback)
    {
        var targets = knownSymbols
            .Where(static symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(PackExtractionConventions.GetSimpleTypeName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();
        if (targets.Length == 0)
        {
            targets = fallback
                .Where(static symbol => !string.IsNullOrWhiteSpace(symbol))
                .Select(PackExtractionConventions.GetSimpleTypeName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToArray();
        }

        return targets.Length == 0 ? "入口シンボルまで" : string.Join(" / ", targets);
    }

    private static string FormatCandidates(IReadOnlyList<string> candidates) =>
        candidates.Count == 0
            ? "なし"
            : string.Join(", ", candidates.Select(PackExtractionConventions.GetSimpleTypeName));
}
