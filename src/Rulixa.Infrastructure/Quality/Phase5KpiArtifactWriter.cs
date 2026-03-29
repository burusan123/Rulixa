using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rulixa.Infrastructure.Quality;

public sealed class Phase5KpiArtifactWriter
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
        IReadOnlyList<Phase5KpiCaseArtifact> cases,
        DateTimeOffset? generatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRootDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteName);
        ArgumentNullException.ThrowIfNull(cases);

        var timestamp = generatedAtUtc ?? DateTimeOffset.UtcNow;
        var gate = new Phase5QualityGateEvaluator().Evaluate(cases);
        var artifact = new Phase5KpiArtifact(
            SchemaVersion: Phase5KpiArtifactConventions.SchemaVersion,
            SuiteName: suiteName,
            GeneratedAtUtc: timestamp.UtcDateTime.ToString("O"),
            Cases: cases,
            QualityGate: gate);
        var json = JsonSerializer.Serialize(artifact, JsonOptions);

        var fullOutputRoot = Path.GetFullPath(outputRootDirectory);
        Directory.CreateDirectory(fullOutputRoot);

        var filePath = Path.Combine(fullOutputRoot, Phase5KpiArtifactConventions.BuildFileName(timestamp, suiteName));
        await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
        return filePath;
    }
}
