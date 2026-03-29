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
        IReadOnlyList<HumanOutputArtifactReference>? humanOutputs = null,
        IReadOnlyList<VisualOutputArtifactReference>? visualOutputs = null,
        string? releaseReviewPath = null,
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
        var runArtifact = BuildRunArtifact(
            runId,
            timestamp,
            suites,
            flattenedCases,
            gate,
            relatedArtifacts ?? [],
            humanOutputs ?? [],
            visualOutputs ?? [],
            releaseReviewPath);
        var performanceBaseline = await new QualityPerformanceBaselineComparer()
            .TryLoadAndCompareAsync(fullOutputRoot, runArtifact, cancellationToken)
            .ConfigureAwait(false);
        runArtifact = runArtifact with { PerformanceBaseline = performanceBaseline };

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
        IReadOnlyList<string> relatedArtifacts,
        IReadOnlyList<HumanOutputArtifactReference> humanOutputs,
        IReadOnlyList<VisualOutputArtifactReference> visualOutputs,
        string? releaseReviewPath)
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
            SyntheticCorpusHandoffs: BuildCorpusHandoffSummaries(syntheticCases, "synthetic"),
            ObservedCorpusHandoffs: BuildCorpusHandoffSummaries(optionalSmokeCases, "observed"),
            MissOrUnknownCases: BuildCaseHandoffSummaries(cases),
            UnknownGuidanceSummary: new LocalUnknownGuidanceSummaryArtifact(
                CaseCount: observation.UnknownGuidanceCaseCount,
                GuidanceItemCount: observation.UnknownGuidanceItemCount,
                Families: observation.Families,
                FirstCandidates: observation.FirstCandidates),
            HandoffSummary: new LocalHandoffSummaryArtifact(
                HitCount: observation.HandoffHitCount,
                MissCount: observation.HandoffMissCount,
                UnknownCount: observation.HandoffUnknownCount,
                ObservedFamilies: observation.HandoffFamilies,
                FirstCandidates: observation.HandoffFirstCandidates),
            FirstUsefulMapTimeMs: observation.FirstUsefulMapTimeMs,
            UnknownGuidanceCaseCount: observation.UnknownGuidanceCaseCount,
            UnknownGuidanceItemCount: observation.UnknownGuidanceItemCount,
            UnknownGuidanceFamilyCount: observation.UnknownGuidanceFamilyCount,
            RepresentativeChainCount: observation.RepresentativeChainCount,
            DegradedReasonCount: observation.DegradedReasonCount,
            TotalDegradedDiagnosticCount: cases.Sum(static item => item.DegradedDiagnosticCount),
            HandoffWarnings: handoffWarnings,
            PerformanceBaseline: null,
            HumanOutputs: humanOutputs
                .OrderBy(static item => item.Mode, StringComparer.Ordinal)
                .ThenBy(static item => item.CorpusCategory, StringComparer.Ordinal)
                .ThenBy(static item => item.CaseId, StringComparer.Ordinal)
                .ToArray(),
            VisualOutputs: visualOutputs
                .OrderBy(static item => item.CorpusCategory, StringComparer.Ordinal)
                .ThenBy(static item => item.CaseId, StringComparer.Ordinal)
                .ThenBy(static item => item.Path, StringComparer.Ordinal)
                .ToArray(),
            ReleaseReviewPath: releaseReviewPath,
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
        AppendCorpusHandoffSummaries(builder, artifact.SyntheticCorpusHandoffs);

        builder.AppendLine();
        builder.AppendLine("## Observed Corpus");
        builder.AppendLine();
        AppendObservation(builder, artifact.OptionalSmokeSummary);
        AppendCorpusHandoffSummaries(builder, artifact.ObservedCorpusHandoffs);
        foreach (var smokeCase in artifact.Cases
                     .Where(static item => item.Tags.Contains("optional-smoke", StringComparer.OrdinalIgnoreCase))
                     .OrderBy(static item => item.CorpusCategory, StringComparer.Ordinal)
                     .ThenBy(static item => item.CaseId, StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{smokeCase.CorpusCategory}` / `{smokeCase.CaseId}`: `{smokeCase.Status}`");
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
        builder.AppendLine($"- hit: `{artifact.HandoffSummary.HitCount}`");
        builder.AppendLine($"- miss: `{artifact.HandoffSummary.MissCount}`");
        builder.AppendLine($"- unknown: `{artifact.HandoffSummary.UnknownCount}`");
        builder.AppendLine($"- representative_chains: `{artifact.RepresentativeChainCount}`");
        builder.AppendLine($"- first_useful_map_time_ms: `{artifact.FirstUsefulMapTimeMs?.ToString() ?? "none"}`");
        builder.AppendLine($"- observed_families: `{FormatInlineList(artifact.HandoffSummary.ObservedFamilies)}`");
        builder.AppendLine($"- next_candidates: `{FormatInlineList(artifact.HandoffSummary.FirstCandidates)}`");
        builder.AppendLine($"- handoff_warnings: `{artifact.HandoffWarnings.Count}`");
        foreach (var warning in artifact.HandoffWarnings)
        {
            builder.AppendLine($"  `{warning.CaseId}` [{warning.Category}] {warning.Message}");
        }

        builder.AppendLine();
        builder.AppendLine("## Release Review");
        builder.AppendLine();
        builder.AppendLine("- `summary.md` を一次資料とし、`release-review.md`、`human-outputs/`、`visual-outputs/` を補助資料として読む。");
        if (artifact.HumanOutputs.Count == 0)
        {
            builder.AppendLine("- human_outputs: none");
        }
        else
        {
            foreach (var humanOutput in artifact.HumanOutputs)
            {
                builder.AppendLine(
                    $"- `{humanOutput.Mode}` / `{humanOutput.CorpusCategory}` / `{humanOutput.CaseId}`: `{humanOutput.Path}`");
            }
        }

        if (artifact.VisualOutputs.Count == 0)
        {
            builder.AppendLine("- visual_outputs: none");
        }
        else
        {
            foreach (var visualOutput in artifact.VisualOutputs)
            {
                builder.AppendLine(
                    $"- visual / `{visualOutput.CorpusCategory}` / `{visualOutput.CaseId}`: `{visualOutput.Path}`");
            }
        }

        builder.AppendLine(
            !string.IsNullOrWhiteSpace(artifact.ReleaseReviewPath)
                ? $"- release_review: `{artifact.ReleaseReviewPath}`"
                : "- release_review: none");

        builder.AppendLine();
        builder.AppendLine("## Case Handoff Details");
        builder.AppendLine();
        if (artifact.MissOrUnknownCases.Count == 0)
        {
            builder.AppendLine("- miss_or_unknown_cases: none");
        }
        else
        {
            foreach (var item in artifact.MissOrUnknownCases)
            {
                builder.AppendLine(
                    $"- `{item.CorpusCategory}` / `{item.CaseId}` outcome=`{item.Outcome}` first_candidate=`{item.FirstCandidate ?? "none"}` reason=`{item.Reason ?? "none"}`");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Performance Baseline");
        builder.AppendLine();
        if (artifact.PerformanceBaseline is null)
        {
            builder.AppendLine("- baseline: `none`");
        }
        else
        {
            AppendBaselineMetric(builder, "first_useful_map_time_ms", artifact.PerformanceBaseline.FirstUsefulMapTimeMs);
            AppendBaselineMetric(builder, "representative_chain_count", artifact.PerformanceBaseline.RepresentativeChainCount);
            AppendBaselineMetric(builder, "unknown_guidance_case_count", artifact.PerformanceBaseline.UnknownGuidanceCaseCount);
            AppendBaselineMetric(builder, "degraded_reason_count", artifact.PerformanceBaseline.DegradedReasonCount);
            builder.AppendLine(
                artifact.PerformanceBaseline.RegressionWarnings.Count > 0
                    ? $"- regression_warnings: `{string.Join("`, `", artifact.PerformanceBaseline.RegressionWarnings)}`"
                    : "- regression_warnings: none");
            if (artifact.PerformanceBaseline.CaseComparisons.Count == 0)
            {
                builder.AppendLine("- case_comparisons: none");
            }
            else
            {
                builder.AppendLine("- case_comparisons:");
                foreach (var item in artifact.PerformanceBaseline.CaseComparisons)
                {
                    builder.AppendLine(
                        $"  `{item.CorpusCategory}` / `{item.CaseId}` time_delta=`{FormatMetricDelta(item.FirstUsefulMapTimeMs)}` representative_delta=`{FormatMetricDelta(item.RepresentativeChainCount)}` unknown_delta=`{FormatMetricDelta(item.UnknownGuidanceCaseCount)}` degraded_delta=`{FormatMetricDelta(item.DegradedReasonCount)}` warnings=`{FormatInlineList(item.RegressionWarnings)}`");
                }
            }
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

    private static void AppendCorpusHandoffSummaries(
        StringBuilder builder,
        IReadOnlyList<CorpusHandoffSummaryArtifact> summaries)
    {
        if (summaries.Count == 0)
        {
            builder.AppendLine("- corpus_handoff: none");
            return;
        }

        foreach (var summary in summaries)
        {
            builder.AppendLine(
                $"- `{summary.CorpusCategory}` hit=`{summary.HitCount}` miss=`{summary.MissCount}` unknown=`{summary.UnknownCount}` total=`{summary.TotalCases}`");
        }
    }

    private static string FormatInlineList(IReadOnlyCollection<string> values) =>
        values.Count == 0 ? "none" : string.Join("`, `", values);

    private static string FormatMetricDelta(MetricBaselineArtifact? metric) =>
        metric?.Delta?.ToString() ?? "none";

    private static void AppendBaselineMetric(StringBuilder builder, string name, MetricBaselineArtifact? metric)
    {
        if (metric is null)
        {
            builder.AppendLine($"- {name}: `none`");
            return;
        }

        var deltaText = metric.Delta?.ToString() ?? "none";
        builder.AppendLine(
            $"- {name}: current=`{metric.Current}` baseline=`{metric.Baseline?.ToString() ?? "none"}` delta=`{deltaText}` regression_warning=`{metric.RegressionWarning}`");
    }

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

    private static IReadOnlyList<CorpusHandoffSummaryArtifact> BuildCorpusHandoffSummaries(
        IReadOnlyCollection<QualityCaseArtifact> cases,
        string scope) =>
        cases.GroupBy(static item => item.CorpusCategory, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .Select(group => new CorpusHandoffSummaryArtifact(
                CorpusCategory: group.Key,
                Scope: scope,
                TotalCases: group.Count(),
                HitCount: group.Count(static item => string.Equals(item.HandoffOutcome, "hit", StringComparison.OrdinalIgnoreCase)),
                MissCount: group.Count(static item => string.Equals(item.HandoffOutcome, "miss", StringComparison.OrdinalIgnoreCase)),
                UnknownCount: group.Count(static item => string.Equals(item.HandoffOutcome, "unknown", StringComparison.OrdinalIgnoreCase))))
            .ToArray();

    private static IReadOnlyList<CaseHandoffSummaryArtifact> BuildCaseHandoffSummaries(
        IReadOnlyCollection<QualityCaseArtifact> cases) =>
        cases.Where(static item => item.HandoffOutcome is "miss" or "unknown")
            .OrderBy(static item => item.CorpusCategory, StringComparer.Ordinal)
            .ThenBy(static item => item.CaseId, StringComparer.Ordinal)
            .Select(item => new CaseHandoffSummaryArtifact(
                CaseId: item.CaseId,
                CorpusCategory: item.CorpusCategory,
                Scope: item.Tags.Contains("optional-smoke", StringComparer.OrdinalIgnoreCase) ? "observed" : "synthetic",
                Outcome: item.HandoffOutcome ?? "unknown",
                FirstCandidate: item.HandoffFirstCandidate,
                Reason: item.HandoffReason))
            .ToArray();
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
