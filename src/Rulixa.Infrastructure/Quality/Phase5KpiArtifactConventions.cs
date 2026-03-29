namespace Rulixa.Infrastructure.Quality;

public static class Phase5KpiArtifactConventions
{
    public const string SchemaVersion = "rulixa.phase5.kpi.v1";

    public static string BuildDefaultOutputDirectory(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        return Path.Combine(Path.GetFullPath(repositoryRoot), "artifacts", "phase5");
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
            slug = "phase5";
        }

        return $"{generatedAtUtc.UtcDateTime:yyyyMMddTHHmmssfffffffZ}-{slug}.json";
    }
}
