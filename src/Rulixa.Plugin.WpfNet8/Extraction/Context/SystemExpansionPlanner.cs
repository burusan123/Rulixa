using Rulixa.Application.Ports;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class SystemExpansionPlanner
{
    private readonly IWorkspaceFileSystem workspaceFileSystem;

    internal SystemExpansionPlanner(IWorkspaceFileSystem workspaceFileSystem)
    {
        this.workspaceFileSystem = workspaceFileSystem ?? throw new ArgumentNullException(nameof(workspaceFileSystem));
    }

    internal async Task<SystemPackContext?> PlanAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        IReadOnlySet<string> relevantViewModelSymbols,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentNullException.ThrowIfNull(scanResult);
        ArgumentNullException.ThrowIfNull(resolvedEntry);
        ArgumentNullException.ThrowIfNull(relevantViewModelSymbols);

        var rootSymbol = ResolveRootSeed(scanResult, resolvedEntry, relevantViewModelSymbols);
        if (string.IsNullOrWhiteSpace(rootSymbol))
        {
            return null;
        }

        var rootAggregate = PartialSymbolAggregateResolver.Build(scanResult, [rootSymbol]);
        if (!rootAggregate.TryGetValue(rootSymbol, out var aggregate))
        {
            return null;
        }

        var relatedSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootSymbol };
        var explorationRootSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootSymbol };
        var familyBySymbol = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [rootSymbol] = PackAnalysisHelpers.ClassifySystemFamily(rootSymbol)
        };
        var familyCandidatesBySymbol = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [rootSymbol] = ["Shell"]
        };

        foreach (var candidate in await DiscoverReferencedSymbolsAsync(
                     workspaceRoot,
                     scanResult,
                     rootSymbol,
                     aggregate.FilePaths,
                     cancellationToken).ConfigureAwait(false))
        {
            RegisterRouteCandidate(candidate.Symbol, candidate.FirstHopSymbol, relatedSymbols, familyBySymbol, familyCandidatesBySymbol);
            if (PackAnalysisHelpers.IsViewModelLikeName(PackExtractionConventions.GetSimpleTypeName(candidate.Symbol)))
            {
                explorationRootSymbols.Add(candidate.Symbol);
            }
        }

        AddDialogSymbols(scanResult, relatedSymbols, explorationRootSymbols, familyBySymbol, familyCandidatesBySymbol);
        AddArchitectureSignals(scanResult, relatedSymbols, familyBySymbol, familyCandidatesBySymbol);

        var subMaps = BuildSubMaps(scanResult, rootSymbol, relatedSymbols, familyBySymbol);
        return new SystemPackContext(
            RootSymbol: rootSymbol,
            ExplorationRootSymbols: explorationRootSymbols,
            RelatedSymbols: relatedSymbols,
            FamilyBySymbol: familyBySymbol,
            FamilyCandidatesBySymbol: familyCandidatesBySymbol,
            SubMaps: subMaps);
    }

    private static string? ResolveRootSeed(
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        IReadOnlySet<string> relevantViewModelSymbols)
    {
        foreach (var symbol in relevantViewModelSymbols.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase))
        {
            if (scanResult.ProjectSummary.RootViewModels.Contains(symbol, StringComparer.OrdinalIgnoreCase))
            {
                return symbol;
            }
        }

        if (!string.IsNullOrWhiteSpace(resolvedEntry.Symbol)
            && scanResult.ProjectSummary.RootViewModels.Contains(resolvedEntry.Symbol, StringComparer.OrdinalIgnoreCase))
        {
            return resolvedEntry.Symbol;
        }

        if (!string.IsNullOrWhiteSpace(resolvedEntry.ResolvedPath))
        {
            var rootBinding = scanResult.ViewModelBindings.FirstOrDefault(binding =>
                string.Equals(binding.ViewPath, resolvedEntry.ResolvedPath, StringComparison.OrdinalIgnoreCase)
                && binding.BindingKind == ViewModelBindingKind.RootDataContext
                && scanResult.ProjectSummary.RootViewModels.Contains(binding.ViewModelSymbol, StringComparer.OrdinalIgnoreCase));
            if (rootBinding is not null)
            {
                return rootBinding.ViewModelSymbol;
            }

            var conventionalViewModel = PackExtractionConventions.FindConventionalViewModelSymbol(scanResult, resolvedEntry.ResolvedPath);
            if (!string.IsNullOrWhiteSpace(conventionalViewModel)
                && scanResult.ProjectSummary.RootViewModels.Contains(conventionalViewModel, StringComparer.OrdinalIgnoreCase))
            {
                return conventionalViewModel;
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<SystemRouteCandidate>> DiscoverReferencedSymbolsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        string rootSymbol,
        IReadOnlyList<string> rootFilePaths,
        CancellationToken cancellationToken)
    {
        var symbols = new Dictionary<string, SystemRouteCandidate>(StringComparer.OrdinalIgnoreCase);
        var rootTypeName = PackExtractionConventions.GetSimpleTypeName(rootSymbol);

        foreach (var filePath in rootFilePaths)
        {
            var source = await ReadSourceAsync(workspaceRoot, filePath, cancellationToken).ConfigureAwait(false);
            foreach (var dependency in PackAnalysisHelpers.ExtractConstructorDependencyTypeNames(
                         source,
                         rootTypeName))
            {
                var resolved = PackAnalysisHelpers.ResolveTypeSymbol(scanResult, dependency);
                if (!string.IsNullOrWhiteSpace(resolved)
                    && PackAnalysisHelpers.IsSystemExpansionRelevantName(PackExtractionConventions.GetSimpleTypeName(resolved)))
                {
                    symbols[resolved] = new SystemRouteCandidate(resolved, resolved);
                }
            }

            foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeCandidates(
                         scanResult,
                         source,
                         PackAnalysisHelpers.IsSystemExpansionRelevantName))
            {
                if (!string.IsNullOrWhiteSpace(referenced.ResolvedSymbol))
                {
                    symbols[referenced.ResolvedSymbol] = new SystemRouteCandidate(referenced.ResolvedSymbol, referenced.ResolvedSymbol);
                }
            }
        }

        return symbols.Values
            .OrderBy(candidate => PackAnalysisHelpers.GetSystemFamilyPriority(SystemFamilyRoutingSupport.ResolveFamily(candidate.FirstHopSymbol, candidate.Symbol)))
            .ThenBy(static candidate => candidate.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddDialogSymbols(
        WorkspaceScanResult scanResult,
        ISet<string> relatedSymbols,
        ISet<string> explorationRootSymbols,
        IDictionary<string, string> familyBySymbol,
        IDictionary<string, IReadOnlyList<string>> familyCandidatesBySymbol)
    {
        var referencedServiceSymbols = relatedSymbols
            .SelectMany(symbol => scanResult.ServiceRegistrations
                .Where(registration =>
                    string.Equals(registration.ServiceType, symbol, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(registration.ImplementationType, symbol, StringComparison.OrdinalIgnoreCase))
                .SelectMany(static registration => new[] { registration.ServiceType, registration.ImplementationType }))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var activation in scanResult.WindowActivations.Where(activation =>
                     relatedSymbols.Contains(activation.ServiceSymbol)
                     || referencedServiceSymbols.Contains(activation.ServiceSymbol, StringComparer.OrdinalIgnoreCase)))
        {
            RegisterRouteCandidate(
                activation.ServiceSymbol,
                activation.ServiceSymbol,
                relatedSymbols,
                familyBySymbol,
                familyCandidatesBySymbol);

            RegisterRouteCandidate(
                activation.WindowSymbol,
                activation.ServiceSymbol,
                relatedSymbols,
                familyBySymbol,
                familyCandidatesBySymbol);

            if (string.IsNullOrWhiteSpace(activation.WindowViewModelSymbol))
            {
                continue;
            }

            RegisterRouteCandidate(
                activation.WindowViewModelSymbol,
                activation.ServiceSymbol,
                relatedSymbols,
                familyBySymbol,
                familyCandidatesBySymbol);
            explorationRootSymbols.Add(activation.WindowViewModelSymbol);
        }
    }

    private static void AddArchitectureSignals(
        WorkspaceScanResult scanResult,
        ISet<string> relatedSymbols,
        IDictionary<string, string> familyBySymbol,
        IDictionary<string, IReadOnlyList<string>> familyCandidatesBySymbol)
    {
        if (!scanResult.Files.Any(static file =>
                file.Path.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)
                || file.Path.Contains("/tests/", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        const string architectureMarker = "__architecture__";
        relatedSymbols.Add(architectureMarker);
        familyBySymbol[architectureMarker] = "Architecture";
        familyCandidatesBySymbol[architectureMarker] = ["Architecture"];
    }

    private static IReadOnlyList<SystemSubMap> BuildSubMaps(
        WorkspaceScanResult scanResult,
        string rootSymbol,
        IEnumerable<string> relatedSymbols,
        IReadOnlyDictionary<string, string> familyBySymbol)
    {
        var subMaps = new List<SystemSubMap>();
        foreach (var group in relatedSymbols
                     .Where(static symbol => !string.IsNullOrWhiteSpace(symbol))
                     .GroupBy(symbol => familyBySymbol.TryGetValue(symbol, out var family) ? family : "Shell", StringComparer.OrdinalIgnoreCase)
                     .OrderBy(static group => PackAnalysisHelpers.GetSystemFamilyPriority(group.Key))
                     .ThenBy(static group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var representativeSymbol = SelectRepresentativeSymbol(group.Key, group, rootSymbol);
            var filePaths = group
                .Where(static symbol => symbol != "__architecture__")
                .SelectMany(symbol => PartialSymbolAggregateResolver.Build(scanResult, [symbol])
                    .TryGetValue(symbol, out var aggregate)
                    ? aggregate.FilePaths
                    : [])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            subMaps.Add(new SystemSubMap(
                Family: group.Key,
                RepresentativeSymbol: representativeSymbol,
                RelatedSymbols: group
                    .OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                FilePaths: filePaths));
        }

        return subMaps;
    }

    private static string SelectRepresentativeSymbol(
        string family,
        IEnumerable<string> symbols,
        string rootSymbol)
    {
        if (string.Equals(family, "Shell", StringComparison.OrdinalIgnoreCase))
        {
            return rootSymbol;
        }

        return symbols
            .OrderBy(static symbol => PackExtractionConventions.GetSimpleTypeName(symbol), StringComparer.OrdinalIgnoreCase)
            .First();
    }

    private static void RegisterRouteCandidate(
        string targetSymbol,
        string? firstHopSymbol,
        ISet<string> relatedSymbols,
        IDictionary<string, string> familyBySymbol,
        IDictionary<string, IReadOnlyList<string>> familyCandidatesBySymbol)
    {
        relatedSymbols.Add(targetSymbol);
        var candidateFamilies = SystemFamilyRoutingSupport.ResolveCandidateFamilies(firstHopSymbol, targetSymbol);
        familyCandidatesBySymbol[targetSymbol] = familyCandidatesBySymbol.TryGetValue(targetSymbol, out var existingFamilies)
            ? existingFamilies
                .Concat(candidateFamilies)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(SystemFamilyRoutingSupport.GetResolutionPriority)
                .ThenBy(static family => family, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : candidateFamilies;
        familyBySymbol[targetSymbol] = SystemFamilyRoutingSupport.SelectPreferredFamily(familyCandidatesBySymbol[targetSymbol]);
    }

    private async Task<string> ReadSourceAsync(
        string workspaceRoot,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
    }

    private sealed record SystemRouteCandidate(
        string Symbol,
        string? FirstHopSymbol);
}
