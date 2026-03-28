using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class HubObjectPackSectionBuilder
{
    private const int MaxHubObjectLines = 3;
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
        ICollection<PackDecisionTrace> decisionTraces,
        ICollection<Diagnostic> unknowns,
        CancellationToken cancellationToken)
    {
        var hubObjects = await DiscoverHubObjectsAsync(workspaceRoot, scanResult, relevantContext, cancellationToken)
            .ConfigureAwait(false);
        var analyses = AnalyzeHubObjects(scanResult, relevantContext, hubObjects);
        var compression = CompressAnalyses(analyses);
        AddDecisionTraces(analyses, compression.DecisionKinds, decisionTraces);

        var selected = compression.Selected.ToArray();
        if (selected.Length == 0)
        {
            AddUnknowns(relevantContext, analyses, unknowns, decisionTraces);
            return;
        }

        indexes.Add(new IndexSection("Hub Objects", selected.Select(static analysis => analysis.ToIndexLine()).ToArray()));
        contracts.Add(new Contract(
            ContractKind.DependencyInjection,
            "Hub Objects",
            BuildSummary(selected),
            selected.SelectMany(static analysis => analysis.Candidate.FilePaths).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            selected.Select(static analysis => analysis.Candidate.Symbol).ToArray()));

        foreach (var analysis in selected)
        {
            foreach (var filePath in analysis.Candidate.FilePaths)
            {
                fileCandidates.Add(new FileSelectionCandidate(filePath, "hub-object", analysis.Evaluation.ToPriority(28), false));
            }
        }

        AddUnknowns(relevantContext, analyses, unknowns, decisionTraces);
        await AddSnippetsAsync(workspaceRoot, scanResult, relevantContext, selected, snippetCandidates, cancellationToken)
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

                var allSources = new List<string>();
                var signals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var hubFile in hubFiles)
                {
                    var hubSource = await ReadSourceAsync(workspaceRoot, hubFile, cancellationToken).ConfigureAwait(false);
                    allSources.Add(hubSource);
                    foreach (var signal in ExtractSignals(hubSource))
                    {
                        signals.Add(signal);
                    }
                }

                candidates[referenced] = new HubObjectCandidate(
                    referenced,
                    signals.OrderBy(static signal => signal, StringComparer.OrdinalIgnoreCase).ToArray(),
                    hubFiles,
                    allSources.Count(source => PackAnalysisHelpers.HasHubObjectSignals(source)));
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

    private IReadOnlyList<HubObjectAnalysis> AnalyzeHubObjects(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IReadOnlyList<HubObjectCandidate> candidates)
    {
        var analyses = candidates
            .Select(candidate => new HubObjectAnalysis(
                candidate,
                HighSignalSelectionSupport.Evaluate(
                    relevantContext.GoalProfile,
                    BuildTextEvidence(candidate),
                    BuildEvidence(scanResult, relevantContext, candidate))))
            .OrderByDescending(static analysis => analysis.Evaluation.Score)
            .ThenBy(static analysis => analysis.Candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        for (var index = 0; index < analyses.Length; index++)
        {
            analyses[index] = analyses[index] with { Rank = index + 1, CandidateCount = analyses.Length };
        }

        return analyses;
    }

    private static IReadOnlyList<SectionTextEvidence> BuildTextEvidence(HubObjectCandidate candidate) =>
    [
        new("hub-object", [candidate.Symbol]),
        new("hub-signals", candidate.Signals)
    ];

    private static SectionSignalEvidence BuildEvidence(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        HubObjectCandidate candidate)
    {
        var goalCategoryMatches =
            relevantContext.GoalProfile.HasCategory("system") || relevantContext.GoalProfile.HasCategory("project")
                ? 1
                : 0;

        return new SectionSignalEvidence(
            PartialSymbolMatches: PackAnalysisHelpers.ResolveAggregate(scanResult, relevantContext, candidate.Symbol)?.FilePaths.Count > 1 ? 1 : 0,
            FileKindMatches: PackAnalysisHelpers.CountFileKindMatches(scanResult, candidate.FilePaths, ScanFileKind.CSharp, ScanFileKind.ViewModel, ScanFileKind.Service),
            GoalCategoryMatches: goalCategoryMatches,
            SemanticSignalCount: candidate.Signals.Count,
            DownstreamCount: candidate.SignalSourceCount);
    }

    private static void AddDecisionTraces(
        IReadOnlyList<HubObjectAnalysis> analyses,
        IReadOnlyDictionary<string, string> decisionKinds,
        ICollection<PackDecisionTrace> decisionTraces)
    {
        foreach (var analysis in analyses)
        {
            var decisionKind = decisionKinds.TryGetValue(analysis.Candidate.Symbol, out var resolvedDecision)
                ? resolvedDecision
                : "omitted-low-score";
            decisionTraces.Add(HighSignalSelectionSupport.BuildDecisionTrace(
                "hub-object-selection",
                analysis.Candidate.Symbol,
                decisionKind,
                $"{analysis.Evaluation.ConfidenceLabel}: {analysis.Candidate.DisplayName}",
                analysis.Evaluation,
                analysis.Rank,
                analysis.CandidateCount));
        }
    }

    private static SectionCompressionResult<HubObjectAnalysis> CompressAnalyses(IReadOnlyList<HubObjectAnalysis> analyses)
    {
        var candidates = analyses
            .Select(analysis => new SectionCompressionCandidate<HubObjectAnalysis>(
                analysis,
                analysis.Candidate.Symbol,
                PackAnalysisHelpers.ClassifyHubObjectFamily(analysis.Candidate.Symbol),
                analysis.Evaluation,
                IsSelfLoop: false,
                IsWeakRoute: analysis.Candidate.Signals.Count < 2))
            .ToArray();
        return SectionCompressionSupport.Compress(candidates, MaxHubObjectLines);
    }

    private static void AddUnknowns(
        RelevantPackContext relevantContext,
        IReadOnlyList<HubObjectAnalysis> analyses,
        ICollection<Diagnostic> unknowns,
        ICollection<PackDecisionTrace> decisionTraces)
    {
        if (analyses.Any(static analysis => analysis.Evaluation.IsSelectable))
        {
            return;
        }

        var weakCandidates = analyses
            .Where(static analysis => analysis.Candidate.Signals.Count < 2)
            .Select(static analysis => analysis.Candidate.Symbol)
            .ToArray();
        if (weakCandidates.Length == 0)
        {
            return;
        }

        var diagnostic = HighSignalSelectionSupport.BuildGuidedDiagnostic(
            "hub-object.weak-signal",
            BuildKnownRange(weakCandidates),
            "共有状態の中核と判断できる dirty / snapshot / restore 系の信号が不足しています",
            null,
            DiagnosticSeverity.Info,
            weakCandidates);
        unknowns.Add(diagnostic);
        decisionTraces.Add(HighSignalSelectionSupport.BuildGuidedUnknownTrace(
            "hub-object-selection",
            "hub-object.weak-signal",
            $"{diagnostic.Message} 次に見る候補: {FormatCandidates(diagnostic.Candidates)}",
            relevantContext.GoalProfile,
            analyses.Count));
    }

    private async Task AddSnippetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IReadOnlyList<HubObjectAnalysis> analyses,
        ICollection<SnippetSelectionCandidate> snippetCandidates,
        CancellationToken cancellationToken)
    {
        foreach (var analysis in analyses)
        {
            var filePath = PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, analysis.Candidate.Symbol).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(filePath) || !PackExtractionConventions.ShouldCreateSnippet(scanResult, filePath))
            {
                continue;
            }

            var className = PackExtractionConventions.GetSimpleTypeName(analysis.Candidate.Symbol);
            var snippet = await snippetFactory
                .CreateConstructorSnippetAsync(
                    workspaceRoot,
                    filePath,
                    className,
                    "hub-object",
                    analysis.Evaluation.ToPriority(28),
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

        if (source.Contains("Identity", StringComparison.Ordinal))
        {
            signals.Add("Identity");
        }

        return signals;
    }

    private static string BuildSummary(IReadOnlyList<HubObjectAnalysis> analyses) =>
        $"この画面の共有状態の中核は {string.Join(" / ", analyses.Select(static analysis => analysis.Candidate.DisplayName))} です。";

    private static string BuildKnownRange(IEnumerable<string> symbols)
    {
        var names = symbols
            .Select(PackExtractionConventions.GetSimpleTypeName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();
        return names.Length == 0 ? "入口シンボルまで" : string.Join(" / ", names);
    }

    private static string FormatCandidates(IReadOnlyList<string> candidates) =>
        candidates.Count == 0
            ? "なし"
            : string.Join(", ", candidates.Select(PackExtractionConventions.GetSimpleTypeName));

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
        IReadOnlyList<string> FilePaths,
        int SignalSourceCount)
    {
        internal string DisplayName => PackExtractionConventions.GetSimpleTypeName(Symbol);
    }

    private sealed record HubObjectAnalysis(
        HubObjectCandidate Candidate,
        SectionSelectionEvaluation Evaluation)
    {
        internal int Rank { get; init; }

        internal int CandidateCount { get; init; }

        internal string ToIndexLine() =>
            Candidate.Signals.Count == 0
                ? $"{Evaluation.ConfidenceLabel}: {Candidate.DisplayName}"
                : $"{Evaluation.ConfidenceLabel}: {Candidate.DisplayName} ({string.Join(", ", Candidate.Signals)})";
    }
}
