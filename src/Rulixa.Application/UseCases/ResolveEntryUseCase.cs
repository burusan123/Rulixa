using Rulixa.Application.Ports;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.UseCases;

public sealed class ResolveEntryUseCase
{
    private readonly IEntryResolver entryResolver;

    public ResolveEntryUseCase(IEntryResolver entryResolver)
    {
        this.entryResolver = entryResolver ?? throw new ArgumentNullException(nameof(entryResolver));
    }

    public Task<ResolvedEntry> ExecuteAsync(
        Entry entry,
        WorkspaceScanResult scanResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(scanResult);

        return entryResolver.ResolveAsync(entry, scanResult, cancellationToken);
    }
}
