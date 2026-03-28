namespace Rulixa.Application.Ports;

public interface IWorkspaceFileSystem
{
    Task<IReadOnlyList<string>> EnumerateFilesAsync(string workspaceRoot, CancellationToken cancellationToken = default);

    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

    Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken = default);
}
