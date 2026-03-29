using Rulixa.Application.Ports;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class DialogPackSectionBuilder
{
    private readonly IWorkspaceFileSystem workspaceFileSystem;
    private readonly CSharpSnippetCandidateFactory snippetFactory;

    internal DialogPackSectionBuilder(
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
        ICollection<SnippetSelectionCandidate> snippetCandidates,
        ICollection<FileSelectionCandidate> fileCandidates,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resolvedEntry.ResolvedPath))
        {
            return;
        }

        var absolutePath = Path.Combine(workspaceRoot, resolvedEntry.ResolvedPath.Replace('/', Path.DirectorySeparatorChar));
        var source = await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
        var relatedSymbolNames = relevantContext.RelatedSymbols
            .Select(PackExtractionConventions.GetSimpleTypeName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var reachableImplementations = scanResult.ServiceRegistrations
            .Where(registration =>
                SourceContainsIdentifier(source, registration.ServiceType)
                || SourceContainsIdentifier(source, registration.ImplementationType))
            .Select(static registration => registration.ImplementationType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var activations = scanResult.WindowActivations
            .Where(activation => IsRelevantActivation(activation, reachableImplementations, relevantContext.RelatedSymbols, relatedSymbolNames))
            .OrderBy(static activation => activation.WindowSymbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var activation in activations)
        {
            contracts.Add(new Contract(
                ContractKind.DialogActivation,
                activation.WindowSymbol,
                $"{activation.CallerSymbol} から {activation.WindowSymbol} が {activation.ActivationKind} で起動されます。owner={activation.OwnerKind}。",
                [],
                [activation.CallerSymbol, activation.ServiceSymbol, activation.WindowSymbol]));

            var serviceFile = scanResult.Symbols.FirstOrDefault(symbol =>
                string.Equals(symbol.QualifiedName, activation.ServiceSymbol, StringComparison.OrdinalIgnoreCase))?.FilePath;
            if (!string.IsNullOrWhiteSpace(serviceFile))
            {
                fileCandidates.Add(new FileSelectionCandidate(serviceFile, "dialog-service", 12, false));
                if (PackExtractionConventions.ShouldCreateSnippet(scanResult, serviceFile))
                {
                    var methodName = activation.CallerSymbol.Split('.').Last();
                    var snippet = await snippetFactory
                        .CreateMethodSnippetAsync(
                            workspaceRoot,
                            serviceFile,
                            methodName,
                            "dialog-service",
                            25,
                            false,
                            $"{methodName}(...)",
                            null,
                            cancellationToken)
                        .ConfigureAwait(false);
                    if (snippet is not null)
                    {
                        snippetCandidates.Add(snippet);
                    }
                }
            }

            AddDialogTargetFiles(scanResult, activation, fileCandidates);
        }
    }

    private static bool SourceContainsIdentifier(string source, string identifier)
    {
        var simpleName = PackExtractionConventions.GetSimpleTypeName(identifier);
        return source.Contains(simpleName, StringComparison.Ordinal);
    }

    private static bool IsRelevantActivation(
        WindowActivation activation,
        IReadOnlyList<string> reachableImplementations,
        IReadOnlySet<string> relatedSymbols,
        IReadOnlySet<string> relatedSymbolNames)
    {
        if (reachableImplementations.Any(implementation =>
                activation.ServiceSymbol.EndsWith($".{implementation}", StringComparison.OrdinalIgnoreCase)
                || activation.CallerSymbol.Contains(implementation, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (relatedSymbols.Contains(activation.ServiceSymbol)
            || relatedSymbols.Contains(activation.WindowSymbol)
            || (!string.IsNullOrWhiteSpace(activation.WindowViewModelSymbol) && relatedSymbols.Contains(activation.WindowViewModelSymbol)))
        {
            return true;
        }

        foreach (var relatedSymbol in relatedSymbols)
        {
            if (activation.CallerSymbol.StartsWith($"{relatedSymbol}.", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var activationNames = new[]
        {
            PackExtractionConventions.GetSimpleTypeName(activation.ServiceSymbol),
            PackExtractionConventions.GetSimpleTypeName(activation.WindowSymbol),
            string.IsNullOrWhiteSpace(activation.WindowViewModelSymbol)
                ? string.Empty
                : PackExtractionConventions.GetSimpleTypeName(activation.WindowViewModelSymbol)
        };

        return activationNames.Any(name => !string.IsNullOrWhiteSpace(name) && relatedSymbolNames.Contains(name));
    }

    private static void AddDialogTargetFiles(
        WorkspaceScanResult scanResult,
        WindowActivation activation,
        ICollection<FileSelectionCandidate> fileCandidates)
    {
        var windowFile = scanResult.Symbols.FirstOrDefault(symbol =>
            string.Equals(symbol.QualifiedName, activation.WindowSymbol, StringComparison.OrdinalIgnoreCase))?.FilePath;
        if (!string.IsNullOrWhiteSpace(windowFile))
        {
            fileCandidates.Add(new FileSelectionCandidate(windowFile, "dialog-window", 8, true));
            if (windowFile.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                PackExtractionConventions.AddCodeBehindIfPresent(scanResult, windowFile, fileCandidates, 9, false);
            }
        }

        if (string.IsNullOrWhiteSpace(activation.WindowViewModelSymbol))
        {
            return;
        }

        var windowViewModelFile = scanResult.Symbols.FirstOrDefault(symbol =>
            string.Equals(symbol.QualifiedName, activation.WindowViewModelSymbol, StringComparison.OrdinalIgnoreCase))?.FilePath;
        if (!string.IsNullOrWhiteSpace(windowViewModelFile))
        {
            fileCandidates.Add(new FileSelectionCandidate(windowViewModelFile, "dialog-viewmodel", 10, true));
        }
    }
}
