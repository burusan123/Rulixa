using System.Security.Cryptography;
using Rulixa.Application.Ports;

namespace Rulixa.Infrastructure.FileSystem;

public sealed class WorkspaceFileSystem : IWorkspaceFileSystem
{
    public Task<IReadOnlyList<string>> EnumerateFilesAsync(string workspaceRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var files = Directory
            .EnumerateFiles(workspaceRoot, "*", SearchOption.AllDirectories)
            .Where(static path => !IsIgnored(path))
            .ToArray();

        return Task.FromResult<IReadOnlyList<string>>(files);
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) =>
        File.ReadAllTextAsync(path, cancellationToken);

    public async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool IsIgnored(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/.git/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj_codex/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/.vs/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/TestResults/", StringComparison.OrdinalIgnoreCase);
    }
}
