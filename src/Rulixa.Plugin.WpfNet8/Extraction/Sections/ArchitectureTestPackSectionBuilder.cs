using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class ArchitectureTestPackSectionBuilder
{
    private readonly IWorkspaceFileSystem workspaceFileSystem;

    internal ArchitectureTestPackSectionBuilder(IWorkspaceFileSystem workspaceFileSystem)
    {
        this.workspaceFileSystem = workspaceFileSystem ?? throw new ArgumentNullException(nameof(workspaceFileSystem));
    }

    internal async Task AddAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        ICollection<Contract> contracts,
        ICollection<IndexSection> indexes,
        ICollection<FileSelectionCandidate> fileCandidates,
        ICollection<PackDecisionTrace> decisionTraces,
        ICollection<Diagnostic> unknowns,
        CancellationToken cancellationToken)
    {
        var tests = await DiscoverTestsAsync(workspaceRoot, scanResult, cancellationToken).ConfigureAwait(false);
        var analyses = AnalyzeTests(scanResult, relevantContext, tests);
        AddDecisionTraces(analyses, decisionTraces);

        var selected = analyses
            .Where(static analysis => analysis.Evaluation.IsSelectable)
            .ToArray();
        if (selected.Length == 0)
        {
            if (relevantContext.GoalProfile.HasCategory("architecture") || relevantContext.GoalProfile.HasCategory("system"))
            {
                var diagnostic = HighSignalSelectionSupport.BuildDiagnostic(
                    "architecture-tests.not-found",
                    "No architecture, golden, or regression-style tests were detected from the scanned workspace.",
                    null,
                    DiagnosticSeverity.Info,
                    []);
                unknowns.Add(diagnostic);
                decisionTraces.Add(HighSignalSelectionSupport.BuildDecisionTrace(
                    "architecture-test-selection",
                    "architecture-tests.not-found",
                    "unknown-raised",
                    diagnostic.Message,
                    new SectionSelectionEvaluation(0, SectionConfidence.Low, relevantContext.GoalProfile.Terms, [], [], new SectionSignalEvidence()),
                    0,
                    0));
            }

            return;
        }

        indexes.Add(new IndexSection("Architecture Tests", selected.Select(static analysis => analysis.ToIndexLine()).ToArray()));
        contracts.Add(new Contract(
            ContractKind.DependencyInjection,
            "Architecture Tests",
            BuildSummary(selected),
            selected.Select(static analysis => analysis.Signal.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            []));

        foreach (var analysis in selected)
        {
            fileCandidates.Add(new FileSelectionCandidate(
                analysis.Signal.FilePath,
                "architecture-test",
                analysis.Evaluation.ToPriority(32),
                false));
        }
    }

    private async Task<IReadOnlyList<ArchitectureTestSignal>> DiscoverTestsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        CancellationToken cancellationToken)
    {
        var candidates = scanResult.Files
            .Where(static file =>
                file.Path.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)
                || file.Path.Contains("/tests/", StringComparison.OrdinalIgnoreCase))
            .Where(static file => file.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var signals = new List<ArchitectureTestSignal>();

        foreach (var candidate in candidates)
        {
            var source = await ReadSourceAsync(workspaceRoot, candidate.Path, cancellationToken).ConfigureAwait(false);
            var descriptors = ExtractDescriptors(candidate.Path, source);
            if (descriptors.Count == 0)
            {
                continue;
            }

            signals.Add(new ArchitectureTestSignal(candidate.Path, descriptors));
        }

        return signals;
    }

    private IReadOnlyList<ArchitectureTestAnalysis> AnalyzeTests(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IReadOnlyList<ArchitectureTestSignal> signals)
    {
        var analyses = signals
            .Select(signal => new ArchitectureTestAnalysis(
                signal,
                HighSignalSelectionSupport.Evaluate(
                    relevantContext.GoalProfile,
                    BuildTextEvidence(signal),
                    BuildEvidence(scanResult, relevantContext, signal))))
            .OrderByDescending(static analysis => analysis.Evaluation.Score)
            .ThenBy(static analysis => analysis.Signal.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        for (var index = 0; index < analyses.Length; index++)
        {
            analyses[index] = analyses[index] with { Rank = index + 1, CandidateCount = analyses.Length };
        }

        return analyses;
    }

    private static IReadOnlyList<SectionTextEvidence> BuildTextEvidence(ArchitectureTestSignal signal) =>
    [
        new("test-file", [signal.FilePath]),
        new("test-descriptor", signal.Descriptors)
    ];

    private static SectionSignalEvidence BuildEvidence(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        ArchitectureTestSignal signal)
    {
        var goalCategoryMatches =
            relevantContext.GoalProfile.HasCategory("architecture") || relevantContext.GoalProfile.HasCategory("system")
                ? 1
                : 0;

        return new SectionSignalEvidence(
            FileKindMatches: PackAnalysisHelpers.CountFileKindMatches(scanResult, [signal.FilePath], ScanFileKind.CSharp),
            GoalCategoryMatches: goalCategoryMatches,
            SemanticSignalCount: signal.Descriptors.Count,
            DownstreamCount: 1);
    }

    private static void AddDecisionTraces(
        IReadOnlyList<ArchitectureTestAnalysis> analyses,
        ICollection<PackDecisionTrace> decisionTraces)
    {
        foreach (var analysis in analyses)
        {
            var decisionKind = analysis.Evaluation.IsSelectable
                ? "selected"
                : "omitted-low-score";
            decisionTraces.Add(HighSignalSelectionSupport.BuildDecisionTrace(
                "architecture-test-selection",
                analysis.Signal.FilePath,
                decisionKind,
                $"{analysis.Evaluation.ConfidenceLabel}: {Path.GetFileName(analysis.Signal.FilePath)}",
                analysis.Evaluation,
                analysis.Rank,
                analysis.CandidateCount));
        }
    }

    private static IReadOnlyList<string> ExtractDescriptors(string path, string source)
    {
        var descriptors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddIfContains(path, source, descriptors, "Architecture");
        AddIfContains(path, source, descriptors, "Golden");
        AddIfContains(path, source, descriptors, "Regression");
        AddIfContains(path, source, descriptors, "layer");
        AddIfContains(path, source, descriptors, "dependency");
        AddIfContains(path, source, descriptors, "Should");
        return descriptors.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void AddIfContains(string path, string source, ISet<string> descriptors, string token)
    {
        if (path.Contains(token, StringComparison.OrdinalIgnoreCase)
            || source.Contains(token, StringComparison.OrdinalIgnoreCase))
        {
            descriptors.Add(token);
        }
    }

    private static string BuildSummary(IReadOnlyList<ArchitectureTestAnalysis> analyses) =>
        $"Selected {analyses.Count} architecture test signals. {string.Join(" / ", analyses.Take(3).Select(static analysis => analysis.ToIndexLine()))}";

    private async Task<string> ReadSourceAsync(
        string workspaceRoot,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
    }

    private sealed record ArchitectureTestSignal(
        string FilePath,
        IReadOnlyList<string> Descriptors);

    private sealed record ArchitectureTestAnalysis(
        ArchitectureTestSignal Signal,
        SectionSelectionEvaluation Evaluation)
    {
        internal int Rank { get; init; }

        internal int CandidateCount { get; init; }

        internal string ToIndexLine() =>
            $"{Evaluation.ConfidenceLabel}: {Path.GetFileName(Signal.FilePath)} -> {string.Join(" / ", Signal.Descriptors)}";
    }
}
