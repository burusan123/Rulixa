using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rulixa.Infrastructure.Quality;

public sealed class QualityArtifactWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<string> WriteAsync(
        string outputRootDirectory,
        string suiteName,
        IReadOnlyList<QualityCaseArtifact> cases,
        DateTimeOffset? generatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRootDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteName);
        ArgumentNullException.ThrowIfNull(cases);

        var timestamp = generatedAtUtc ?? DateTimeOffset.UtcNow;
        var gate = new QualityGateEvaluator().Evaluate(cases);
        var observation = new QualityObservationCalculator().Calculate(cases);
        var artifact = new QualityArtifact(
            SchemaVersion: QualityArtifactConventions.SchemaVersion,
            SuiteName: suiteName,
            GeneratedAtUtc: timestamp.UtcDateTime.ToString("O"),
            Cases: cases,
            QualityGate: gate,
            FirstUsefulMapTimeMs: observation.FirstUsefulMapTimeMs,
            UnknownGuidanceCaseCount: observation.UnknownGuidanceCaseCount,
            UnknownGuidanceItemCount: observation.UnknownGuidanceItemCount,
            UnknownGuidanceFamilyCount: observation.UnknownGuidanceFamilyCount,
            RepresentativeChainCount: observation.RepresentativeChainCount,
            DegradedReasonCount: observation.DegradedReasonCount);
        var json = JsonSerializer.Serialize(artifact, JsonOptions);

        var fullOutputRoot = Path.GetFullPath(outputRootDirectory);
        Directory.CreateDirectory(fullOutputRoot);

        var filePath = Path.Combine(fullOutputRoot, QualityArtifactConventions.BuildFileName(timestamp, suiteName));
        await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
        return filePath;
    }
}
