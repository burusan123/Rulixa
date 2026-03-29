namespace Rulixa.Infrastructure.Quality;

public sealed record Phase5KpiArtifact(
    string SchemaVersion,
    string SuiteName,
    string GeneratedAtUtc,
    IReadOnlyList<Phase5KpiCaseArtifact> Cases,
    Phase5QualityGateArtifact QualityGate);

public sealed record Phase5KpiCaseArtifact(
    string CaseId,
    string CorpusName,
    string WorkspaceType,
    string Entry,
    string Goal,
    string Status,
    IReadOnlyList<string> Tags,
    bool? PackSuccess,
    bool? CrashFree,
    bool? PartialPack,
    bool? HasUnknownGuidance,
    bool? FalseConfidenceDetected,
    bool? Deterministic,
    long DurationMilliseconds,
    string? FailureReason,
    string? SkipReason,
    int DegradedDiagnosticCount,
    IReadOnlyList<Phase5UnknownGuidanceArtifact> UnknownGuidance);

public sealed record Phase5UnknownGuidanceArtifact(
    string Code,
    string Family,
    int CandidateCount,
    string? FirstCandidate);

public sealed record Phase5QualityGateArtifact(
    bool Passed,
    double CrashFreeRate,
    double PackSuccessRate,
    double PartialPackRate,
    double DeterministicRate,
    double FalseConfidenceRate,
    IReadOnlyList<string> FailedChecks);
