namespace Rulixa.Infrastructure.Quality;

public static class QualityArtifactConventions
{
    public const string SchemaVersion = "rulixa.quality.kpi.v1";
    public const string RunSchemaVersion = "rulixa.local-quality-run.v1";

    public static string BuildDefaultOutputDirectory(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        return Path.Combine(Path.GetFullPath(repositoryRoot), "artifacts", "local-quality");
    }

    public static string BuildFileName(DateTimeOffset generatedAtUtc, string suiteName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteName);
        var slug = new string(
            suiteName
                .Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
                .ToArray())
            .Trim('-');
        if (slug.Length == 0)
        {
            slug = "quality";
        }

        return $"{generatedAtUtc.UtcDateTime:yyyyMMddTHHmmssfffffffZ}-{slug}.json";
    }

    public static string BuildRunId(DateTimeOffset generatedAtUtc) =>
        generatedAtUtc.UtcDateTime.ToString("yyyyMMddTHHmmssfffffffZ");
}
