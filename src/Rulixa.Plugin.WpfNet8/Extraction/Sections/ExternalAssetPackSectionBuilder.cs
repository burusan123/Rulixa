using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class ExternalAssetPackSectionBuilder
{
    private const int MaxExternalAssetLines = 3;
    private readonly IWorkspaceFileSystem workspaceFileSystem;

    internal ExternalAssetPackSectionBuilder(IWorkspaceFileSystem workspaceFileSystem)
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
        var usages = await DiscoverAssetsAsync(workspaceRoot, scanResult, relevantContext, cancellationToken).ConfigureAwait(false);
        var analyses = AnalyzeAssets(scanResult, relevantContext, usages);
        var compression = CompressAnalyses(analyses);
        AddDecisionTraces(analyses, compression.DecisionKinds, decisionTraces);

        var selected = compression.Selected.ToArray();
        if (selected.Length == 0)
        {
            AddUnknowns(relevantContext, analyses, unknowns, decisionTraces);
            return;
        }

        indexes.Add(new IndexSection("External Assets", selected.Select(static analysis => analysis.ToIndexLine()).ToArray()));
        contracts.Add(new Contract(
            ContractKind.DependencyInjection,
            "External Assets",
            BuildSummary(selected),
            selected.Select(static analysis => analysis.Usage.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            selected.Select(static analysis => analysis.Usage.OwnerSymbol).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));

        foreach (var analysis in selected)
        {
            fileCandidates.Add(new FileSelectionCandidate(
                analysis.Usage.FilePath,
                "external-asset",
                analysis.Evaluation.ToPriority(30),
                false));
        }

        AddUnknowns(relevantContext, analyses, unknowns, decisionTraces);
    }

    private async Task<IReadOnlyList<ExternalAssetUsage>> DiscoverAssetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        CancellationToken cancellationToken)
    {
        var usages = new List<ExternalAssetUsage>();
        var candidateSymbols = new HashSet<string>(relevantContext.ViewModelSymbols, StringComparer.OrdinalIgnoreCase);

        foreach (var symbol in relevantContext.ViewModelSymbols)
        {
            foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, symbol))
            {
                var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
                foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeSymbols(
                             scanResult,
                             source,
                             static name =>
                                 PackAnalysisHelpers.IsWorkflowLikeName(name)
                                 || PackAnalysisHelpers.IsPersistenceLikeName(name)
                                 || PackAnalysisHelpers.IsHubObjectLikeName(name)))
                {
                    candidateSymbols.Add(referenced);
                }
            }
        }

        var expandedSymbols = candidateSymbols.ToArray();
        foreach (var symbol in expandedSymbols)
        {
            foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, symbol))
            {
                var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
                foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeSymbols(
                             scanResult,
                             source,
                             static name =>
                                 PackAnalysisHelpers.IsPersistenceLikeName(name)
                                 || PackAnalysisHelpers.IsWorkflowLikeName(name)))
                {
                    candidateSymbols.Add(referenced);
                }
            }
        }

        foreach (var symbol in candidateSymbols.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var filePath in PackAnalysisHelpers.GetSymbolFilePaths(scanResult, relevantContext, symbol))
            {
                var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
                var descriptors = ExtractAssetDescriptors(source);
                if (descriptors.Count == 0)
                {
                    continue;
                }

                usages.Add(new ExternalAssetUsage(
                    symbol,
                    filePath,
                    descriptors,
                    HasResolutionCode(descriptors)));
            }
        }

        return usages
            .DistinctBy(static usage => usage.Key, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static usage => usage.OwnerDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static usage => usage.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<ExternalAssetAnalysis> AnalyzeAssets(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        IReadOnlyList<ExternalAssetUsage> usages)
    {
        var analyses = usages
            .Select(usage => new ExternalAssetAnalysis(
                usage,
                HighSignalSelectionSupport.Evaluate(
                    relevantContext.GoalProfile,
                    BuildTextEvidence(usage),
                    BuildEvidence(scanResult, relevantContext, usage))))
            .OrderByDescending(static analysis => analysis.Evaluation.Score)
            .ThenBy(static analysis => analysis.Usage.OwnerDisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        for (var index = 0; index < analyses.Length; index++)
        {
            analyses[index] = analyses[index] with { Rank = index + 1, CandidateCount = analyses.Length };
        }

        return analyses;
    }

    private static IReadOnlyList<SectionTextEvidence> BuildTextEvidence(ExternalAssetUsage usage) =>
    [
        new("owner-symbol", [usage.OwnerSymbol]),
        new("asset-descriptor", usage.Descriptors)
    ];

    private static SectionSignalEvidence BuildEvidence(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        ExternalAssetUsage usage)
    {
        var goalCategoryMatches = 0;
        if (relevantContext.GoalProfile.HasCategory("system") || relevantContext.GoalProfile.HasCategory("project"))
        {
            goalCategoryMatches++;
        }

        if (relevantContext.GoalProfile.HasCategory("drafting")
            && usage.Descriptors.Any(static descriptor => descriptor.Contains(".onnx", StringComparison.OrdinalIgnoreCase)))
        {
            goalCategoryMatches++;
        }

        return new SectionSignalEvidence(
            ServiceRegistrationMatches: PackAnalysisHelpers.CountServiceRegistrationMatches(scanResult, [usage.OwnerSymbol]),
            FileKindMatches: PackAnalysisHelpers.CountFileKindMatches(scanResult, [usage.FilePath], ScanFileKind.Service, ScanFileKind.Config, ScanFileKind.CSharp),
            GoalCategoryMatches: goalCategoryMatches,
            SemanticSignalCount: usage.Descriptors.Count,
            DownstreamCount: usage.HasResolutionCode ? 1 : 0,
            HasOnlyFilePathEvidence: !usage.HasResolutionCode);
    }

    private static void AddDecisionTraces(
        IReadOnlyList<ExternalAssetAnalysis> analyses,
        IReadOnlyDictionary<string, string> decisionKinds,
        ICollection<PackDecisionTrace> decisionTraces)
    {
        foreach (var analysis in analyses)
        {
            var decisionKind = decisionKinds.TryGetValue(analysis.Usage.Key, out var resolvedDecision)
                ? resolvedDecision
                : "omitted-low-score";
            decisionTraces.Add(HighSignalSelectionSupport.BuildDecisionTrace(
                "external-asset-selection",
                analysis.Usage.Key,
                decisionKind,
                $"{analysis.Evaluation.ConfidenceLabel}: {analysis.Usage.OwnerDisplayName} -> {string.Join(" / ", analysis.Usage.Descriptors)}",
                analysis.Evaluation,
                analysis.Rank,
                analysis.CandidateCount));
        }
    }

    private static SectionCompressionResult<ExternalAssetAnalysis> CompressAnalyses(IReadOnlyList<ExternalAssetAnalysis> analyses)
    {
        var candidates = analyses
            .Select(analysis => new SectionCompressionCandidate<ExternalAssetAnalysis>(
                analysis,
                analysis.Usage.Key,
                analysis.Usage.AssetFamily,
                analysis.Evaluation,
                IsUiBoundary: PackAnalysisHelpers.IsUiBoundaryLikeName(analysis.Usage.OwnerDisplayName),
                IsWeakRoute: !analysis.Usage.HasResolutionCode || string.Equals(analysis.Usage.AssetFamily, "other", StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        return SectionCompressionSupport.Compress(candidates, MaxExternalAssetLines);
    }

    private static void AddUnknowns(
        RelevantPackContext relevantContext,
        IReadOnlyList<ExternalAssetAnalysis> analyses,
        ICollection<Diagnostic> unknowns,
        ICollection<PackDecisionTrace> decisionTraces)
    {
        if (analyses.Any(static analysis => analysis.Evaluation.IsSelectable))
        {
            return;
        }

        var candidates = analyses.Select(static analysis => analysis.Usage.OwnerSymbol).ToArray();
        if (candidates.Length == 0)
        {
            return;
        }

        var diagnostic = HighSignalSelectionSupport.BuildDiagnostic(
            "external-asset.unresolved-source",
            $"追跡できた範囲: {BuildKnownRange(candidates)}。停止点: パス文字列は見つかりましたが、資産を実際に解決・読込する経路を確定できませんでした。",
            null,
            DiagnosticSeverity.Info,
            candidates);
        unknowns.Add(diagnostic);
        decisionTraces.Add(HighSignalSelectionSupport.BuildGuidedUnknownTrace(
            "external-asset-selection",
            "external-asset.unresolved-source",
            $"{diagnostic.Message} 次に見る候補: {FormatCandidates(diagnostic.Candidates)}",
            relevantContext.GoalProfile,
            analyses.Count));
    }

    private static IReadOnlyList<string> ExtractAssetDescriptors(string source)
    {
        var descriptors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddIfContains(source, descriptors, ".xlsx");
        AddIfContains(source, descriptors, ".json");
        AddIfContains(source, descriptors, ".onnx");
        AddIfContains(source, descriptors, ".pdf");
        AddIfContains(source, descriptors, ".template");
        AddIfContains(source, descriptors, "File.Exists");
        AddIfContains(source, descriptors, "Path.Combine");
        AddIfContains(source, descriptors, "ReadAllText");
        AddIfContains(source, descriptors, "ReadAllBytes");
        AddIfContains(source, descriptors, "OpenRead");
        AddIfContains(source, descriptors, "OpenText");
        AddIfContains(source, descriptors, "GetManifestResourceStream");
        return descriptors.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool HasResolutionCode(IReadOnlyList<string> descriptors)
    {
        var descriptorSet = descriptors.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return descriptorSet.Contains("Path.Combine")
            || descriptorSet.Contains("ReadAllText")
            || descriptorSet.Contains("ReadAllBytes")
            || descriptorSet.Contains("OpenRead")
            || descriptorSet.Contains("OpenText")
            || descriptorSet.Contains("GetManifestResourceStream");
    }

    private static void AddIfContains(string source, ISet<string> descriptors, string token)
    {
        if (source.Contains(token, StringComparison.OrdinalIgnoreCase))
        {
            descriptors.Add(token);
        }
    }

    private static string BuildSummary(IReadOnlyList<ExternalAssetAnalysis> analyses) =>
        $"この画面は {string.Join(" / ", analyses.Select(static analysis => analysis.Usage.AssetFamily))} 系の外部資産を解決します。";

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

    private sealed record ExternalAssetUsage(
        string OwnerSymbol,
        string FilePath,
        IReadOnlyList<string> Descriptors,
        bool HasResolutionCode)
    {
        internal string Key => $"{OwnerSymbol}|{FilePath}|{string.Join("|", Descriptors)}";

        internal string OwnerDisplayName => PackExtractionConventions.GetSimpleTypeName(OwnerSymbol);

        internal string AssetFamily => PackAnalysisHelpers.ClassifyAssetFamily(Descriptors);
    }

    private sealed record ExternalAssetAnalysis(
        ExternalAssetUsage Usage,
        SectionSelectionEvaluation Evaluation)
    {
        internal int Rank { get; init; }

        internal int CandidateCount { get; init; }

        internal string ToIndexLine() =>
            $"{Evaluation.ConfidenceLabel}: {Usage.OwnerDisplayName} -> {string.Join(" / ", Usage.Descriptors)}";
    }
}
