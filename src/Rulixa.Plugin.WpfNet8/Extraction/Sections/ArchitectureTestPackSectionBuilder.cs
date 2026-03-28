using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class ArchitectureTestPackSectionBuilder
{
    private const int MaxArchitectureFamilies = 4;
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
        var families = await DiscoverFamiliesAsync(workspaceRoot, scanResult, cancellationToken).ConfigureAwait(false);
        var analyses = AnalyzeFamilies(scanResult, relevantContext, families);
        var compression = CompressAnalyses(analyses);
        AddDecisionTraces(analyses, compression.DecisionKinds, decisionTraces);

        var selected = compression.Selected.ToArray();
        if (selected.Length == 0)
        {
            if (relevantContext.GoalProfile.HasCategory("architecture") || relevantContext.GoalProfile.HasCategory("system"))
            {
                var diagnostic = HighSignalSelectionSupport.BuildDiagnostic(
                    "architecture-tests.not-found",
                    "追跡できた範囲: tests 配下のファイル走査まで。停止点: architecture / golden / regression / compatibility の代表 family を確定できませんでした。",
                    null,
                    DiagnosticSeverity.Info,
                    []);
                unknowns.Add(diagnostic);
                decisionTraces.Add(HighSignalSelectionSupport.BuildGuidedUnknownTrace(
                    "architecture-test-selection",
                    "architecture-tests.not-found",
                    diagnostic.Message,
                    relevantContext.GoalProfile,
                    0));
            }

            return;
        }

        indexes.Add(new IndexSection("Architecture Tests", selected.Select(static analysis => analysis.ToIndexLine()).ToArray()));
        contracts.Add(new Contract(
            ContractKind.DependencyInjection,
            "Architecture Tests",
            BuildSummary(selected),
            selected.SelectMany(static analysis => analysis.Family.RepresentativeFiles).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            []));

        foreach (var analysis in selected)
        {
            foreach (var filePath in analysis.Family.RepresentativeFiles)
            {
                fileCandidates.Add(new FileSelectionCandidate(
                    filePath,
                    "architecture-test",
                    analysis.Evaluation.ToPriority(32),
                    false));
            }
        }
    }

    private async Task<IReadOnlyList<ArchitectureFamilyCandidate>> DiscoverFamiliesAsync(
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
        var filesByFamily = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            var source = await ReadSourceAsync(workspaceRoot, candidate.Path, cancellationToken).ConfigureAwait(false);
            foreach (var family in ExtractFamilies(candidate.Path, source))
            {
                if (!filesByFamily.TryGetValue(family, out var files))
                {
                    files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    filesByFamily[family] = files;
                }

                files.Add(candidate.Path);
            }
        }

        return filesByFamily
            .Select(static pair => new ArchitectureFamilyCandidate(
                pair.Key,
                pair.Value.OrderBy(static file => file, StringComparer.OrdinalIgnoreCase).Take(2).ToArray(),
                pair.Value.Count))
            .OrderByDescending(static candidate => candidate.TotalFileCount)
            .ThenBy(static candidate => candidate.Family, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<ArchitectureFamilyAnalysis> AnalyzeFamilies(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IReadOnlyList<ArchitectureFamilyCandidate> families)
    {
        var analyses = families
            .Select(family => new ArchitectureFamilyAnalysis(
                family,
                HighSignalSelectionSupport.Evaluate(
                    relevantContext.GoalProfile,
                    BuildTextEvidence(family),
                    BuildEvidence(scanResult, relevantContext, family))))
            .OrderByDescending(static analysis => analysis.Evaluation.Score)
            .ThenByDescending(static analysis => analysis.Family.TotalFileCount)
            .ThenBy(static analysis => analysis.Family.Family, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        for (var index = 0; index < analyses.Length; index++)
        {
            analyses[index] = analyses[index] with { Rank = index + 1, CandidateCount = analyses.Length };
        }

        return analyses;
    }

    private static SectionCompressionResult<ArchitectureFamilyAnalysis> CompressAnalyses(
        IReadOnlyList<ArchitectureFamilyAnalysis> analyses)
    {
        var candidates = analyses
            .Select(analysis => new SectionCompressionCandidate<ArchitectureFamilyAnalysis>(
                analysis,
                analysis.Family.Family,
                analysis.Family.Family,
                analysis.Evaluation,
                IsWeakRoute: analysis.Family.TotalFileCount == 0))
            .ToArray();
        return SectionCompressionSupport.Compress(candidates, MaxArchitectureFamilies);
    }

    private static IReadOnlyList<SectionTextEvidence> BuildTextEvidence(ArchitectureFamilyCandidate family) =>
    [
        new("test-family", [family.Family]),
        new("test-file", family.RepresentativeFiles)
    ];

    private static SectionSignalEvidence BuildEvidence(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        ArchitectureFamilyCandidate family)
    {
        var goalCategoryMatches =
            relevantContext.GoalProfile.HasCategory("architecture") || relevantContext.GoalProfile.HasCategory("system")
                ? 1
                : 0;

        return new SectionSignalEvidence(
            FileKindMatches: PackAnalysisHelpers.CountFileKindMatches(scanResult, family.RepresentativeFiles, ScanFileKind.CSharp),
            GoalCategoryMatches: goalCategoryMatches,
            SemanticSignalCount: family.TotalFileCount,
            DownstreamCount: family.RepresentativeFiles.Count);
    }

    private static void AddDecisionTraces(
        IReadOnlyList<ArchitectureFamilyAnalysis> analyses,
        IReadOnlyDictionary<string, string> decisionKinds,
        ICollection<PackDecisionTrace> decisionTraces)
    {
        foreach (var analysis in analyses)
        {
            var decisionKind = decisionKinds.TryGetValue(analysis.Family.Family, out var resolvedDecision)
                ? resolvedDecision
                : "omitted-low-score";
            decisionTraces.Add(HighSignalSelectionSupport.BuildDecisionTrace(
                "architecture-test-selection",
                analysis.Family.Family,
                decisionKind,
                $"{analysis.Evaluation.ConfidenceLabel}: {analysis.Family.Family}",
                analysis.Evaluation,
                analysis.Rank,
                analysis.CandidateCount));
        }
    }

    private static IReadOnlyList<string> ExtractFamilies(string path, string source)
    {
        var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddFamilyIfContains(path, source, families, "Architecture", "Architecture");
        AddFamilyIfContains(path, source, families, "Golden", "Golden");
        AddFamilyIfContains(path, source, families, "Regression", "Regression");
        AddFamilyIfContains(path, source, families, "Compatibility", "Compatibility");
        if ((path.Contains("layer", StringComparison.OrdinalIgnoreCase)
                || source.Contains("layer", StringComparison.OrdinalIgnoreCase)
                || path.Contains("dependency", StringComparison.OrdinalIgnoreCase)
                || source.Contains("dependency", StringComparison.OrdinalIgnoreCase))
            && !families.Contains("Architecture"))
        {
            families.Add("Architecture");
        }

        return families.OrderBy(static family => family, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void AddFamilyIfContains(string path, string source, ISet<string> families, string token, string family)
    {
        if (path.Contains(token, StringComparison.OrdinalIgnoreCase)
            || source.Contains(token, StringComparison.OrdinalIgnoreCase))
        {
            families.Add(family);
        }
    }

    private static string BuildSummary(IReadOnlyList<ArchitectureFamilyAnalysis> analyses) =>
        $"このワークスペースでは {string.Join(" / ", analyses.Select(static analysis => analysis.Family.Family))} 系の回帰拘束が確認できます。";

    private async Task<string> ReadSourceAsync(
        string workspaceRoot,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
    }

    private sealed record ArchitectureFamilyCandidate(
        string Family,
        IReadOnlyList<string> RepresentativeFiles,
        int TotalFileCount);

    private sealed record ArchitectureFamilyAnalysis(
        ArchitectureFamilyCandidate Family,
        SectionSelectionEvaluation Evaluation)
    {
        internal int Rank { get; init; }

        internal int CandidateCount { get; init; }

        internal string ToIndexLine()
        {
            var samples = string.Join(", ", Family.RepresentativeFiles.Select(Path.GetFileName));
            return $"{Evaluation.ConfidenceLabel}: {Family.Family} family ({samples})";
        }
    }
}
