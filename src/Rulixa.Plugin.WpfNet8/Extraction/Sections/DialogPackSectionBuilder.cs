using Rulixa.Application.Ports;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class DialogPackSectionBuilder
{
    private readonly IWorkspaceFileSystem workspaceFileSystem;

    internal DialogPackSectionBuilder(IWorkspaceFileSystem workspaceFileSystem)
    {
        this.workspaceFileSystem = workspaceFileSystem ?? throw new ArgumentNullException(nameof(workspaceFileSystem));
    }

    internal async Task AddAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        ICollection<Contract> contracts,
        ICollection<FileSelectionCandidate> fileCandidates,
        CancellationToken cancellationToken)
    {
        if (resolvedEntry.ResolvedKind != ResolvedEntryKind.Symbol || string.IsNullOrWhiteSpace(resolvedEntry.ResolvedPath))
        {
            return;
        }

        var absolutePath = Path.Combine(workspaceRoot, resolvedEntry.ResolvedPath.Replace('/', Path.DirectorySeparatorChar));
        var source = await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);

        var reachableImplementations = scanResult.ServiceRegistrations
            .Where(registration =>
                SourceContainsIdentifier(source, registration.ServiceType)
                || SourceContainsIdentifier(source, registration.ImplementationType))
            .Select(static registration => registration.ImplementationType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var activations = scanResult.WindowActivations
            .Where(activation => reachableImplementations.Any(implementation =>
                activation.ServiceSymbol.EndsWith($".{implementation}", StringComparison.OrdinalIgnoreCase)
                || activation.CallerSymbol.Contains(implementation, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(static activation => activation.WindowSymbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var activation in activations)
        {
            contracts.Add(new Contract(
                ContractKind.DialogActivation,
                activation.WindowSymbol,
                $"{activation.CallerSymbol} から {activation.WindowSymbol} が起動されます。",
                [],
                [activation.CallerSymbol, activation.ServiceSymbol, activation.WindowSymbol]));

            var serviceFile = scanResult.Symbols.FirstOrDefault(symbol =>
                string.Equals(symbol.QualifiedName, activation.ServiceSymbol, StringComparison.OrdinalIgnoreCase))?.FilePath;
            if (!string.IsNullOrWhiteSpace(serviceFile))
            {
                fileCandidates.Add(new FileSelectionCandidate(serviceFile, "dialog-service", 12, false));
            }
        }
    }

    private static bool SourceContainsIdentifier(string source, string identifier)
    {
        var simpleName = PackExtractionConventions.GetSimpleTypeName(identifier);
        return source.Contains(simpleName, StringComparison.Ordinal);
    }
}
