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
    int DegradedReasonCount,
    int HandoffHitCount,
    int HandoffMissCount,
    int HandoffUnknownCount);

public sealed record LocalQualityRunArtifact(
    string SchemaVersion,
    string RunId,
    string GeneratedAtUtc,
    IReadOnlyList<LocalQualitySuiteArtifact> Suites,
    IReadOnlyList<QualityCaseArtifact> Cases,
    QualityGateArtifact QualityGate,
    LocalQualityObservationSummary SyntheticSummary,
    LocalQualityObservationSummary OptionalSmokeSummary,
    IReadOnlyList<CorpusHandoffSummaryArtifact> SyntheticCorpusHandoffs,
    IReadOnlyList<CorpusHandoffSummaryArtifact> ObservedCorpusHandoffs,
    IReadOnlyList<CaseHandoffSummaryArtifact> MissOrUnknownCases,
    LocalUnknownGuidanceSummaryArtifact UnknownGuidanceSummary,
    LocalHandoffSummaryArtifact HandoffSummary,
    long? FirstUsefulMapTimeMs,
    int UnknownGuidanceCaseCount,
    int UnknownGuidanceItemCount,
    int UnknownGuidanceFamilyCount,
    int RepresentativeChainCount,
    int DegradedReasonCount,
    int TotalDegradedDiagnosticCount,
    IReadOnlyList<HandoffWarningArtifact> HandoffWarnings,
    PerformanceBaselineArtifact? PerformanceBaseline,
    IReadOnlyList<string> RelatedArtifacts);

public sealed record QualityCaseArtifact(
    string CaseId,
    string CorpusName,
    string CorpusCategory,
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
    IReadOnlyList<UnknownGuidanceArtifact> UnknownGuidance,
    string? HandoffOutcome = null,
    IReadOnlyList<string>? HandoffExpectedFamilies = null,
    IReadOnlyList<string>? HandoffObservedFamilies = null,
    string? HandoffFirstCandidate = null,
    string? HandoffReason = null);

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

public sealed record CorpusHandoffSummaryArtifact(
    string CorpusCategory,
    string Scope,
    int TotalCases,
    int HitCount,
    int MissCount,
    int UnknownCount);

public sealed record CaseHandoffSummaryArtifact(
    string CaseId,
    string CorpusCategory,
    string Scope,
    string Outcome,
    string? FirstCandidate,
    string? Reason);

public sealed record LocalHandoffSummaryArtifact(
    int HitCount,
    int MissCount,
    int UnknownCount,
    IReadOnlyList<string> ObservedFamilies,
    IReadOnlyList<string> FirstCandidates);

public sealed record HandoffWarningArtifact(
    string CaseId,
    string Category,
    string Message);

public sealed record PerformanceBaselineArtifact(
    MetricBaselineArtifact? FirstUsefulMapTimeMs,
    MetricBaselineArtifact? RepresentativeChainCount,
    MetricBaselineArtifact? UnknownGuidanceCaseCount,
    MetricBaselineArtifact? DegradedReasonCount,
    IReadOnlyList<CasePerformanceBaselineArtifact> CaseComparisons,
    IReadOnlyList<string> RegressionWarnings);

public sealed record MetricBaselineArtifact(
    long Current,
    long? Baseline,
    long? Delta,
    bool RegressionWarning);

public sealed record CasePerformanceBaselineArtifact(
    string CaseId,
    string CorpusCategory,
    string Entry,
    string Goal,
    MetricBaselineArtifact? FirstUsefulMapTimeMs,
    MetricBaselineArtifact? RepresentativeChainCount,
    MetricBaselineArtifact? UnknownGuidanceCaseCount,
    MetricBaselineArtifact? DegradedReasonCount,
    IReadOnlyList<string> RegressionWarnings);

public sealed record QualityGateArtifact(
    bool Passed,
    double CrashFreeRate,
    double PackSuccessRate,
    double PartialPackRate,
    double DeterministicRate,
    double FalseConfidenceRate,
    IReadOnlyList<string> FailedChecks);
