using Rulixa.Domain.Scanning;

namespace Rulixa.Application.Ports;

public interface IWorkspaceScanner
{
    Task<WorkspaceScanResult> ScanAsync(string workspaceRoot, CancellationToken cancellationToken = default);
}
