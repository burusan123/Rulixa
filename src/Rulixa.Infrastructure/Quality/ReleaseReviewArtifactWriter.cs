using System.Text;

namespace Rulixa.Infrastructure.Quality;

public sealed class ReleaseReviewArtifactWriter
{
    public async Task<string> WriteAsync(
        string runDirectory,
        LocalQualityRunArtifact runArtifact,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runDirectory);
        ArgumentNullException.ThrowIfNull(runArtifact);

        Directory.CreateDirectory(runDirectory);
        var path = Path.Combine(runDirectory, "release-review.md");
        await File.WriteAllTextAsync(path, BuildMarkdown(runArtifact), cancellationToken).ConfigureAwait(false);
        return path;
    }

    internal static string BuildMarkdown(LocalQualityRunArtifact artifact)
    {
        var builder = new StringBuilder();
        var humanOutputLookup = BuildHumanOutputLookup(artifact.HumanOutputs);
        var visualOutputLookup = BuildVisualOutputLookup(artifact.VisualOutputs);

        builder.AppendLine("# Release Review");
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
        builder.AppendLine(
            artifact.QualityGate.FailedChecks.Count > 0
                ? $"- failed_checks: `{string.Join("`, `", artifact.QualityGate.FailedChecks)}`"
                : "- failed_checks: none");
        builder.AppendLine();

        builder.AppendLine("## Handoff");
        builder.AppendLine();
        builder.AppendLine($"- synthetic hit/miss/unknown: `{artifact.HandoffSummary.HitCount}/{artifact.HandoffSummary.MissCount}/{artifact.HandoffSummary.UnknownCount}`");
        builder.AppendLine($"- observed families: `{FormatInlineList(artifact.HandoffSummary.ObservedFamilies)}`");
        builder.AppendLine($"- next candidates: `{FormatInlineList(artifact.HandoffSummary.FirstCandidates)}`");
        builder.AppendLine(
            artifact.HandoffWarnings.Count > 0
                ? $"- warnings: `{string.Join("`, `", artifact.HandoffWarnings.Select(static warning => $"{warning.CaseId}:{warning.Category}"))}`"
                : "- warnings: none");
        builder.AppendLine();

        builder.AppendLine("## Handoff Follow-ups");
        builder.AppendLine();
        if (artifact.MissOrUnknownCases.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            foreach (var item in artifact.MissOrUnknownCases)
            {
                builder.AppendLine(
                    $"- `{item.Scope}` / `{item.CorpusCategory}` / `{item.CaseId}` outcome=`{item.Outcome}` first_candidate=`{item.FirstCandidate ?? "none"}` reason=`{item.Reason ?? "none"}`");
            }
        }
        builder.AppendLine();

        builder.AppendLine("## Observed Corpus");
        builder.AppendLine();
        foreach (var caseArtifact in artifact.Cases
                     .Where(static item => item.Tags.Contains("optional-smoke", StringComparer.OrdinalIgnoreCase))
                     .OrderBy(static item => item.CorpusCategory, StringComparer.Ordinal)
                     .ThenBy(static item => item.CaseId, StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{caseArtifact.CorpusCategory}` / `{caseArtifact.CaseId}`: `{caseArtifact.Status}`");
            if (humanOutputLookup.TryGetValue(caseArtifact.CaseId, out var modes))
            {
                builder.AppendLine($"  human outputs: `{string.Join("`, `", modes)}`");
            }
            else
            {
                builder.AppendLine("  human outputs: `none (observation-only)`");
            }

            if (visualOutputLookup.TryGetValue(caseArtifact.CaseId, out var visualPaths))
            {
                builder.AppendLine($"  visual outputs: `{string.Join("`, `", visualPaths)}`");
            }
            else
            {
                builder.AppendLine("  visual outputs: `none (observation-only)`");
            }

            if (!string.IsNullOrWhiteSpace(caseArtifact.SkipReason))
            {
                builder.AppendLine($"  skip reason: `{caseArtifact.SkipReason}`");
            }
            else if (!string.IsNullOrWhiteSpace(caseArtifact.FailureReason))
            {
                builder.AppendLine($"  failure: `{caseArtifact.FailureReason}`");
            }
        }
        if (!artifact.Cases.Any(static item => item.Tags.Contains("optional-smoke", StringComparer.OrdinalIgnoreCase)))
        {
            builder.AppendLine("- none");
        }
        builder.AppendLine();

        builder.AppendLine("## Performance");
        builder.AppendLine();
        if (artifact.PerformanceBaseline is null)
        {
            builder.AppendLine("- baseline: none");
        }
        else
        {
            AppendPerformanceMetric(builder, "first_useful_map_time_ms", artifact.PerformanceBaseline.FirstUsefulMapTimeMs);
            AppendPerformanceMetric(builder, "representative_chain_count", artifact.PerformanceBaseline.RepresentativeChainCount);
            AppendPerformanceMetric(builder, "unknown_guidance_case_count", artifact.PerformanceBaseline.UnknownGuidanceCaseCount);
            AppendPerformanceMetric(builder, "degraded_reason_count", artifact.PerformanceBaseline.DegradedReasonCount);
            builder.AppendLine(
                artifact.PerformanceBaseline.RegressionWarnings.Count > 0
                    ? $"- regression_warnings: `{string.Join("`, `", artifact.PerformanceBaseline.RegressionWarnings)}`"
                    : "- regression_warnings: none");
        }
        builder.AppendLine();

        builder.AppendLine("## Human Outputs");
        builder.AppendLine();
        if (artifact.HumanOutputs.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            foreach (var humanOutput in artifact.HumanOutputs
                         .OrderBy(static item => item.Mode, StringComparer.Ordinal)
                         .ThenBy(static item => item.CaseId, StringComparer.Ordinal))
            {
                builder.AppendLine(
                    $"- `{humanOutput.Mode}` / `{humanOutput.CorpusCategory}` / `{humanOutput.CaseId}`: `{humanOutput.Path}`");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Visual Outputs");
        builder.AppendLine();
        if (artifact.VisualOutputs.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            foreach (var visualOutput in artifact.VisualOutputs
                         .OrderBy(static item => item.CorpusCategory, StringComparer.Ordinal)
                         .ThenBy(static item => item.CaseId, StringComparer.Ordinal))
            {
                builder.AppendLine(
                    $"- `{visualOutput.CorpusCategory}` / `{visualOutput.CaseId}`: `{visualOutput.Path}`");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Artifacts");
        builder.AppendLine();
        builder.AppendLine("- `kpi.json`");
        builder.AppendLine("- `gate.json`");
        builder.AppendLine("- `summary.md`");
        if (!string.IsNullOrWhiteSpace(artifact.ReleaseReviewPath))
        {
            builder.AppendLine($"- `{artifact.ReleaseReviewPath}`");
        }
        foreach (var path in artifact.RelatedArtifacts)
        {
            builder.AppendLine($"- `{path}`");
        }

        return builder.ToString();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildHumanOutputLookup(
        IReadOnlyList<HumanOutputArtifactReference> humanOutputs) =>
        humanOutputs
            .GroupBy(static item => item.CaseId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<string>)group
                    .Select(static item => item.Mode)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(static item => item, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildVisualOutputLookup(
        IReadOnlyList<VisualOutputArtifactReference> visualOutputs) =>
        visualOutputs
            .GroupBy(static item => item.CaseId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<string>)group
                    .Select(static item => item.Path)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(static item => item, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

    private static void AppendPerformanceMetric(StringBuilder builder, string name, MetricBaselineArtifact? metric)
    {
        if (metric is null)
        {
            builder.AppendLine($"- {name}: `none`");
            return;
        }

        builder.AppendLine(
            $"- {name}: current=`{metric.Current}` baseline=`{metric.Baseline?.ToString() ?? "none"}` delta=`{metric.Delta?.ToString() ?? "none"}` regression_warning=`{metric.RegressionWarning}`");
    }

    private static string FormatInlineList(IReadOnlyCollection<string> values) =>
        values.Count == 0 ? "none" : string.Join("`, `", values);
}
