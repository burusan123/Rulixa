using System.Text.RegularExpressions;
using Rulixa.Application.Ports;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class DependencyInjectionPackSectionBuilder
{
    private readonly IWorkspaceFileSystem workspaceFileSystem;
    private readonly CSharpSnippetCandidateFactory snippetFactory;

    internal DependencyInjectionPackSectionBuilder(
        IWorkspaceFileSystem workspaceFileSystem,
        CSharpSnippetCandidateFactory snippetFactory)
    {
        this.workspaceFileSystem = workspaceFileSystem ?? throw new ArgumentNullException(nameof(workspaceFileSystem));
        this.snippetFactory = snippetFactory ?? throw new ArgumentNullException(nameof(snippetFactory));
    }

    internal async Task AddAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        ICollection<Contract> contracts,
        ICollection<IndexSection> indexes,
        ICollection<SnippetSelectionCandidate> snippetCandidates,
        ICollection<FileSelectionCandidate> fileCandidates,
        CancellationToken cancellationToken)
    {
        var registrationFiles = scanResult.ServiceRegistrations
            .Select(static registration => registration.RegistrationFile)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (registrationFiles.Length == 0)
        {
            return;
        }

        foreach (var registrationFile in registrationFiles)
        {
            fileCandidates.Add(new FileSelectionCandidate(registrationFile, "dependency-injection", 20, true));
        }

        var summary = await BuildSummaryAsync(
                workspaceRoot,
                scanResult,
                relevantContext.ViewModelSymbols,
                cancellationToken)
            .ConfigureAwait(false);
        if (summary is null)
        {
            contracts.Add(new Contract(
                ContractKind.DependencyInjection,
                "DI 登録",
                "ViewModel と Service は DI 登録を通じて構成されます。",
                registrationFiles,
                scanResult.ServiceRegistrations.Select(static registration => registration.ServiceType).ToArray()));
            return;
        }

        if (summary.Value.ViewModelRegistrations.Count > 0)
        {
            contracts.Add(new Contract(
                ContractKind.DependencyInjection,
                "主要 ViewModel の登録",
                BuildViewModelRegistrationSummary(summary.Value.ViewModelRegistrations),
                registrationFiles,
                summary.Value.ViewModelRegistrations
                    .Select(static registration => registration.ServiceType)
                    .Concat(summary.Value.ViewModelSymbols)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()));
        }

        if (summary.Value.DirectDependencyRegistrations.Count > 0)
        {
            contracts.Add(new Contract(
                ContractKind.DependencyInjection,
                "直接依存のライフタイム",
                BuildDependencyLifetimeSummary(summary.Value.ViewModelSymbols, summary.Value.DirectDependencyRegistrations),
                registrationFiles,
                summary.Value.DirectDependencyRegistrations
                    .Select(static registration => registration.ServiceType)
                    .Concat(summary.Value.DirectDependencyRegistrations.Select(static registration => registration.ImplementationType))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()));
        }

        indexes.Add(BuildIndex(summary.Value));
        await AddConstructorSnippetsAsync(
                workspaceRoot,
                scanResult,
                summary.Value.ViewModelSymbols,
                snippetCandidates,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<DependencyInjectionSummary?> BuildSummaryAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        IReadOnlySet<string> relevantViewModelSymbols,
        CancellationToken cancellationToken)
    {
        var viewModelSymbols = relevantViewModelSymbols
            .Where(static symbol => symbol.EndsWith("ViewModel", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (viewModelSymbols.Length == 0)
        {
            return null;
        }

        var viewModelTypeNames = viewModelSymbols
            .Select(PackExtractionConventions.GetSimpleTypeName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var directDependencyTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var viewModelSymbol in viewModelSymbols)
        {
            var viewModelFilePath = scanResult.Symbols.FirstOrDefault(symbol =>
                string.Equals(symbol.QualifiedName, viewModelSymbol, StringComparison.OrdinalIgnoreCase))?.FilePath;
            if (string.IsNullOrWhiteSpace(viewModelFilePath))
            {
                continue;
            }

            var absolutePath = Path.Combine(workspaceRoot, viewModelFilePath.Replace('/', Path.DirectorySeparatorChar));
            var source = await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
            foreach (var typeName in ExtractConstructorDependencyTypeNames(source, PackExtractionConventions.GetSimpleTypeName(viewModelSymbol)))
            {
                directDependencyTypeNames.Add(typeName);
            }
        }

        var viewModelRegistrations = scanResult.ServiceRegistrations
            .Where(registration => RegistrationMatches(registration, viewModelTypeNames))
            .Distinct()
            .OrderBy(static registration => registration.ServiceType, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var directDependencyRegistrations = scanResult.ServiceRegistrations
            .Where(registration => RegistrationMatches(registration, directDependencyTypeNames))
            .Except(viewModelRegistrations)
            .Distinct()
            .OrderBy(static registration => registration.ServiceType, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return viewModelRegistrations.Length == 0 && directDependencyRegistrations.Length == 0
            ? null
            : new DependencyInjectionSummary(viewModelSymbols, viewModelRegistrations, directDependencyRegistrations);
    }

    private static IndexSection BuildIndex(DependencyInjectionSummary summary)
    {
        var lines = summary.ViewModelRegistrations
            .Select(static registration => $"{PackExtractionConventions.GetSimpleTypeName(registration.ImplementationType)} ({registration.Lifetime})")
            .ToList();
        if (summary.DirectDependencyRegistrations.Count > 0)
        {
            var counts = BuildLifetimeCounts(summary.DirectDependencyRegistrations);
            var sampleNames = summary.DirectDependencyRegistrations
                .Select(static registration => PackExtractionConventions.GetSimpleTypeName(registration.ServiceType))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToArray();
            lines.Add($"直接依存 {summary.DirectDependencyRegistrations.Count}件: {counts} (例: {string.Join(", ", sampleNames)})");
        }

        return new IndexSection("DI", lines);
    }

    private static string BuildViewModelRegistrationSummary(IReadOnlyList<ServiceRegistration> registrations)
    {
        var registrationsText = registrations
            .Select(static registration => $"{PackExtractionConventions.GetSimpleTypeName(registration.ImplementationType)} は {registration.Lifetime}")
            .ToArray();
        return $"{string.Join("、", registrationsText)} として DI 登録されます。";
    }

    private static string BuildDependencyLifetimeSummary(
        IReadOnlyList<string> viewModelSymbols,
        IReadOnlyList<ServiceRegistration> registrations)
    {
        var subject = viewModelSymbols.Count == 1 ? PackExtractionConventions.GetSimpleTypeName(viewModelSymbols[0]) : "主要 ViewModel";
        var counts = BuildLifetimeCounts(registrations);
        var sampleNames = registrations
            .Select(static registration => PackExtractionConventions.GetSimpleTypeName(registration.ServiceType))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();
        return $"{subject} の直接依存 {registrations.Count} 件は {counts} です。例: {string.Join(", ", sampleNames)}。";
    }

    private static string BuildLifetimeCounts(IReadOnlyList<ServiceRegistration> registrations) =>
        string.Join(
            "、",
            registrations
                .GroupBy(static registration => registration.Lifetime)
                .OrderBy(static group => GetLifetimePriority(group.Key))
                .Select(static group => $"{group.Key} {group.Count()}"));

    private static int GetLifetimePriority(ServiceRegistrationLifetime lifetime) => lifetime switch
    {
        ServiceRegistrationLifetime.Singleton => 0,
        ServiceRegistrationLifetime.Scoped => 1,
        ServiceRegistrationLifetime.Transient => 2,
        ServiceRegistrationLifetime.Factory => 3,
        _ => 100
    };

    private static bool RegistrationMatches(ServiceRegistration registration, IReadOnlySet<string> typeNames)
    {
        if (typeNames.Count == 0)
        {
            return false;
        }

        return ExtractRelevantTypeNames(registration.ServiceType).Any(typeNames.Contains)
            || ExtractRelevantTypeNames(registration.ImplementationType).Any(typeNames.Contains);
    }

    private static IReadOnlyList<string> ExtractConstructorDependencyTypeNames(string source, string className)
    {
        var parameterList = TryExtractConstructorParameterList(source, className);
        if (string.IsNullOrWhiteSpace(parameterList))
        {
            return [];
        }

        return SplitTopLevel(parameterList!, ',')
            .Select(ExtractParameterTypeExpression)
            .SelectMany(ExtractRelevantTypeNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? TryExtractConstructorParameterList(string source, string className)
    {
        var match = Regex.Match(
            source,
            $@"(?:public|internal|protected|private)\s+{Regex.Escape(className)}\s*\(",
            RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return null;
        }

        var openParenthesisIndex = source.IndexOf('(', match.Index);
        if (openParenthesisIndex < 0)
        {
            return null;
        }

        var depth = 0;
        for (var index = openParenthesisIndex; index < source.Length; index++)
        {
            if (source[index] == '(')
            {
                depth++;
                continue;
            }

            if (source[index] != ')')
            {
                continue;
            }

            depth--;
            if (depth == 0)
            {
                return source[(openParenthesisIndex + 1)..index];
            }
        }

        return null;
    }

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
        return parts.Where(static part => !string.IsNullOrWhiteSpace(part)).ToArray();
    }

    private static string ExtractParameterTypeExpression(string parameter)
    {
        var withoutDefaultValue = parameter.Split('=')[0].Trim();
        var typeEndIndex = FindLastTopLevelWhitespace(withoutDefaultValue);
        if (typeEndIndex <= 0)
        {
            return string.Empty;
        }

        var typeExpression = withoutDefaultValue[..typeEndIndex].Trim();
        while (true)
        {
            var nextWhitespaceIndex = FindFirstTopLevelWhitespace(typeExpression);
            if (nextWhitespaceIndex <= 0)
            {
                return typeExpression;
            }

            var modifier = typeExpression[..nextWhitespaceIndex];
            if (modifier is not ("ref" or "out" or "in" or "params" or "this"))
            {
                return typeExpression;
            }

            typeExpression = typeExpression[(nextWhitespaceIndex + 1)..].Trim();
        }
    }

    private static int FindLastTopLevelWhitespace(string value)
    {
        var genericDepth = 0;
        for (var index = value.Length - 1; index >= 0; index--)
        {
            switch (value[index])
            {
                case '>':
                    genericDepth++;
                    break;
                case '<':
                    genericDepth = Math.Max(0, genericDepth - 1);
                    break;
                default:
                    if (char.IsWhiteSpace(value[index]) && genericDepth == 0)
                    {
                        return index;
                    }

                    break;
            }
        }

        return -1;
    }

    private static int FindFirstTopLevelWhitespace(string value)
    {
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
                    if (char.IsWhiteSpace(value[index]) && genericDepth == 0)
                    {
                        return index;
                    }

                    break;
            }
        }

        return -1;
    }

    private static IReadOnlyList<string> ExtractRelevantTypeNames(string typeExpression) =>
        Regex.Matches(typeExpression, @"[A-Za-z_][A-Za-z0-9_\.]*", RegexOptions.CultureInvariant)
            .Select(static match => PackExtractionConventions.GetSimpleTypeName(match.Value))
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private readonly record struct DependencyInjectionSummary(
        IReadOnlyList<string> ViewModelSymbols,
        IReadOnlyList<ServiceRegistration> ViewModelRegistrations,
        IReadOnlyList<ServiceRegistration> DirectDependencyRegistrations);

    private async Task AddConstructorSnippetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        IReadOnlyList<string> viewModelSymbols,
        ICollection<SnippetSelectionCandidate> snippetCandidates,
        CancellationToken cancellationToken)
    {
        foreach (var viewModelSymbol in viewModelSymbols)
        {
            var viewModelFilePath = scanResult.Symbols.FirstOrDefault(symbol =>
                string.Equals(symbol.QualifiedName, viewModelSymbol, StringComparison.OrdinalIgnoreCase))?.FilePath;
            if (string.IsNullOrWhiteSpace(viewModelFilePath)
                || !PackExtractionConventions.ShouldCreateSnippet(scanResult, viewModelFilePath))
            {
                continue;
            }

            var className = PackExtractionConventions.GetSimpleTypeName(viewModelSymbol);
            var snippet = await snippetFactory
                .CreateConstructorSnippetAsync(
                    workspaceRoot,
                    viewModelFilePath,
                    className,
                    "dependency-injection",
                    0,
                    true,
                    $"{className}(...)",
                    cancellationToken)
                .ConfigureAwait(false);
            if (snippet is not null)
            {
                snippetCandidates.Add(snippet);
            }
        }
    }
}
