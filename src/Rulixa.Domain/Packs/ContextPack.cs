using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;

namespace Rulixa.Domain.Packs;

public sealed record ContextPack(
    string Goal,
    Entry Entry,
    ResolvedEntry ResolvedEntry,
    IReadOnlyList<Contract> Contracts,
    IReadOnlyList<IndexSection> Indexes,
    IReadOnlyList<SelectedSnippet> SelectedSnippets,
    IReadOnlyList<SelectedFile> SelectedFiles,
    IReadOnlyList<Diagnostic> Unknowns);

public sealed record Contract(
    ContractKind Kind,
    string Title,
    string Summary,
    IReadOnlyList<string> RelatedFilePaths,
    IReadOnlyList<string> RelatedSymbols);

public sealed record IndexSection(
    string Title,
    IReadOnlyList<string> Lines);

public sealed record SelectedFile(
    string Path,
    string Reason,
    int Priority,
    bool IsRequired,
    int LineCount);

public sealed record SelectedSnippet(
    string Path,
    string Reason,
    int Priority,
    bool IsRequired,
    string Anchor,
    int StartLine,
    int EndLine,
    string Content);

public sealed record FileSelectionCandidate(
    string Path,
    string Reason,
    int Priority,
    bool IsRequired);

public sealed record SnippetSelectionCandidate(
    string Path,
    string Reason,
    int Priority,
    bool IsRequired,
    string Anchor,
    int StartLine,
    int EndLine,
    string Content);

public sealed record PackIngredients(
    IReadOnlyList<Contract> Contracts,
    IReadOnlyList<IndexSection> Indexes,
    IReadOnlyList<SnippetSelectionCandidate> SnippetCandidates,
    IReadOnlyList<FileSelectionCandidate> FileCandidates,
    IReadOnlyList<Diagnostic> Unknowns);

public enum ContractKind
{
    Startup,
    DependencyInjection,
    ViewModelBinding,
    Navigation,
    Command,
    DialogActivation
}
