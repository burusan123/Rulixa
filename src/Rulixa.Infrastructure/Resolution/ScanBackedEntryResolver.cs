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
        var exactMatches = scanResult.Files
            .Where(file => string.Equals(file.Path, normalizedInput, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (exactMatches.Length == 1)
        {
            return new ResolvedEntry(entry.ToString(), ResolvedEntryKind.File, exactMatches[0].Path, null, ConfidenceLevel.High, []);
        }

        if (exactMatches.Length > 1)
        {
            return ResolvedEntry.Unresolved(
                entry.ToString(),
                exactMatches.Select(file => new ResolvedCandidate(CandidateKind.File, file.Path, null, "file-exact-match")).ToArray());
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
        var exactMatches = scanResult.Symbols
            .Where(symbol => string.Equals(symbol.QualifiedName, entry.Value, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (exactMatches.Length == 1)
        {
            var exact = exactMatches[0];
            return new ResolvedEntry(entry.ToString(), ResolvedEntryKind.Symbol, exact.FilePath, exact.QualifiedName, ConfidenceLevel.High, []);
        }

        if (exactMatches.Length > 1)
        {
            return ResolvedEntry.Unresolved(
                entry.ToString(),
                exactMatches.Select(symbol => new ResolvedCandidate(CandidateKind.Symbol, symbol.FilePath, symbol.QualifiedName, "symbol-qualified-name-match")).ToArray());
        }

        var candidates = scanResult.Symbols
            .Where(symbol => string.Equals(symbol.DisplayName, entry.Value, StringComparison.OrdinalIgnoreCase))
            .Select(symbol => new ResolvedCandidate(CandidateKind.Symbol, symbol.FilePath, symbol.QualifiedName, "symbol-display-name-match"))
            .ToArray();

        return ResolvedEntry.Unresolved(entry.ToString(), candidates);
    }

    private static ResolvedEntry ResolveAuto(Entry entry, WorkspaceScanResult scanResult)
    {
        var fileCandidates = FindFileNameCandidates(entry.Value, scanResult);
        if (fileCandidates.Length > 0)
        {
            return ResolveCandidates(entry, fileCandidates);
        }

        var classCandidates = FindClassCandidates(entry.Value, scanResult);
        if (classCandidates.Length > 0)
        {
            return ResolveCandidates(entry, classCandidates);
        }

        var viewConventionCandidates = FindViewConventionCandidates(entry.Value, scanResult);
        if (viewConventionCandidates.Length > 0)
        {
            return ResolveCandidates(entry, viewConventionCandidates);
        }

        var windowCandidates = FindWindowCandidates(entry.Value, scanResult);
        return ResolveCandidates(entry, windowCandidates);
    }

    private static ResolvedCandidate[] FindFileNameCandidates(string value, WorkspaceScanResult scanResult)
    {
        var fileName = Path.GetFileName(value);
        var baseName = Path.GetFileNameWithoutExtension(value);
        return scanResult.Files
            .Where(file =>
                string.Equals(Path.GetFileName(file.Path), fileName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetFileNameWithoutExtension(file.Path), value, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetFileNameWithoutExtension(file.Path), baseName, StringComparison.OrdinalIgnoreCase))
            .Select(file => new ResolvedCandidate(CandidateKind.File, file.Path, null, "auto-file-match"))
            .Distinct()
            .OrderBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ResolvedCandidate[] FindClassCandidates(string value, WorkspaceScanResult scanResult) =>
        scanResult.Symbols
            .Where(symbol => symbol.Kind == SymbolKind.Class)
            .Where(symbol => string.Equals(symbol.DisplayName, value, StringComparison.OrdinalIgnoreCase))
            .Select(symbol => new ResolvedCandidate(CandidateKind.Symbol, symbol.FilePath, symbol.QualifiedName, "auto-class-match"))
            .Distinct()
            .OrderBy(static candidate => candidate.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static ResolvedCandidate[] FindViewConventionCandidates(string value, WorkspaceScanResult scanResult)
    {
        var normalized = value.EndsWith("ViewModel", StringComparison.OrdinalIgnoreCase)
            ? value[..^"ViewModel".Length]
            : value.EndsWith("View", StringComparison.OrdinalIgnoreCase)
                ? value[..^"View".Length]
                : value;

        var candidates = new List<ResolvedCandidate>();
        foreach (var binding in scanResult.ViewModelBindings)
        {
            var viewName = Path.GetFileNameWithoutExtension(binding.ViewPath);
            var viewModelName = binding.ViewModelSymbol.Split('.').Last();
            var isMatch =
                string.Equals(viewName, $"{normalized}View", StringComparison.OrdinalIgnoreCase)
                || string.Equals(viewModelName, $"{normalized}ViewModel", StringComparison.OrdinalIgnoreCase);
            if (!isMatch)
            {
                continue;
            }

            candidates.Add(new ResolvedCandidate(CandidateKind.View, binding.ViewPath, null, "auto-view-convention-match"));
            candidates.Add(new ResolvedCandidate(CandidateKind.ViewModel, null, binding.ViewModelSymbol, "auto-viewmodel-convention-match"));
        }

        return candidates
            .Distinct()
            .OrderBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ResolvedCandidate[] FindWindowCandidates(string value, WorkspaceScanResult scanResult)
    {
        var normalized = value.EndsWith("Window", StringComparison.OrdinalIgnoreCase)
            ? value
            : $"{value}Window";
        return scanResult.Symbols
            .Where(symbol => symbol.Kind == SymbolKind.Window)
            .Where(symbol => string.Equals(symbol.DisplayName, value, StringComparison.OrdinalIgnoreCase)
                || string.Equals(symbol.DisplayName, normalized, StringComparison.OrdinalIgnoreCase))
            .Select(symbol => new ResolvedCandidate(CandidateKind.Window, symbol.FilePath, symbol.QualifiedName, "auto-window-match"))
            .Distinct()
            .OrderBy(static candidate => candidate.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ResolvedEntry ResolveCandidates(Entry entry, IReadOnlyList<ResolvedCandidate> candidates)
    {
        if (candidates.Count == 1)
        {
            var candidate = candidates[0];
            if (!string.IsNullOrWhiteSpace(candidate.Symbol))
            {
                return new ResolvedEntry(entry.ToString(), ResolvedEntryKind.Symbol, candidate.Path, candidate.Symbol, ConfidenceLevel.Medium, []);
            }

            if (!string.IsNullOrWhiteSpace(candidate.Path))
            {
                return new ResolvedEntry(entry.ToString(), ResolvedEntryKind.File, candidate.Path, null, ConfidenceLevel.Medium, []);
            }
        }

        return ResolvedEntry.Unresolved(entry.ToString(), candidates.ToArray());
    }

    private static string NormalizePath(string path, string workspaceRoot)
    {
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(workspaceRoot, path);
        return Path.GetRelativePath(workspaceRoot, fullPath).Replace('\\', '/');
    }
}
