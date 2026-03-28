using Rulixa.Application.Ports;
using Rulixa.Domain.Scanning;
using Rulixa.Plugin.WpfNet8.Discovery;

namespace Rulixa.Plugin.WpfNet8.Scanning;

internal sealed class WorkspaceScanInventoryBuilder
{
    private readonly IWorkspaceFileSystem fileSystem;

    internal WorkspaceScanInventoryBuilder(IWorkspaceFileSystem fileSystem)
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    internal async Task<WorkspaceScanInventory> BuildAsync(
        string workspaceRoot,
        CancellationToken cancellationToken)
    {
        var allFiles = await fileSystem.EnumerateFilesAsync(workspaceRoot, cancellationToken).ConfigureAwait(false);
        var relevantFiles = allFiles
            .Select(path => Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/'))
            .Where(static path => IsRelevant(path))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var fileContents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var scanFiles = new List<ScanFile>();
        DateTimeOffset? latestLastWriteTimeUtc = null;

        foreach (var relativePath in relevantFiles)
        {
            var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var content = await fileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
            fileContents[relativePath] = content;

            var kind = ProjectFileDiscovery.DetectKind(relativePath);
            var hash = await fileSystem.ComputeSha256Async(absolutePath, cancellationToken).ConfigureAwait(false);
            var lastWriteTimeUtc = await fileSystem.GetLastWriteTimeUtcAsync(absolutePath, cancellationToken).ConfigureAwait(false);
            latestLastWriteTimeUtc = latestLastWriteTimeUtc is null || lastWriteTimeUtc > latestLastWriteTimeUtc
                ? lastWriteTimeUtc
                : latestLastWriteTimeUtc;
            scanFiles.Add(new ScanFile(
                relativePath,
                kind,
                ResolveProjectName(relativePath),
                hash,
                CountLines(content),
                ProjectFileDiscovery.DetectTags(relativePath, kind)));
        }

        return new WorkspaceScanInventory(
            fileContents,
            scanFiles,
            latestLastWriteTimeUtc ?? DateTimeOffset.UnixEpoch);
    }

    private static bool IsRelevant(string relativePath) =>
        relativePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
        || relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
        || relativePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
        || relativePath.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)
        || relativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
        || relativePath.EndsWith(".props", StringComparison.OrdinalIgnoreCase);

    private static int CountLines(string content) => content.Split('\n').Length;

    private static string ResolveProjectName(string relativePath)
    {
        var parts = relativePath.Split('/');
        return parts.Length >= 2 ? parts[1] : parts[0];
    }
}
