namespace Rulixa.Cli;

internal static class EvidenceBundleConventions
{
    public const string SchemaVersion = "rulixa.phase1.evidence.v1";
    public const string ManifestFileName = "manifest.json";
    public const string ScanFileName = "scan.json";
    public const string ResolvedEntryFileName = "resolved-entry.json";
    public const string PackFileName = "pack.md";
}

internal sealed record EvidenceManifestDto(
    string SchemaVersion,
    string DirectoryName,
    string WorkspaceRoot,
    string GeneratedAtUtc,
    string Entry,
    string Goal,
    EvidenceBudgetDto Budget,
    EvidenceResolvedEntryDto ResolvedEntry,
    EvidenceSelectionSummaryDto SelectionSummary,
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

internal sealed record EvidenceSelectionSummaryDto(
    IReadOnlyList<EvidenceContractDto> Contracts,
    IReadOnlyList<EvidenceSelectedFileDto> SelectedFiles,
    IReadOnlyList<EvidenceSelectedSnippetDto> SelectedSnippets);

internal sealed record EvidenceContractDto(
    string Kind,
    string Title,
    string Summary);

internal sealed record EvidenceSelectedFileDto(
    string Path,
    string Reason,
    bool IsRequired,
    int LineCount);

internal sealed record EvidenceSelectedSnippetDto(
    string Path,
    string Reason,
    string Anchor,
    int StartLine,
    int EndLine,
    bool IsRequired);

internal sealed record EvidenceArtifactsDto(
    string Manifest,
    string Scan,
    string ResolvedEntry,
    string Pack);
