using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Cli;

internal sealed class EvidenceBundleWriter(JsonSerializerOptions jsonOptions)
{
    private const string ManifestFileName = "manifest.json";
    private const string ScanFileName = "scan.json";
    private const string ResolvedEntryFileName = "resolved-entry.json";
    private const string PackFileName = "pack.md";

    public async Task<string> WriteAsync(
        string evidenceRootDirectory,
        string workspaceRoot,
        Budget budget,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        ContextPack contextPack,
        string markdown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceRootDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentNullException.ThrowIfNull(budget);
        ArgumentNullException.ThrowIfNull(scanResult);
        ArgumentNullException.ThrowIfNull(resolvedEntry);
        ArgumentNullException.ThrowIfNull(contextPack);
        ArgumentNullException.ThrowIfNull(markdown);

        var scanJson = JsonSerializer.Serialize(CliJsonModels.FromScanResult(scanResult), jsonOptions);
        var resolvedEntryJson = JsonSerializer.Serialize(CliJsonModels.FromResolvedEntry(resolvedEntry), jsonOptions);
        var canonicalDirectoryName = BuildCanonicalDirectoryName(scanResult.GeneratedAtUtc, contextPack.Entry, contextPack.Goal, budget, workspaceRoot);
        var manifest = BuildManifest(
            workspaceRoot,
            canonicalDirectoryName,
            scanResult.GeneratedAtUtc,
            budget,
            resolvedEntry,
            contextPack);
        var manifestJson = JsonSerializer.Serialize(manifest, jsonOptions);
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ManifestFileName] = manifestJson,
            [ScanFileName] = scanJson,
            [ResolvedEntryFileName] = resolvedEntryJson,
            [PackFileName] = markdown
        };

        var fullRootPath = Path.GetFullPath(evidenceRootDirectory);
        Directory.CreateDirectory(fullRootPath);

        var targetDirectory = ResolveTargetDirectory(fullRootPath, canonicalDirectoryName, files);
        Directory.CreateDirectory(targetDirectory);

        foreach (var file in files)
        {
            var targetPath = Path.Combine(targetDirectory, file.Key);
            await File.WriteAllTextAsync(targetPath, file.Value).ConfigureAwait(false);
        }

        return targetDirectory;
    }

    private static EvidenceManifestDto BuildManifest(
        string workspaceRoot,
        string directoryName,
        DateTimeOffset generatedAtUtc,
        Budget budget,
        ResolvedEntry resolvedEntry,
        ContextPack contextPack) =>
        new(
            SchemaVersion: "rulixa.phase1.evidence.v1",
            DirectoryName: directoryName,
            WorkspaceRoot: NormalizePath(Path.GetFullPath(workspaceRoot))!,
            GeneratedAtUtc: generatedAtUtc.UtcDateTime.ToString("O"),
            Entry: contextPack.Entry.ToString(),
            Goal: contextPack.Goal,
            Budget: new EvidenceBudgetDto(
                budget.MaxFiles,
                budget.MaxTotalLines,
                budget.MaxSnippetsPerFile),
            ResolvedEntry: new EvidenceResolvedEntryDto(
                ResolvedKind: resolvedEntry.ResolvedKind.ToString().ToLowerInvariant(),
                ResolvedPath: NormalizePath(resolvedEntry.ResolvedPath),
                Symbol: resolvedEntry.Symbol,
                Confidence: resolvedEntry.Confidence.ToString().ToLowerInvariant()),
            Artifacts: new EvidenceArtifactsDto(
                ManifestFileName,
                ScanFileName,
                ResolvedEntryFileName,
                PackFileName));

    private static string ResolveTargetDirectory(
        string evidenceRootDirectory,
        string canonicalDirectoryName,
        IReadOnlyDictionary<string, string> files)
    {
        var canonicalDirectory = Path.Combine(evidenceRootDirectory, canonicalDirectoryName);
        if (!Directory.Exists(canonicalDirectory) || DirectoryMatches(canonicalDirectory, files))
        {
            return canonicalDirectory;
        }

        for (var revision = 2; revision < int.MaxValue; revision++)
        {
            var revisionDirectory = Path.Combine(evidenceRootDirectory, $"{canonicalDirectoryName}__r{revision}");
            if (!Directory.Exists(revisionDirectory) || DirectoryMatches(revisionDirectory, files))
            {
                return revisionDirectory;
            }
        }

        throw new InvalidOperationException("evidence directory could not be resolved.");
    }

    private static bool DirectoryMatches(string directoryPath, IReadOnlyDictionary<string, string> files)
    {
        foreach (var file in files)
        {
            var path = Path.Combine(directoryPath, file.Key);
            if (!File.Exists(path))
            {
                return false;
            }

            var currentContent = File.ReadAllText(path);
            if (!string.Equals(currentContent, file.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static string BuildCanonicalDirectoryName(
        DateTimeOffset generatedAtUtc,
        Entry entry,
        string goal,
        Budget budget,
        string workspaceRoot)
    {
        var identity = string.Join(
            "|",
            NormalizePath(Path.GetFullPath(workspaceRoot)),
            generatedAtUtc.UtcDateTime.ToString("O"),
            entry.ToString(),
            goal,
            budget.MaxFiles,
            budget.MaxTotalLines,
            budget.MaxSnippetsPerFile);
        var hash = ComputeHash(identity)[..12];
        var generatedAtToken = generatedAtUtc.UtcDateTime.ToString("yyyyMMddTHHmmssfffffffZ");
        var entryToken = Slugify(entry.Value, 24);
        return $"{generatedAtToken}-{entry.Kind.ToString().ToLowerInvariant()}-{entryToken}-{hash}";
    }

    private static string ComputeHash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string Slugify(string value, int maxLength)
    {
        var builder = new StringBuilder();
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString().Trim('-');
        if (slug.Length == 0)
        {
            return "entry";
        }

        return slug.Length <= maxLength ? slug : slug[..maxLength].TrimEnd('-');
    }

    private static string? NormalizePath(string? path) => path?.Replace('\\', '/');

    internal sealed record EvidenceManifestDto(
        string SchemaVersion,
        string DirectoryName,
        string WorkspaceRoot,
        string GeneratedAtUtc,
        string Entry,
        string Goal,
        EvidenceBudgetDto Budget,
        EvidenceResolvedEntryDto ResolvedEntry,
        EvidenceArtifactsDto Artifacts);

    internal sealed record EvidenceBudgetDto(
        int MaxFiles,
        int MaxTotalLines,
        int MaxSnippetsPerFile);

    internal sealed record EvidenceResolvedEntryDto(
        string ResolvedKind,
        string? ResolvedPath,
        string? Symbol,
        string Confidence);

    internal sealed record EvidenceArtifactsDto(
        string Manifest,
        string Scan,
        string ResolvedEntry,
        string Pack);
}
