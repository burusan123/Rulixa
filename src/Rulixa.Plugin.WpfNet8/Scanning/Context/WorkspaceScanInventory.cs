using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Scanning;

internal sealed record WorkspaceScanInventory(
    IReadOnlyDictionary<string, string> FileContents,
    IReadOnlyList<ScanFile> ScanFiles);
