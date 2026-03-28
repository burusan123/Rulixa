using System.Text.RegularExpressions;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class ServiceRegistrationExtractor
{
    private static readonly Regex GenericRegistrationRegex = new(@"Add(?<lifetime>Singleton|Scoped|Transient)<(?<service>[^,>]+),\s*(?<implementation>[^>]+)>", RegexOptions.Compiled);
    private static readonly Regex SelfRegistrationRegex = new(@"Add(?<lifetime>Singleton|Scoped|Transient)<(?<service>[^>]+)>\(", RegexOptions.Compiled);
    private static readonly Regex FactoryRegistrationRegex = new(@"Add(?<lifetime>Singleton|Scoped|Transient)<(?<service>[^>]+)>\([^)]*new\s+(?<implementation>[A-Za-z_]\w*)\(", RegexOptions.Compiled);

    public IReadOnlyList<ServiceRegistration> Extract(IReadOnlyDictionary<string, string> fileContents)
    {
        var registrations = new List<ServiceRegistration>();

        foreach (var (path, content) in fileContents.Where(static pair =>
                     Path.GetFileName(pair.Key).Equals("ServiceRegistration.cs", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var genericMatch = GenericRegistrationRegex.Match(line);
                if (genericMatch.Success)
                {
                    registrations.Add(new ServiceRegistration(
                        path,
                        genericMatch.Groups["service"].Value.Trim(),
                        genericMatch.Groups["implementation"].Value.Trim(),
                        ParseLifetime(genericMatch.Groups["lifetime"].Value)));
                    continue;
                }

                var factoryMatch = FactoryRegistrationRegex.Match(line);
                if (factoryMatch.Success)
                {
                    registrations.Add(new ServiceRegistration(
                        path,
                        factoryMatch.Groups["service"].Value.Trim(),
                        factoryMatch.Groups["implementation"].Value.Trim(),
                        ServiceRegistrationLifetime.Factory));
                    continue;
                }

                var selfMatch = SelfRegistrationRegex.Match(line);
                if (selfMatch.Success)
                {
                    var serviceType = selfMatch.Groups["service"].Value.Trim();
                    registrations.Add(new ServiceRegistration(
                        path,
                        serviceType,
                        serviceType,
                        ParseLifetime(selfMatch.Groups["lifetime"].Value)));
                }
            }
        }

        return registrations
            .OrderBy(static registration => registration.ServiceType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static registration => registration.ImplementationType, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ServiceRegistrationLifetime ParseLifetime(string value) => value switch
    {
        "Singleton" => ServiceRegistrationLifetime.Singleton,
        "Scoped" => ServiceRegistrationLifetime.Scoped,
        "Transient" => ServiceRegistrationLifetime.Transient,
        _ => ServiceRegistrationLifetime.Factory
    };
}
