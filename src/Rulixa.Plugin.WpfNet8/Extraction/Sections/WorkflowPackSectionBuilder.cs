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
        AddDecisionTraces(analyses, decisionTraces);

        var selected = analyses
            .Where(static analysis => analysis.Evaluation.IsSelectable)
            .ToArray();

        if (selected.Length == 0)
        {
            AddUnknowns(relevantContext, discovery, analyses, unknowns, decisionTraces);
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

        AddUnknowns(relevantContext, discovery, analyses, unknowns, decisionTraces);
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
        var missingDownstreamCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                    missingDownstreamCandidates.Add(firstHopCandidate.Symbol);
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

        return new WorkflowDiscoveryResult(candidates, ambiguousTargets.ToArray(), missingDownstreamCandidates.ToArray());
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

        foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, symbol))
        {
            var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
            foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeCandidates(
                         scanResult,
                         source,
                         static name =>
                             PackAnalysisHelpers.IsPersistenceLikeName(name)
                             || PackAnalysisHelpers.IsHubObjectLikeName(name)
                             || name.EndsWith("AlgorithmRunner", StringComparison.Ordinal)
                             || name.EndsWith("Algorithm", StringComparison.Ordinal)
                             || name.EndsWith("Analyzer", StringComparison.Ordinal)))
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
                    && !string.Equals(referenced.ResolvedSymbol, symbol, StringComparison.OrdinalIgnoreCase))
                {
                    symbols.Add(referenced.ResolvedSymbol);
                }
            }
        }

        return new SecondHopDiscovery(
            symbols.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
            ambiguousCandidates.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray());
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
                PackExtractionConventions.GetSimpleTypeName(symbol).Contains("Algorithm", StringComparison.Ordinal)
                || PackExtractionConventions.GetSimpleTypeName(symbol).Contains("Analyzer", StringComparison.Ordinal)
                || PackExtractionConventions.GetSimpleTypeName(symbol).Contains("Port", StringComparison.Ordinal)
                || PackExtractionConventions.GetSimpleTypeName(symbol).Contains("Adapter", StringComparison.Ordinal)))
        {
            goalCategoryMatches++;
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
        ICollection<PackDecisionTrace> decisionTraces)
    {
        foreach (var analysis in analyses)
        {
            var decisionKind = analysis.Evaluation.IsSelectable
                ? "selected"
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

    private static void AddUnknowns(
        RelevantPackContext relevantContext,
        WorkflowDiscoveryResult discovery,
        IReadOnlyList<WorkflowAnalysis> analyses,
        ICollection<Diagnostic> unknowns,
        ICollection<PackDecisionTrace> decisionTraces)
    {
        if (discovery.AmbiguousTargets.Count > 0)
        {
            var diagnostic = HighSignalSelectionSupport.BuildDiagnostic(
                "workflow.ambiguous-target",
                "Workflow candidates were found, but at least one downstream target was ambiguous.",
                null,
                DiagnosticSeverity.Info,
                discovery.AmbiguousTargets);
            unknowns.Add(diagnostic);
            decisionTraces.Add(HighSignalSelectionSupport.BuildDecisionTrace(
                "workflow-selection",
                "workflow.ambiguous-target",
                "unknown-raised",
                diagnostic.Message,
                new SectionSelectionEvaluation(0, SectionConfidence.Low, relevantContext.GoalProfile.Terms, [], [], new SectionSignalEvidence(HasAmbiguousCandidates: true)),
                0,
                analyses.Count));
        }

        if (discovery.MissingDownstreamCandidates.Count > 0)
        {
            var diagnostic = HighSignalSelectionSupport.BuildDiagnostic(
                "workflow.missing-downstream",
                "Workflow owner was found, but no downstream service or persistence symbol was resolved within two hops.",
                null,
                DiagnosticSeverity.Info,
                discovery.MissingDownstreamCandidates);
            unknowns.Add(diagnostic);
            decisionTraces.Add(HighSignalSelectionSupport.BuildDecisionTrace(
                "workflow-selection",
                "workflow.missing-downstream",
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

    private static string BuildSummary(IReadOnlyList<WorkflowAnalysis> analyses) =>
        $"Selected {analyses.Count} workflow chains. {string.Join(" / ", analyses.Take(3).Select(static analysis => analysis.ToIndexLine()))}";

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
        IReadOnlyList<string> AmbiguousCandidates);

    private sealed record WorkflowDiscoveryResult(
        IReadOnlyList<WorkflowCandidate> Candidates,
        IReadOnlyList<string> AmbiguousTargets,
        IReadOnlyList<string> MissingDownstreamCandidates);

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
}
