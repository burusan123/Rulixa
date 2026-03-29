using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rulixa.Infrastructure.Quality;

public sealed class LocalQualityGateRunWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<LocalQualityGateWriteResult> WriteAsync(
        string qualityOutputRoot,
        string runId,
        IReadOnlyList<LocalQualitySuiteInput> suites,
        IReadOnlyList<string>? relatedArtifacts = null,
        DateTimeOffset? generatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualityOutputRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(suites);

        var timestamp = generatedAtUtc ?? DateTimeOffset.UtcNow;
        var fullOutputRoot = Path.GetFullPath(qualityOutputRoot);
        var runDirectory = Path.Combine(fullOutputRoot, runId);
        Directory.CreateDirectory(runDirectory);

        var flattenedCases = suites
            .SelectMany(static suite => suite.Cases)
            .OrderBy(static item => item.CorpusName, StringComparer.Ordinal)
            .ThenBy(static item => item.CaseId, StringComparer.Ordinal)
            .ToArray();
        var gateCases = suites
            .Where(static suite => suite.IncludedInGate)
            .SelectMany(static suite => suite.Cases)
            .ToArray();

        var gate = new QualityGateEvaluator().Evaluate(gateCases);
        var runArtifact = BuildRunArtifact(runId, timestamp, suites, flattenedCases, gate, relatedArtifacts ?? []);

        var kpiPath = Path.Combine(runDirectory, "kpi.json");
        var gatePath = Path.Combine(runDirectory, "gate.json");
        var summaryPath = Path.Combine(runDirectory, "summary.md");

        await File.WriteAllTextAsync(kpiPath, JsonSerializer.Serialize(runArtifact, JsonOptions), cancellationToken)
            .ConfigureAwait(false);
        await File.WriteAllTextAsync(gatePath, JsonSerializer.Serialize(gate, JsonOptions), cancellationToken)
            .ConfigureAwait(false);
        await File.WriteAllTextAsync(summaryPath, BuildSummary(runArtifact), cancellationToken)
            .ConfigureAwait(false);

        var latestDirectory = Path.Combine(fullOutputRoot, "latest");
        ReplaceLatestDirectory(latestDirectory, kpiPath, gatePath, summaryPath);
        await File.WriteAllTextAsync(Path.Combine(fullOutputRoot, "latest.txt"), runId, cancellationToken)
            .ConfigureAwait(false);

        return new LocalQualityGateWriteResult(runDirectory, kpiPath, gatePath, summaryPath, latestDirectory);
    }

    private static LocalQualityRunArtifact BuildRunArtifact(
        string runId,
        DateTimeOffset timestamp,
        IReadOnlyList<LocalQualitySuiteInput> suites,
        IReadOnlyList<QualityCaseArtifact> cases,
        QualityGateArtifact gate,
        IReadOnlyList<string> relatedArtifacts)
    {
        var observation = new QualityObservationCalculator().Calculate(cases);
        var handoffWarnings = new QualityHandoffWarningEvaluator().Evaluate(cases);
        var suiteArtifacts = suites
            .Select(static suite => new LocalQualitySuiteArtifact(
                Name: suite.Name,
                Scope: suite.Scope,
                IncludedInGate: suite.IncludedInGate,
                CaseIds: suite.Cases.Select(static item => item.CaseId).ToArray(),
                TotalCases: suite.Cases.Count,
                PassedCount: suite.Cases.Count(static item => string.Equals(item.Status, "passed", StringComparison.OrdinalIgnoreCase)),
                FailedCount: suite.Cases.Count(static item => string.Equals(item.Status, "failed", StringComparison.OrdinalIgnoreCase)),
                SkippedCount: suite.Cases.Count(static item => string.Equals(item.Status, "skipped", StringComparison.OrdinalIgnoreCase))))
            .ToArray();

        var syntheticCases = suites
            .Where(static suite => suite.IncludedInGate)
            .SelectMany(static suite => suite.Cases)
            .ToArray();
        var optionalSmokeCases = suites
            .Where(static suite => string.Equals(suite.Scope, "optional-smoke", StringComparison.OrdinalIgnoreCase))
            .SelectMany(static suite => suite.Cases)
            .ToArray();

        return new LocalQualityRunArtifact(
            SchemaVersion: QualityArtifactConventions.RunSchemaVersion,
            RunId: runId,
            GeneratedAtUtc: timestamp.UtcDateTime.ToString("O"),
            Suites: suiteArtifacts,
            Cases: cases,
            QualityGate: gate,
            SyntheticSummary: BuildObservationSummary(syntheticCases),
            OptionalSmokeSummary: BuildObservationSummary(optionalSmokeCases),
            UnknownGuidanceSummary: new LocalUnknownGuidanceSummaryArtifact(
                CaseCount: observation.UnknownGuidanceCaseCount,
                GuidanceItemCount: observation.UnknownGuidanceItemCount,
                Families: observation.Families,
                FirstCandidates: observation.FirstCandidates),
            FirstUsefulMapTimeMs: observation.FirstUsefulMapTimeMs,
            UnknownGuidanceCaseCount: observation.UnknownGuidanceCaseCount,
            UnknownGuidanceItemCount: observation.UnknownGuidanceItemCount,
            UnknownGuidanceFamilyCount: observation.UnknownGuidanceFamilyCount,
            RepresentativeChainCount: observation.RepresentativeChainCount,
            DegradedReasonCount: observation.DegradedReasonCount,
            TotalDegradedDiagnosticCount: cases.Sum(static item => item.DegradedDiagnosticCount),
            HandoffWarnings: handoffWarnings,
            RelatedArtifacts: relatedArtifacts.OrderBy(static item => item, StringComparer.Ordinal).ToArray());
    }

    private static LocalQualityObservationSummary BuildObservationSummary(IReadOnlyCollection<QualityCaseArtifact> cases) =>
        new(
            TotalCases: cases.Count,
            PassedCount: cases.Count(static item => string.Equals(item.Status, "passed", StringComparison.OrdinalIgnoreCase)),
            FailedCount: cases.Count(static item => string.Equals(item.Status, "failed", StringComparison.OrdinalIgnoreCase)),
            SkippedCount: cases.Count(static item => string.Equals(item.Status, "skipped", StringComparison.OrdinalIgnoreCase)));

    private static string BuildSummary(LocalQualityRunArtifact artifact)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Local Quality Gate");
        builder.AppendLine();
        builder.AppendLine($"- run id: `{artifact.RunId}`");
        builder.AppendLine($"- generated at (UTC): `{artifact.GeneratedAtUtc}`");
        builder.AppendLine($"- gate: `{(artifact.QualityGate.Passed ? "passed" : "failed")}`");
        builder.AppendLine();
        builder.AppendLine("## Gate");
        builder.AppendLine();
        builder.AppendLine($"- crash_free_rate: `{artifact.QualityGate.CrashFreeRate:P0}`");
        builder.AppendLine($"- pack_success_rate: `{artifact.QualityGate.PackSuccessRate:P0}`");
        builder.AppendLine($"- deterministic_rate: `{artifact.QualityGate.DeterministicRate:P0}`");
        builder.AppendLine($"- false_confidence_rate: `{artifact.QualityGate.FalseConfidenceRate:P0}`");
        builder.AppendLine($"- partial_pack_rate: `{artifact.QualityGate.PartialPackRate:P0}`");
        builder.AppendLine(
            artifact.QualityGate.FailedChecks.Count > 0
                ? $"- failed_checks: `{string.Join("`, `", artifact.QualityGate.FailedChecks)}`"
                : "- failed_checks: none");

        builder.AppendLine();
        builder.AppendLine("## Synthetic Corpus");
        builder.AppendLine();
        AppendObservation(builder, artifact.SyntheticSummary);

        builder.AppendLine();
        builder.AppendLine("## Optional Smoke");
        builder.AppendLine();
        AppendObservation(builder, artifact.OptionalSmokeSummary);
        foreach (var smokeCase in artifact.Cases
                     .Where(static item => item.Tags.Contains("optional-smoke", StringComparer.OrdinalIgnoreCase))
                     .OrderBy(static item => item.CaseId, StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{smokeCase.CaseId}`: `{smokeCase.Status}`");
            if (!string.IsNullOrWhiteSpace(smokeCase.SkipReason))
            {
                builder.AppendLine($"  skip reason: `{smokeCase.SkipReason}`");
            }
            else if (!string.IsNullOrWhiteSpace(smokeCase.FailureReason))
            {
                builder.AppendLine($"  failure: `{smokeCase.FailureReason}`");
            }
        }

        var failedSmoke = artifact.Cases.Any(static item =>
            item.Tags.Contains("optional-smoke", StringComparer.OrdinalIgnoreCase)
            && string.Equals(item.Status, "failed", StringComparison.OrdinalIgnoreCase));
        if (failedSmoke)
        {
            builder.AppendLine();
            builder.AppendLine("> warning: optional smoke failed. gate result is unchanged because smoke is observation-only.");
        }

        builder.AppendLine();
        builder.AppendLine("## Handoff Observations");
        builder.AppendLine();
        builder.AppendLine("- synthetic corpus is the handoff quality baseline. optional smoke remains observation-only.");
        builder.AppendLine($"- representative_chains: `{artifact.RepresentativeChainCount}`");
        builder.AppendLine($"- first_useful_map_time_ms: `{artifact.FirstUsefulMapTimeMs?.ToString() ?? "none"}`");
        builder.AppendLine($"- unknown_guidance_families: `{FormatInlineList(artifact.UnknownGuidanceSummary.Families)}`");
        builder.AppendLine($"- next_candidates: `{FormatInlineList(artifact.UnknownGuidanceSummary.FirstCandidates)}`");
        builder.AppendLine($"- handoff_warnings: `{artifact.HandoffWarnings.Count}`");
        foreach (var warning in artifact.HandoffWarnings)
        {
            builder.AppendLine($"  `{warning.CaseId}` [{warning.Category}] {warning.Message}");
        }

        builder.AppendLine();
        builder.AppendLine("## Unknown Guidance Details");
        builder.AppendLine();
        builder.AppendLine($"- cases_with_guidance: `{artifact.UnknownGuidanceCaseCount}`");
        builder.AppendLine($"- guidance_items: `{artifact.UnknownGuidanceItemCount}`");
        builder.AppendLine($"- family_count: `{artifact.UnknownGuidanceFamilyCount}`");
        foreach (var item in artifact.Cases
                     .Where(static item => item.UnknownGuidance.Count > 0)
                     .OrderBy(static item => item.CaseId, StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{item.CaseId}`");
            foreach (var guidance in item.UnknownGuidance.OrderBy(static guidance => guidance.Code, StringComparer.Ordinal))
            {
                builder.AppendLine(
                    $"  `{guidance.Code}` family=`{guidance.Family}` candidates=`{guidance.CandidateCount}` first=`{guidance.FirstCandidate ?? "none"}`");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Degraded Diagnostics");
        builder.AppendLine();
        builder.AppendLine($"- degraded_diagnostics: `{artifact.TotalDegradedDiagnosticCount}`");
        builder.AppendLine($"- degraded_reasons: `{artifact.DegradedReasonCount}`");

        builder.AppendLine();
        builder.AppendLine("## Deterministic / False Confidence");
        builder.AppendLine();
        var deterministicCases = artifact.Cases.Count(static item => item.Tags.Contains("deterministic", StringComparer.OrdinalIgnoreCase));
        var deterministicPassed = artifact.Cases.Count(static item =>
            item.Tags.Contains("deterministic", StringComparer.OrdinalIgnoreCase) && item.Deterministic == true);
        var weakSignalCases = artifact.Cases.Count(static item => item.Tags.Contains("weak-signal", StringComparer.OrdinalIgnoreCase));
        var falseConfidenceDetected = artifact.Cases.Count(static item =>
            item.Tags.Contains("weak-signal", StringComparer.OrdinalIgnoreCase) && item.FalseConfidenceDetected == true);
        builder.AppendLine($"- deterministic_cases: `{deterministicPassed}/{deterministicCases}`");
        builder.AppendLine($"- false_confidence_detected: `{falseConfidenceDetected}/{weakSignalCases}`");

        builder.AppendLine();
        builder.AppendLine("## Related Artifacts");
        builder.AppendLine();
        builder.AppendLine("- `kpi.json`");
        builder.AppendLine("- `gate.json`");
        builder.AppendLine("- `summary.md`");
        foreach (var path in artifact.RelatedArtifacts)
        {
            builder.AppendLine($"- `{path}`");
        }

        return builder.ToString();
    }

    private static void AppendObservation(StringBuilder builder, LocalQualityObservationSummary summary)
    {
        builder.AppendLine($"- total: `{summary.TotalCases}`");
        builder.AppendLine($"- passed: `{summary.PassedCount}`");
        builder.AppendLine($"- failed: `{summary.FailedCount}`");
        builder.AppendLine($"- skipped: `{summary.SkippedCount}`");
    }

    private static string FormatInlineList(IReadOnlyCollection<string> values) =>
        values.Count == 0 ? "none" : string.Join("`, `", values);

    private static void ReplaceLatestDirectory(string latestDirectory, params string[] sourceFiles)
    {
        if (Directory.Exists(latestDirectory))
        {
            Directory.Delete(latestDirectory, recursive: true);
        }

        Directory.CreateDirectory(latestDirectory);
        foreach (var sourceFile in sourceFiles)
        {
            var targetFile = Path.Combine(latestDirectory, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, targetFile, overwrite: true);
        }
    }
}

public sealed record LocalQualitySuiteInput(
    string Name,
    string Scope,
    bool IncludedInGate,
    IReadOnlyList<QualityCaseArtifact> Cases);

public sealed record LocalQualityGateWriteResult(
    string RunDirectory,
    string KpiPath,
    string GatePath,
    string SummaryPath,
    string LatestDirectory);
