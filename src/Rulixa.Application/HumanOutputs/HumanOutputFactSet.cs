namespace Rulixa.Application.HumanOutputs;

internal sealed record HumanOutputFactSet(
    string Entry,
    string ResolvedKind,
    string? ResolvedPath,
    string? ResolvedSymbol,
    string Goal,
    string? SystemSummary,
    IReadOnlyList<string> CenterStates,
    IReadOnlyList<string> WorkflowLines,
    IReadOnlyList<string> PersistenceLines,
    IReadOnlyList<string> ExternalAssetLines,
    IReadOnlyList<string> ObservedFacts,
    IReadOnlyList<string> EvidenceSources,
    IReadOnlyList<string> DependencySeams,
    IReadOnlyList<string> ArchitecturalConstraints,
    IReadOnlyList<string> KnownUnknowns,
    IReadOnlyList<string> RiskLines,
    IReadOnlyList<string> NextCandidates,
    int DegradedDiagnosticCount,
    int RepresentativeChainCount,
    string? EvidenceDirectory,
    string? CompareEvidenceReference);
