using Rulixa.Domain.Entries;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.Ports;

public interface IEntryResolver
{
    Task<ResolvedEntry> ResolveAsync(Entry entry, WorkspaceScanResult scanResult, CancellationToken cancellationToken = default);
}
