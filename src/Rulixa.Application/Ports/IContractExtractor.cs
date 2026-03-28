using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.Ports;

public interface IContractExtractor
{
    Task<PackIngredients> ExtractAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        CancellationToken cancellationToken = default);
}
