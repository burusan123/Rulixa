using System.Text.RegularExpressions;
using Rulixa.Application.Ports;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class CommandImpactAnalyzer
{
    private static readonly Regex FieldRegex = new(
        @"^\s*(?:private|protected|internal|public)\s+(?:readonly\s+)?(?<type>[A-Za-z_][A-Za-z0-9_<>\.\?,\[\]]*)\s+(?<name>[A-Za-z_]\w*)\s*;",
        RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex PropertyRegex = new(
        @"^\s*(?:private|protected|internal|public)\s+(?<type>[A-Za-z_][A-Za-z0-9_<>\.\?,\[\]]*)\s+(?<name>[A-Za-z_]\w*)\s*\{\s*get;",
        RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex LocalRegex = new(
        @"(?<type>var|[A-Za-z_][A-Za-z0-9_<>\.\?,\[\]]*)\s+(?<name>[A-Za-z_]\w*)\s*=\s*(?<rhs>[^;]+);",
        RegexOptions.Compiled);
    private static readonly Regex MethodCallRegex = new(
        @"(?<target>new\s+[A-Za-z_][A-Za-z0-9_\.]*\s*\([^;]*?\)|(?:this\.)?[A-Za-z_]\w*)\.(?<method>[A-Za-z_]\w*)\s*\(",
        RegexOptions.Compiled);
    private static readonly Regex NewTypeRegex = new(@"new\s+(?<type>[A-Za-z_][A-Za-z0-9_\.]*)\s*\(", RegexOptions.Compiled);
    private static readonly Regex IdentifierRegex = new(@"^(?:this\.)?(?<name>[A-Za-z_]\w*)$", RegexOptions.Compiled);
    private static readonly Regex ThisHelperInvocationRegex = new(@"\bthis\.(?<method>[A-Za-z_]\w*)\s*\(", RegexOptions.Compiled);
    private static readonly Regex ImplicitHelperInvocationRegex = new(
        @"(?<!new\s)(?<![\.\w])(?<method>[A-Za-z_]\w*)\s*\(",
        RegexOptions.Compiled);
    private static readonly HashSet<string> HelperInvocationKeywords = new(StringComparer.Ordinal)
    {
        "base",
        "catch",
        "checked",
        "default",
        "do",
        "else",
        "finally",
        "for",
        "foreach",
        "if",
        "lock",
        "nameof",
        "new",
        "return",
        "sizeof",
        "switch",
        "typeof",
        "unchecked",
        "using",
        "while"
    };

    private readonly IWorkspaceFileSystem workspaceFileSystem;

    internal CommandImpactAnalyzer(IWorkspaceFileSystem workspaceFileSystem)
    {
        this.workspaceFileSystem = workspaceFileSystem ?? throw new ArgumentNullException(nameof(workspaceFileSystem));
    }

    internal async Task<IReadOnlyList<CommandImpactDetails>> AnalyzeAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        IReadOnlyList<CommandBinding> commands,
        CancellationToken cancellationToken)
    {
        var impactDetails = new List<CommandImpactDetails>();

        foreach (var command in commands)
        {
            var sourceDocuments = await ReadSourceDocumentsAsync(workspaceRoot, scanResult, command.ViewModelSymbol, cancellationToken)
                .ConfigureAwait(false);
            if (sourceDocuments.Count == 0)
            {
                impactDetails.Add(new CommandImpactDetails(command, [], [], null));
                continue;
            }

            var executeMethodName = command.ExecuteSymbol.Split('.').Last();
            var executeMethod = TryFindMethodDefinition(sourceDocuments, executeMethodName);
            if (executeMethod is null)
            {
                impactDetails.Add(new CommandImpactDetails(command, [], [], sourceDocuments[0].RelativePath));
                continue;
            }

            var knownTypes = ExtractKnownTypes(sourceDocuments);
            var directImpacts = ExtractMethodImpacts(
                scanResult,
                command,
                executeMethodName,
                knownTypes,
                executeMethod.Body);
            var helperInvocations = ExtractHelperInvocations(sourceDocuments, executeMethodName, executeMethod.Body);
            var helperImpacts = ExtractHelperImpacts(
                sourceDocuments,
                scanResult,
                command,
                knownTypes,
                helperInvocations);
            var allImpacts = directImpacts
                .Concat(helperImpacts)
                .GroupBy(static impact => new
                {
                    impact.DisplaySymbol,
                    impact.DialogWindowSymbol,
                    impact.ActivationKind,
                    impact.OriginMethodName
                })
                .Select(static group => group.First())
                .ToArray();
            var helperNamesWithImpacts = helperImpacts
                .Select(static impact => impact.OriginMethodName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            impactDetails.Add(new CommandImpactDetails(
                command,
                allImpacts,
                helperInvocations
                    .Where(invocation => helperNamesWithImpacts.Contains(invocation.HelperMethodName))
                    .ToArray(),
                executeMethod.SourceFilePath));
        }

        return impactDetails;
    }

    private async Task<IReadOnlyList<SourceDocument>> ReadSourceDocumentsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        string symbol,
        CancellationToken cancellationToken)
    {
        var aggregates = PartialSymbolAggregateResolver.Build(scanResult, [symbol]);
        if (!aggregates.TryGetValue(symbol, out var aggregate))
        {
            return [];
        }

        var documents = new List<SourceDocument>();
        foreach (var relativePath in aggregate.FilePaths)
        {
            var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var source = await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
            documents.Add(new SourceDocument(relativePath, source));
        }

        return documents;
    }

    private static Dictionary<string, string> ExtractKnownTypes(IReadOnlyList<SourceDocument> sourceDocuments)
    {
        var knownTypes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var source in sourceDocuments.Select(static document => document.Content))
        {
            foreach (Match match in FieldRegex.Matches(source))
            {
                knownTypes[match.Groups["name"].Value] = match.Groups["type"].Value;
            }

            foreach (Match match in PropertyRegex.Matches(source))
            {
                knownTypes[match.Groups["name"].Value] = match.Groups["type"].Value;
            }
        }

        return knownTypes;
    }

    private static IReadOnlyList<DirectCommandImpact> ExtractHelperImpacts(
        IReadOnlyList<SourceDocument> sourceDocuments,
        WorkspaceScanResult scanResult,
        CommandBinding command,
        IReadOnlyDictionary<string, string> knownTypes,
        IReadOnlyList<HelperInvocation> helperInvocations)
    {
        var helperImpacts = new List<DirectCommandImpact>();

        foreach (var helperInvocation in helperInvocations)
        {
            var helperMethod = TryFindMethodDefinition(sourceDocuments, helperInvocation.HelperMethodName);
            if (helperMethod is null)
            {
                continue;
            }

            helperImpacts.AddRange(ExtractMethodImpacts(
                scanResult,
                command,
                helperInvocation.HelperMethodName,
                knownTypes,
                helperMethod.Body));
        }

        return helperImpacts;
    }

    private static IReadOnlyList<DirectCommandImpact> ExtractMethodImpacts(
        WorkspaceScanResult scanResult,
        CommandBinding command,
        string originMethodName,
        IReadOnlyDictionary<string, string> knownTypes,
        string methodBody)
    {
        var localTypes = ExtractLocalTypes(methodBody, knownTypes);
        var impacts = new List<DirectCommandImpact>();

        foreach (Match match in MethodCallRegex.Matches(methodBody))
        {
            var rawTarget = match.Groups["target"].Value.Trim();
            var targetMethodName = match.Groups["method"].Value;
            var resolvedTarget = ResolveTarget(
                scanResult,
                command.ViewModelSymbol,
                rawTarget,
                targetMethodName,
                knownTypes,
                localTypes);
            if (resolvedTarget is null)
            {
                continue;
            }

            impacts.AddRange(BuildImpacts(scanResult, resolvedTarget, originMethodName));
        }

        return impacts
            .GroupBy(static impact => new
            {
                impact.DisplaySymbol,
                impact.DialogWindowSymbol,
                impact.ActivationKind,
                impact.OriginMethodName
            })
            .Select(static group => group.First())
            .ToArray();
    }

    private static Dictionary<string, string> ExtractLocalTypes(
        string methodBody,
        IReadOnlyDictionary<string, string> knownTypes)
    {
        var localTypes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (Match match in LocalRegex.Matches(methodBody))
        {
            var localName = match.Groups["name"].Value;
            var declaredType = match.Groups["type"].Value;
            var rightHandSide = match.Groups["rhs"].Value.Trim();
            var resolvedType = ResolveLocalType(declaredType, rightHandSide, knownTypes);
            if (!string.IsNullOrWhiteSpace(resolvedType))
            {
                localTypes[localName] = resolvedType;
            }
        }

        return localTypes;
    }

    private static string? ResolveLocalType(
        string declaredType,
        string rightHandSide,
        IReadOnlyDictionary<string, string> knownTypes)
    {
        if (!string.Equals(declaredType, "var", StringComparison.Ordinal))
        {
            return declaredType;
        }

        var newTypeMatch = NewTypeRegex.Match(rightHandSide);
        if (newTypeMatch.Success)
        {
            return newTypeMatch.Groups["type"].Value;
        }

        var identifierMatch = IdentifierRegex.Match(rightHandSide);
        if (!identifierMatch.Success)
        {
            return null;
        }

        var identifier = identifierMatch.Groups["name"].Value;
        return knownTypes.TryGetValue(identifier, out var resolvedType) ? resolvedType : null;
    }

    private static IReadOnlyList<HelperInvocation> ExtractHelperInvocations(
        IReadOnlyList<SourceDocument> sourceDocuments,
        string executeMethodName,
        string methodBody)
    {
        var helperMethods = new Dictionary<string, HelperInvocation>(StringComparer.OrdinalIgnoreCase);

        AddHelperInvocations(helperMethods, ThisHelperInvocationRegex.Matches(methodBody), sourceDocuments, executeMethodName);
        AddHelperInvocations(helperMethods, ImplicitHelperInvocationRegex.Matches(methodBody), sourceDocuments, executeMethodName);

        return helperMethods.Values.ToArray();
    }

    private static void AddHelperInvocations(
        IDictionary<string, HelperInvocation> helperMethods,
        MatchCollection matches,
        IReadOnlyList<SourceDocument> sourceDocuments,
        string executeMethodName)
    {
        foreach (Match match in matches)
        {
            var helperMethodName = match.Groups["method"].Value;
            if (string.IsNullOrWhiteSpace(helperMethodName)
                || string.Equals(helperMethodName, executeMethodName, StringComparison.OrdinalIgnoreCase)
                || HelperInvocationKeywords.Contains(helperMethodName))
            {
                continue;
            }

            var helperMethod = TryFindMethodDefinition(sourceDocuments, helperMethodName);
            if (helperMethod is null || !IsEligibleHelperMethod(helperMethod.DeclarationLine))
            {
                continue;
            }

            helperMethods[helperMethodName] = new HelperInvocation(
                helperMethodName,
                helperMethod.SourceFilePath,
                helperMethod.SourceSpan);
        }
    }

    private static bool IsEligibleHelperMethod(string declarationLine)
    {
        var normalized = declarationLine.Trim();
        if (normalized.StartsWith("public ", StringComparison.Ordinal))
        {
            return false;
        }

        if (!(normalized.StartsWith("private ", StringComparison.Ordinal)
              || normalized.StartsWith("internal ", StringComparison.Ordinal)
              || normalized.StartsWith("protected ", StringComparison.Ordinal)))
        {
            return false;
        }

        return !normalized.Contains(" static ", StringComparison.Ordinal)
               && !normalized.StartsWith("static ", StringComparison.Ordinal);
    }

    private static ResolvedImpactTarget? ResolveTarget(
        WorkspaceScanResult scanResult,
        string viewModelSymbol,
        string rawTarget,
        string targetMethodName,
        IReadOnlyDictionary<string, string> knownTypes,
        IReadOnlyDictionary<string, string> localTypes)
    {
        var targetTypeName = ResolveTargetTypeName(rawTarget, knownTypes, localTypes);
        if (string.IsNullOrWhiteSpace(targetTypeName))
        {
            return null;
        }

        var targetTypeSymbol = ResolveTypeSymbol(scanResult, targetTypeName);
        if (string.IsNullOrWhiteSpace(targetTypeSymbol)
            || string.Equals(targetTypeSymbol, viewModelSymbol, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!IsInterestingImpactType(scanResult, targetTypeSymbol))
        {
            return null;
        }

        var implementationTypeSymbol = scanResult.ServiceRegistrations
            .FirstOrDefault(registration => string.Equals(registration.ServiceType, targetTypeSymbol, StringComparison.OrdinalIgnoreCase))
            ?.ImplementationType;
        var sourceTypeSymbol = implementationTypeSymbol ?? targetTypeSymbol;
        var sourceFilePath = scanResult.Symbols.FirstOrDefault(symbol =>
            string.Equals(symbol.QualifiedName, sourceTypeSymbol, StringComparison.OrdinalIgnoreCase))?.FilePath;

        return new ResolvedImpactTarget(targetTypeSymbol, sourceTypeSymbol, targetMethodName, sourceFilePath);
    }

    private static string? ResolveTargetTypeName(
        string rawTarget,
        IReadOnlyDictionary<string, string> knownTypes,
        IReadOnlyDictionary<string, string> localTypes)
    {
        var newTypeMatch = NewTypeRegex.Match(rawTarget);
        if (newTypeMatch.Success)
        {
            return newTypeMatch.Groups["type"].Value;
        }

        var identifier = rawTarget.StartsWith("this.", StringComparison.Ordinal)
            ? rawTarget["this.".Length..]
            : rawTarget;
        if (localTypes.TryGetValue(identifier, out var localType))
        {
            return localType;
        }

        return knownTypes.TryGetValue(identifier, out var knownType) ? knownType : null;
    }

    private static IReadOnlyList<DirectCommandImpact> BuildImpacts(
        WorkspaceScanResult scanResult,
        ResolvedImpactTarget target,
        string originMethodName)
    {
        var activations = scanResult.WindowActivations
            .Where(activation => string.Equals(
                activation.CallerSymbol,
                $"{target.SourceTypeSymbol}.{target.MethodName}",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (activations.Length == 0)
        {
            return
            [
                new DirectCommandImpact(
                    $"{target.TargetTypeSymbol}.{target.MethodName}(...)",
                    target.SourceFilePath,
                    target.MethodName,
                    originMethodName,
                    null,
                    null,
                    null)
            ];
        }

        return activations
            .Select(activation => new DirectCommandImpact(
                $"{target.TargetTypeSymbol}.{target.MethodName}(...)",
                target.SourceFilePath,
                target.MethodName,
                originMethodName,
                activation.WindowSymbol,
                activation.ActivationKind,
                activation.OwnerKind))
            .ToArray();
    }

    private static string? ResolveTypeSymbol(WorkspaceScanResult scanResult, string targetTypeName)
    {
        if (scanResult.Symbols.Any(symbol => string.Equals(symbol.QualifiedName, targetTypeName, StringComparison.OrdinalIgnoreCase)))
        {
            return scanResult.Symbols
                .First(symbol => string.Equals(symbol.QualifiedName, targetTypeName, StringComparison.OrdinalIgnoreCase))
                .QualifiedName;
        }

        var simpleTypeName = targetTypeName.Split('.').Last();
        var matches = scanResult.Symbols
            .Where(symbol => symbol.Kind is SymbolKind.Class or SymbolKind.Interface or SymbolKind.Window)
            .Where(symbol => string.Equals(symbol.DisplayName, simpleTypeName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static symbol => symbol.QualifiedName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return matches.Length == 1 ? matches[0].QualifiedName : null;
    }

    private static bool IsInterestingImpactType(WorkspaceScanResult scanResult, string targetTypeSymbol)
    {
        if (scanResult.ServiceRegistrations.Any(registration =>
                string.Equals(registration.ServiceType, targetTypeSymbol, StringComparison.OrdinalIgnoreCase)
                || string.Equals(registration.ImplementationType, targetTypeSymbol, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var simpleName = targetTypeSymbol.Split('.').Last();
        return simpleName.EndsWith("Service", StringComparison.Ordinal)
            || simpleName.EndsWith("Query", StringComparison.Ordinal)
            || simpleName.EndsWith("Saver", StringComparison.Ordinal)
            || simpleName.EndsWith("Initializer", StringComparison.Ordinal)
            || simpleName.EndsWith("Port", StringComparison.Ordinal)
            || simpleName.EndsWith("Guard", StringComparison.Ordinal)
            || simpleName.EndsWith("Window", StringComparison.Ordinal);
    }

    private static MethodDefinition? TryFindMethodDefinition(
        IReadOnlyList<SourceDocument> sourceDocuments,
        string methodName)
    {
        foreach (var document in sourceDocuments)
        {
            var methodDefinition = TryFindMethodDefinition(document.Content, methodName);
            if (methodDefinition is not null)
            {
                return methodDefinition with { SourceFilePath = document.RelativePath };
            }
        }

        return null;
    }

    private static MethodDefinition? TryFindMethodDefinition(string source, string methodName)
    {
        var lines = source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var pattern = new Regex(
            $@"^\s*(?:(?:public|private|internal|protected|static|virtual|override|sealed|async|partial|new|unsafe|extern)\s+)+(?:[A-Za-z_][A-Za-z0-9_<>,\.\?\[\]]*\s+)+{Regex.Escape(methodName)}\s*\(",
            RegexOptions.CultureInvariant);

        for (var index = 0; index < lines.Length; index++)
        {
            if (!pattern.IsMatch(lines[index]))
            {
                continue;
            }

            var startLine = index + 1;
            var startIndex = GetLineStartIndex(source, startLine);
            var bodyRange = TryFindMemberBodyRange(source, startIndex);
            if (bodyRange is null)
            {
                return null;
            }

            return new MethodDefinition(
                methodName,
                string.Empty,
                lines[index],
                source.Substring(bodyRange.Value.BodyStartIndex, bodyRange.Value.EndIndex - bodyRange.Value.BodyStartIndex + 1),
                new SourceSpan(startLine, GetLineNumberAt(source, bodyRange.Value.EndIndex)));
        }

        return null;
    }

    private static int GetLineStartIndex(string source, int lineNumber)
    {
        if (lineNumber <= 1)
        {
            return 0;
        }

        var currentLine = 1;
        for (var index = 0; index < source.Length; index++)
        {
            if (source[index] != '\n')
            {
                continue;
            }

            currentLine++;
            if (currentLine == lineNumber)
            {
                return index + 1;
            }
        }

        return -1;
    }

    private static int GetLineNumberAt(string source, int index)
    {
        var lineNumber = 1;
        for (var currentIndex = 0; currentIndex < index && currentIndex < source.Length; currentIndex++)
        {
            if (source[currentIndex] == '\n')
            {
                lineNumber++;
            }
        }

        return lineNumber;
    }

    private static (int BodyStartIndex, int EndIndex)? TryFindMemberBodyRange(string source, int startIndex)
    {
        var expressionIndex = source.IndexOf("=>", startIndex, StringComparison.Ordinal);
        var braceIndex = source.IndexOf('{', startIndex);
        if (expressionIndex >= 0 && (braceIndex < 0 || expressionIndex < braceIndex))
        {
            var semicolonIndex = source.IndexOf(';', expressionIndex);
            return semicolonIndex < 0 ? null : (expressionIndex + 2, semicolonIndex - 1);
        }

        if (braceIndex < 0)
        {
            return null;
        }

        var depth = 0;
        for (var index = braceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
                continue;
            }

            if (source[index] != '}')
            {
                continue;
            }

            depth--;
            if (depth == 0)
            {
                return (braceIndex + 1, index - 1);
            }
        }

        return null;
    }

    private sealed record MethodDefinition(
        string Name,
        string SourceFilePath,
        string DeclarationLine,
        string Body,
        SourceSpan SourceSpan);

    private sealed record SourceDocument(
        string RelativePath,
        string Content);
}

internal sealed record CommandImpactDetails(
    CommandBinding Command,
    IReadOnlyList<DirectCommandImpact> DirectImpacts,
    IReadOnlyList<HelperInvocation> HelperInvocations,
    string? ViewModelFilePath);

internal sealed record HelperInvocation(
    string HelperMethodName,
    string SourceFilePath,
    SourceSpan BodyRange);

internal sealed record DirectCommandImpact(
    string DisplaySymbol,
    string? SourceFilePath,
    string MethodName,
    string OriginMethodName,
    string? DialogWindowSymbol,
    string? ActivationKind,
    string? OwnerKind);

internal sealed record ResolvedImpactTarget(
    string TargetTypeSymbol,
    string SourceTypeSymbol,
    string MethodName,
    string? SourceFilePath);
