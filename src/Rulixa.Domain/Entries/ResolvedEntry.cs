namespace Rulixa.Domain.Entries;

public sealed record ResolvedEntry(
    string Input,
    ResolvedEntryKind ResolvedKind,
    string? ResolvedPath,
    string? Symbol,
    ConfidenceLevel Confidence,
    IReadOnlyList<ResolvedCandidate> Candidates)
{
    public static ResolvedEntry Unresolved(string input, IReadOnlyList<ResolvedCandidate> candidates) =>
        new(input, ResolvedEntryKind.Unresolved, null, null, candidates.Count == 0 ? ConfidenceLevel.Low : ConfidenceLevel.Medium, candidates);
}

public sealed record ResolvedCandidate(
    CandidateKind Kind,
    string? Path,
    string? Symbol,
    string Reason);

public enum ResolvedEntryKind
{
    File,
    Symbol,
    Unresolved
}

public enum CandidateKind
{
    File,
    Symbol,
    View,
    ViewModel,
    Window,
    Service
}

public enum ConfidenceLevel
{
    Low,
    Medium,
    High
}
