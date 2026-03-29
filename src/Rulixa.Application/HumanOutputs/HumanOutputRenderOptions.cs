namespace Rulixa.Application.HumanOutputs;

public sealed record HumanOutputRenderOptions(
    string? EvidenceDirectory,
    string? CompareEvidenceReference = null);
