using System.Text.Json;

namespace Rulixa.Infrastructure.Quality;

internal sealed class QualityPerformanceBaselineComparer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<PerformanceBaselineArtifact?> TryLoadAndCompareAsync(
        string qualityOutputRoot,
        LocalQualityRunArtifact currentArtifact,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualityOutputRoot);
        ArgumentNullException.ThrowIfNull(currentArtifact);

        var baselinePath = Path.Combine(Path.GetFullPath(qualityOutputRoot), "latest", "kpi.json");
        if (!File.Exists(baselinePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(baselinePath, cancellationToken).ConfigureAwait(false);
        var baselineArtifact = JsonSerializer.Deserialize<LocalQualityRunArtifact>(json, JsonOptions);
        if (baselineArtifact is null || string.Equals(baselineArtifact.RunId, currentArtifact.RunId, StringComparison.Ordinal))
        {
            return null;
        }

        var firstUsefulMapTime = CompareLowerIsBetter(
            currentArtifact.FirstUsefulMapTimeMs,
            baselineArtifact.FirstUsefulMapTimeMs);
        var representativeChains = CompareHigherIsBetter(
            currentArtifact.RepresentativeChainCount,
            baselineArtifact.RepresentativeChainCount);
        var unknownGuidanceCases = CompareLowerIsBetter(
            currentArtifact.UnknownGuidanceCaseCount,
            baselineArtifact.UnknownGuidanceCaseCount);
        var degradedReasons = CompareLowerIsBetter(
            currentArtifact.DegradedReasonCount,
            baselineArtifact.DegradedReasonCount);

        var warnings = new List<string>();
        AppendWarning(warnings, "first_useful_map_time_ms", firstUsefulMapTime);
        AppendWarning(warnings, "representative_chain_count", representativeChains);
        AppendWarning(warnings, "unknown_guidance_case_count", unknownGuidanceCases);
        AppendWarning(warnings, "degraded_reason_count", degradedReasons);

        return new PerformanceBaselineArtifact(
            FirstUsefulMapTimeMs: firstUsefulMapTime,
            RepresentativeChainCount: representativeChains,
            UnknownGuidanceCaseCount: unknownGuidanceCases,
            DegradedReasonCount: degradedReasons,
            RegressionWarnings: warnings);
    }

    private static MetricBaselineArtifact? CompareLowerIsBetter(long? current, long? baseline)
    {
        if (current is null)
        {
            return null;
        }

        long? delta = baseline is null ? null : current.Value - baseline.Value;
        return new MetricBaselineArtifact(
            Current: current.Value,
            Baseline: baseline,
            Delta: delta,
            RegressionWarning: delta is > 0);
    }

    private static MetricBaselineArtifact CompareLowerIsBetter(int current, int baseline) =>
        new(
            Current: current,
            Baseline: baseline,
            Delta: current - baseline,
            RegressionWarning: current > baseline);

    private static MetricBaselineArtifact CompareHigherIsBetter(int current, int baseline) =>
        new(
            Current: current,
            Baseline: baseline,
            Delta: current - baseline,
            RegressionWarning: current < baseline);

    private static void AppendWarning(ICollection<string> warnings, string metricName, MetricBaselineArtifact? metric)
    {
        if (metric?.RegressionWarning == true)
        {
            warnings.Add(metricName);
        }
    }
}
