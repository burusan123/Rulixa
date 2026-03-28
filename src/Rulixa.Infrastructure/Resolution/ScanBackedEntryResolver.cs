using Rulixa.Application.Ports;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Scanning;

namespace Rulixa.Infrastructure.Resolution;

public sealed class ScanBackedEntryResolver : IEntryResolver
{
    public Task<ResolvedEntry> ResolveAsync(Entry entry, WorkspaceScanResult scanResult, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = entry.Kind switch
        {
            EntryKind.File => ResolveFile(entry, scanResult),
            EntryKind.Symbol => ResolveSymbol(entry, scanResult),
            EntryKind.Auto => ResolveAuto(entry, scanResult),
            _ => ResolvedEntry.Unresolved(entry.ToString(), [])
        };

        return Task.FromResult(result);
    }

    private static ResolvedEntry ResolveFile(Entry entry, WorkspaceScanResult scanResult)
    {
        var normalizedInput = NormalizePath(entry.Value, scanResult.WorkspaceRoot);
        var match = scanResult.Files.FirstOrDefault(file =>
            string.Equals(file.Path, normalizedInput, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            return new ResolvedEntry(entry.ToString(), ResolvedEntryKind.File, match.Path, null, ConfidenceLevel.High, []);
        }

        var candidates = scanResult.Files
            .Where(file => file.Path.EndsWith('/' + Path.GetFileName(entry.Value), StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetFileName(file.Path), Path.GetFileName(entry.Value), StringComparison.OrdinalIgnoreCase))
            .Select(file => new ResolvedCandidate(CandidateKind.File, file.Path, null, "file-name-match"))
            .ToArray();

        return ResolvedEntry.Unresolved(entry.ToString(), candidates);
    }

    private static ResolvedEntry ResolveSymbol(Entry entry, WorkspaceScanResult scanResult)
    {
        var exact = scanResult.Symbols.FirstOrDefault(symbol =>
            string.Equals(symbol.QualifiedName, entry.Value, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return new ResolvedEntry(entry.ToString(), ResolvedEntryKind.Symbol, exact.FilePath, exact.QualifiedName, ConfidenceLevel.High, []);
        }

        var candidates = scanResult.Symbols
            .Where(symbol => string.Equals(symbol.DisplayName, entry.Value, StringComparison.OrdinalIgnoreCase))
            .Select(symbol => new ResolvedCandidate(CandidateKind.Symbol, symbol.FilePath, symbol.QualifiedName, "symbol-display-name-match"))
            .ToArray();

        return ResolvedEntry.Unresolved(entry.ToString(), candidates);
    }

    private static ResolvedEntry ResolveAuto(Entry entry, WorkspaceScanResult scanResult)
    {
        var fileCandidates = scanResult.Files
            .Where(file => string.Equals(Path.GetFileNameWithoutExtension(file.Path), entry.Value, StringComparison.OrdinalIgnoreCase)
                || file.Path.Contains(entry.Value, StringComparison.OrdinalIgnoreCase))
            .Select(file => new ResolvedCandidate(CandidateKind.File, file.Path, null, "auto-file-match"));

        var symbolCandidates = scanResult.Symbols
            .Where(symbol => string.Equals(symbol.DisplayName, entry.Value, StringComparison.OrdinalIgnoreCase)
                || symbol.QualifiedName.Contains(entry.Value, StringComparison.OrdinalIgnoreCase))
            .Select(symbol => new ResolvedCandidate(CandidateKind.Symbol, symbol.FilePath, symbol.QualifiedName, "auto-symbol-match"));

        var candidates = fileCandidates
            .Concat(symbolCandidates)
            .OrderBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (candidates.Length == 1)
        {
            var candidate = candidates[0];
            return candidate.Symbol is not null
                ? new ResolvedEntry(entry.ToString(), ResolvedEntryKind.Symbol, candidate.Path, candidate.Symbol, ConfidenceLevel.Medium, [])
                : new ResolvedEntry(entry.ToString(), ResolvedEntryKind.File, candidate.Path, null, ConfidenceLevel.Medium, []);
        }

        return ResolvedEntry.Unresolved(entry.ToString(), candidates);
    }

    private static string NormalizePath(string path, string workspaceRoot)
    {
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(workspaceRoot, path);
        return Path.GetRelativePath(workspaceRoot, fullPath).Replace('\\', '/');
    }
}
