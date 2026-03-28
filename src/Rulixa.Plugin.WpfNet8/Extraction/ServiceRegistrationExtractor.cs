using System.Text.RegularExpressions;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class ServiceRegistrationExtractor
{
    private static readonly Regex RegistrationStartRegex = new(
        @"Add(?<lifetime>Singleton|Scoped|Transient)\s*<",
        RegexOptions.Compiled);
    private static readonly Regex FactoryRegistrationRegex = new(
        @"new\s+(?<implementation>[A-Za-z_][A-Za-z0-9_\.]*)\s*\(",
        RegexOptions.Compiled | RegexOptions.Singleline);

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
        foreach (var invocation in ExtractInvocations(content))
        {
            var registration = TryCreateRegistration(path, content, invocation);
            if (registration is null)
            {
                continue;
            }

            yield return registration;
        }
    }

    private static IEnumerable<ServiceRegistrationInvocation> ExtractInvocations(string content)
    {
        foreach (Match match in RegistrationStartRegex.Matches(content))
        {
            var invocation = TryExtractInvocation(content, match);
            if (invocation is not null)
            {
                yield return invocation.Value;
            }
        }
    }

    private static ServiceRegistration? TryCreateRegistration(
        string path,
        string content,
        ServiceRegistrationInvocation invocation)
    {
        var genericArguments = SplitTopLevel(invocation.GenericArgumentList, ',');
        if (genericArguments.Count == 2)
        {
            return new ServiceRegistration(
                path,
                genericArguments[0],
                genericArguments[1],
                ParseLifetime(invocation.LifetimeToken),
                SourceSpanFactory.FromMatch(content, invocation.StartIndex, invocation.Length));
        }

        if (genericArguments.Count != 1)
        {
            return null;
        }

        var serviceType = genericArguments[0];
        var factoryImplementation = TryExtractFactoryImplementation(invocation.ArgumentList);
        return factoryImplementation is not null
            ? new ServiceRegistration(
                path,
                serviceType,
                factoryImplementation,
                ServiceRegistrationLifetime.Factory,
                SourceSpanFactory.FromMatch(content, invocation.StartIndex, invocation.Length))
            : new ServiceRegistration(
                path,
                serviceType,
                serviceType,
                ParseLifetime(invocation.LifetimeToken),
                SourceSpanFactory.FromMatch(content, invocation.StartIndex, invocation.Length));
    }

    private static ServiceRegistrationInvocation? TryExtractInvocation(string content, Match match)
    {
        var openGenericIndex = match.Index + match.Length - 1;
        var closeGenericIndex = FindMatchingDelimiter(content, openGenericIndex, '<', '>');
        if (closeGenericIndex < 0)
        {
            return null;
        }

        var openParenthesisIndex = FindNextNonWhitespaceIndex(content, closeGenericIndex + 1);
        if (!IsExpectedCharacter(content, openParenthesisIndex, '('))
        {
            return null;
        }

        var closeParenthesisIndex = FindMatchingDelimiter(content, openParenthesisIndex, '(', ')');
        if (closeParenthesisIndex < 0)
        {
            return null;
        }

        var semicolonIndex = FindNextNonWhitespaceIndex(content, closeParenthesisIndex + 1);
        if (!IsExpectedCharacter(content, semicolonIndex, ';'))
        {
            return null;
        }

        return new ServiceRegistrationInvocation(
            match.Groups["lifetime"].Value,
            content[(openGenericIndex + 1)..closeGenericIndex],
            content[(openParenthesisIndex + 1)..closeParenthesisIndex],
            match.Index,
            semicolonIndex - match.Index + 1);
    }

    private static int FindMatchingDelimiter(string content, int startIndex, char openDelimiter, char closeDelimiter)
    {
        var depth = 0;
        for (var index = startIndex; index < content.Length; index++)
        {
            if (content[index] == openDelimiter)
            {
                depth++;
                continue;
            }

            if (content[index] != closeDelimiter)
            {
                continue;
            }

            depth--;
            if (depth == 0)
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindNextNonWhitespaceIndex(string content, int startIndex)
    {
        for (var index = Math.Max(0, startIndex); index < content.Length; index++)
        {
            if (!char.IsWhiteSpace(content[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsExpectedCharacter(string content, int index, char expectedCharacter) =>
        index >= 0 && index < content.Length && content[index] == expectedCharacter;

    private static IReadOnlyList<string> SplitTopLevel(string value, char separator)
    {
        var parts = new List<string>();
        var start = 0;
        var genericDepth = 0;

        for (var index = 0; index < value.Length; index++)
        {
            switch (value[index])
            {
                case '<':
                    genericDepth++;
                    break;
                case '>':
                    genericDepth = Math.Max(0, genericDepth - 1);
                    break;
                default:
                    if (value[index] == separator && genericDepth == 0)
                    {
                        parts.Add(value[start..index].Trim());
                        start = index + 1;
                    }

                    break;
            }
        }

        parts.Add(value[start..].Trim());
        return parts
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .ToArray();
    }

    private static string? TryExtractFactoryImplementation(string argumentList)
    {
        var match = FactoryRegistrationRegex.Match(argumentList);
        return match.Success
            ? match.Groups["implementation"].Value.Trim()
            : null;
    }

    private static ServiceRegistrationLifetime ParseLifetime(string value) => value switch
    {
        "Singleton" => ServiceRegistrationLifetime.Singleton,
        "Scoped" => ServiceRegistrationLifetime.Scoped,
        "Transient" => ServiceRegistrationLifetime.Transient,
        _ => ServiceRegistrationLifetime.Factory
    };

    private readonly record struct ServiceRegistrationInvocation(
        string LifetimeToken,
        string GenericArgumentList,
        string ArgumentList,
        int StartIndex,
        int Length);
}
