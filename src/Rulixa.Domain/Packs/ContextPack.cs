using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;

namespace Rulixa.Domain.Packs;

public sealed record ContextPack(
    string Goal,
    Entry Entry,
    ResolvedEntry ResolvedEntry,
    IReadOnlyList<Contract> Contracts,
    IReadOnlyList<IndexSection> Indexes,
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

public sealed record FileSelectionCandidate(
    string Path,
    string Reason,
    int Priority,
    bool IsRequired);

public sealed record PackIngredients(
    IReadOnlyList<Contract> Contracts,
    IReadOnlyList<IndexSection> Indexes,
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
