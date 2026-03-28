using Rulixa.Application.Ports;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.UseCases;

public sealed class ScanWorkspaceUseCase
{
    private readonly IWorkspaceScanner workspaceScanner;

    public ScanWorkspaceUseCase(IWorkspaceScanner workspaceScanner)
    {
        this.workspaceScanner = workspaceScanner ?? throw new ArgumentNullException(nameof(workspaceScanner));
    }

    public Task<WorkspaceScanResult> ExecuteAsync(string workspaceRoot, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            throw new ArgumentException("workspaceRoot は必須です。", nameof(workspaceRoot));
        }

        return workspaceScanner.ScanAsync(workspaceRoot, cancellationToken);
    }
}
