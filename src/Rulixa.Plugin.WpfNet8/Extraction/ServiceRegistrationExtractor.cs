using System.Text.RegularExpressions;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class ServiceRegistrationExtractor
{
    private static readonly Regex GenericRegistrationRegex = new(
        @"Add(?<lifetime>Singleton|Scoped|Transient)<(?<service>[^,>]+),\s*(?<implementation>[^>]+)>",
        RegexOptions.Compiled);
    private static readonly Regex SelfRegistrationRegex = new(
        @"Add(?<lifetime>Singleton|Scoped|Transient)<(?<service>[^>]+)>\(",
        RegexOptions.Compiled);
    private static readonly Regex FactoryRegistrationRegex = new(
        @"Add(?<lifetime>Singleton|Scoped|Transient)<(?<service>[^>]+)>\([^)]*new\s+(?<implementation>[A-Za-z_]\w*)\(",
        RegexOptions.Compiled);

    public IReadOnlyList<ServiceRegistration> Extract(IReadOnlyDictionary<string, string> fileContents)
    {
        var registrations = new List<ServiceRegistration>();

        foreach (var (path, content) in fileContents.Where(static pair =>
                     Path.GetFileName(pair.Key).Equals("ServiceRegistration.cs", StringComparison.OrdinalIgnoreCase)))
        {
            registrations.AddRange(ExtractRegistrations(path, content));
        }

        return registrations
            .OrderBy(static registration => registration.ServiceType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static registration => registration.ImplementationType, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<ServiceRegistration> ExtractRegistrations(string path, string content)
    {
        var matches = FactoryRegistrationRegex.Matches(content)
            .Cast<Match>()
            .Select(static match => (Match: match, Kind: RegistrationMatchKind.Factory))
            .Concat(GenericRegistrationRegex.Matches(content)
                .Cast<Match>()
                .Select(static match => (Match: match, Kind: RegistrationMatchKind.Generic)))
            .Concat(SelfRegistrationRegex.Matches(content)
                .Cast<Match>()
                .Select(static match => (Match: match, Kind: RegistrationMatchKind.Self)))
            .OrderBy(static entry => entry.Match.Index)
            .ToArray();

        var seenIndexes = new HashSet<int>();
        foreach (var entry in matches)
        {
            if (!seenIndexes.Add(entry.Match.Index))
            {
                continue;
            }

            yield return CreateRegistration(path, content, entry.Match, entry.Kind);
        }
    }

    private static ServiceRegistration CreateRegistration(
        string path,
        string content,
        Match match,
        RegistrationMatchKind kind)
    {
        return kind switch
        {
            RegistrationMatchKind.Factory => new ServiceRegistration(
                path,
                match.Groups["service"].Value.Trim(),
                match.Groups["implementation"].Value.Trim(),
                ServiceRegistrationLifetime.Factory,
                SourceSpanFactory.FromMatch(content, match.Index, match.Length)),
            RegistrationMatchKind.Generic => new ServiceRegistration(
                path,
                match.Groups["service"].Value.Trim(),
                match.Groups["implementation"].Value.Trim(),
                ParseLifetime(match.Groups["lifetime"].Value),
                SourceSpanFactory.FromMatch(content, match.Index, match.Length)),
            RegistrationMatchKind.Self => new ServiceRegistration(
                path,
                match.Groups["service"].Value.Trim(),
                match.Groups["service"].Value.Trim(),
                ParseLifetime(match.Groups["lifetime"].Value),
                SourceSpanFactory.FromMatch(content, match.Index, match.Length)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    private static ServiceRegistrationLifetime ParseLifetime(string value) => value switch
    {
        "Singleton" => ServiceRegistrationLifetime.Singleton,
        "Scoped" => ServiceRegistrationLifetime.Scoped,
        "Transient" => ServiceRegistrationLifetime.Transient,
        _ => ServiceRegistrationLifetime.Factory
    };

    private enum RegistrationMatchKind
    {
        Generic,
        Self,
        Factory
    }
}
