using Rulixa.Application.Ports;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.UseCases;

public sealed class BuildContextPackUseCase
{
    private readonly IContractExtractor contractExtractor;

    public BuildContextPackUseCase(IContractExtractor contractExtractor)
    {
        this.contractExtractor = contractExtractor ?? throw new ArgumentNullException(nameof(contractExtractor));
    }

    public async Task<ContextPack> ExecuteAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        Entry entry,
        ResolvedEntry resolvedEntry,
        string goal,
        Budget budget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scanResult);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(resolvedEntry);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(goal);
        ArgumentNullException.ThrowIfNull(budget);

        var ingredients = await contractExtractor
            .ExtractAsync(workspaceRoot, scanResult, resolvedEntry, goal, cancellationToken)
            .ConfigureAwait(false);

        return ContextPackFactory.Create(goal, entry, resolvedEntry, ingredients, scanResult, budget);
    }
}
