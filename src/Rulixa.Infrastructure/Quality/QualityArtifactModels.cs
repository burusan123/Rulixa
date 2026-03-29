namespace Rulixa.Infrastructure.Quality;

public sealed record QualityArtifact(
    string SchemaVersion,
    string SuiteName,
    string GeneratedAtUtc,
    IReadOnlyList<QualityCaseArtifact> Cases,
    QualityGateArtifact QualityGate,
    long? FirstUsefulMapTimeMs,
    int UnknownGuidanceCaseCount,
    int UnknownGuidanceItemCount,
    int UnknownGuidanceFamilyCount,
    int RepresentativeChainCount,
    int DegradedReasonCount);

public sealed record LocalQualityRunArtifact(
    string SchemaVersion,
    string RunId,
    string GeneratedAtUtc,
    IReadOnlyList<LocalQualitySuiteArtifact> Suites,
    IReadOnlyList<QualityCaseArtifact> Cases,
    QualityGateArtifact QualityGate,
    LocalQualityObservationSummary SyntheticSummary,
    LocalQualityObservationSummary OptionalSmokeSummary,
    LocalUnknownGuidanceSummaryArtifact UnknownGuidanceSummary,
    long? FirstUsefulMapTimeMs,
    int UnknownGuidanceCaseCount,
    int UnknownGuidanceItemCount,
    int UnknownGuidanceFamilyCount,
    int RepresentativeChainCount,
    int DegradedReasonCount,
    int TotalDegradedDiagnosticCount,
    IReadOnlyList<HandoffWarningArtifact> HandoffWarnings,
    IReadOnlyList<string> RelatedArtifacts);

public sealed record QualityCaseArtifact(
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
    long? FirstUsefulMapTimeMs,
    string? FailureReason,
    string? SkipReason,
    int DegradedDiagnosticCount,
    int RepresentativeChainCount,
    int DegradedReasonCount,
    IReadOnlyList<UnknownGuidanceArtifact> UnknownGuidance);

public sealed record UnknownGuidanceArtifact(
    string Code,
    string Family,
    int CandidateCount,
    string? FirstCandidate);

public sealed record LocalQualitySuiteArtifact(
    string Name,
    string Scope,
    bool IncludedInGate,
    IReadOnlyList<string> CaseIds,
    int TotalCases,
    int PassedCount,
    int FailedCount,
    int SkippedCount);

public sealed record LocalQualityObservationSummary(
    int TotalCases,
    int PassedCount,
    int FailedCount,
    int SkippedCount);

public sealed record LocalUnknownGuidanceSummaryArtifact(
    int CaseCount,
    int GuidanceItemCount,
    IReadOnlyList<string> Families,
    IReadOnlyList<string> FirstCandidates);

public sealed record HandoffWarningArtifact(
    string CaseId,
    string Category,
    string Message);

public sealed record QualityGateArtifact(
    bool Passed,
    double CrashFreeRate,
    double PackSuccessRate,
    double PartialPackRate,
    double DeterministicRate,
    double FalseConfidenceRate,
    IReadOnlyList<string> FailedChecks);
