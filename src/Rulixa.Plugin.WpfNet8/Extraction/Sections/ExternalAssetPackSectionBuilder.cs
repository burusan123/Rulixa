using Rulixa.Application.Ports;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class ExternalAssetPackSectionBuilder
{
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
        CancellationToken cancellationToken)
    {
        var assets = await DiscoverAssetsAsync(workspaceRoot, scanResult, relevantContext, cancellationToken).ConfigureAwait(false);
        if (assets.Count == 0)
        {
            return;
        }

        indexes.Add(new IndexSection("External Assets", assets.Select(static asset => asset.ToIndexLine()).ToArray()));
        contracts.Add(new Contract(
            ContractKind.DependencyInjection,
            "External Assets",
            BuildSummary(assets),
            assets.Select(static asset => asset.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            assets.Select(static asset => asset.OwnerSymbol).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));

        foreach (var filePath in assets.Select(static asset => asset.FilePath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            fileCandidates.Add(new FileSelectionCandidate(filePath, "external-asset", 30, false));
        }
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

                usages.Add(new ExternalAssetUsage(symbol, filePath, descriptors));
            }
        }

        return usages
            .DistinctBy(static usage => usage.Key, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static usage => usage.OwnerDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static usage => usage.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
        return descriptors.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void AddIfContains(string source, ISet<string> descriptors, string token)
    {
        if (source.Contains(token, StringComparison.OrdinalIgnoreCase))
        {
            descriptors.Add(token);
        }
    }

    private static string BuildSummary(IReadOnlyList<ExternalAssetUsage> assets)
    {
        var samples = assets.Take(3).Select(static asset => asset.ToIndexLine()).ToArray();
        return $"Discovered {assets.Count} external asset access points. Examples: {string.Join(" / ", samples)}";
    }

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
        IReadOnlyList<string> Descriptors)
    {
        internal string Key => $"{OwnerSymbol}|{FilePath}|{string.Join("|", Descriptors)}";

        internal string OwnerDisplayName => PackExtractionConventions.GetSimpleTypeName(OwnerSymbol);

        internal string ToIndexLine() => $"{OwnerDisplayName} -> {string.Join(" / ", Descriptors)}";
    }
}
