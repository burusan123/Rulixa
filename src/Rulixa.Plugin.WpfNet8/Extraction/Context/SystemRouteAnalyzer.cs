using System.Text.RegularExpressions;
using Rulixa.Application.Ports;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class SystemRouteAnalyzer
{
    private const int HelperForwardingDepth = 1;
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
    private static readonly Regex SelfMethodCallRegex = new(
        @"(?<!\.)(?<!new\s)\b(?<method>[A-Za-z_]\w*)\s*\(",
        RegexOptions.Compiled);
    private static readonly Regex NewTypeRegex = new(@"new\s+(?<type>[A-Za-z_][A-Za-z0-9_\.]*)\s*\(", RegexOptions.Compiled);
    private static readonly Regex IdentifierRegex = new(@"^(?:this\.)?(?<name>[A-Za-z_]\w*)$", RegexOptions.Compiled);

    private readonly IWorkspaceFileSystem workspaceFileSystem;
    private readonly Dictionary<string, IReadOnlyList<SystemRouteSourceDocument>> documentCache = new(StringComparer.OrdinalIgnoreCase);

    internal SystemRouteAnalyzer(IWorkspaceFileSystem workspaceFileSystem)
    {
        this.workspaceFileSystem = workspaceFileSystem ?? throw new ArgumentNullException(nameof(workspaceFileSystem));
    }

    internal async Task<IReadOnlyList<SystemRouteCandidate>> AnalyzeAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        string rootSymbol,
        IReadOnlyList<string> rootFilePaths,
        CancellationToken cancellationToken)
    {
        var routeCandidates = new Dictionary<string, SystemRouteCandidate>(StringComparer.OrdinalIgnoreCase);
        var firstHopSymbols = await DiscoverRouteTargetsAsync(
                workspaceRoot,
                scanResult,
                rootSymbol,
                rootFilePaths,
                cancellationToken)
            .ConfigureAwait(false);

        foreach (var firstHopSymbol in firstHopSymbols)
        {
            routeCandidates[firstHopSymbol] = new SystemRouteCandidate(firstHopSymbol, firstHopSymbol);
            foreach (var downstream in await DiscoverDownstreamTargetsAsync(
                         workspaceRoot,
                         scanResult,
                         rootSymbol,
                         firstHopSymbol,
                         cancellationToken)
                     .ConfigureAwait(false))
            {
                if (string.Equals(downstream, rootSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                routeCandidates.TryAdd(downstream, new SystemRouteCandidate(downstream, firstHopSymbol));
            }
        }

        return routeCandidates.Values
            .OrderBy(candidate => PackAnalysisHelpers.GetSystemFamilyPriority(SystemFamilyRoutingSupport.ResolveFamily(candidate.FirstHopSymbol, candidate.Symbol)))
            .ThenBy(static candidate => candidate.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<IReadOnlyList<string>> DiscoverDownstreamTargetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        string rootSymbol,
        string firstHopSymbol,
        CancellationToken cancellationToken)
    {
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var symbolsToInspect = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            firstHopSymbol
        };

        foreach (var implementationSymbol in PackAnalysisHelpers.FindImplementationSymbolsForService(scanResult, firstHopSymbol))
        {
            symbolsToInspect.Add(implementationSymbol);
            if (!string.Equals(implementationSymbol, rootSymbol, StringComparison.OrdinalIgnoreCase)
                && PackAnalysisHelpers.IsSystemExpansionRelevantName(PackExtractionConventions.GetSimpleTypeName(implementationSymbol)))
            {
                targets.Add(implementationSymbol);
            }
        }

        foreach (var symbolToInspect in symbolsToInspect)
        {
            var filePaths = await GetSymbolFilePathsAsync(workspaceRoot, scanResult, symbolToInspect, cancellationToken).ConfigureAwait(false);
            foreach (var target in await DiscoverRouteTargetsAsync(
                         workspaceRoot,
                         scanResult,
                         symbolToInspect,
                         filePaths,
                         cancellationToken)
                     .ConfigureAwait(false))
            {
                if (!string.Equals(target, firstHopSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    targets.Add(target);
                }
            }
        }

        return targets
            .OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<IReadOnlyList<string>> DiscoverRouteTargetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        string ownerSymbol,
        IReadOnlyList<string> filePaths,
        CancellationToken cancellationToken)
    {
        var documents = await ReadSourceDocumentsAsync(workspaceRoot, filePaths, cancellationToken).ConfigureAwait(false);
        if (documents.Count == 0)
        {
            return [];
        }

        var ownerClassName = PackExtractionConventions.GetSimpleTypeName(ownerSymbol);
        var knownTypes = ExtractKnownTypes(documents, ownerClassName);
        var discovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var helperBodies = documents
            .SelectMany(static document => ExtractMethodBodies(document.Content))
            .GroupBy(static body => body.Name, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);

        foreach (var dependency in documents
                     .SelectMany(document => PackAnalysisHelpers.ExtractConstructorDependencyTypeNames(document.Content, ownerClassName))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            AddResolvedSymbol(discovered, scanResult, dependency, ownerSymbol);
        }

        foreach (var document in documents)
        {
            var localTypes = ExtractLocalTypes(document.Content, knownTypes);
            AddDocumentTargets(discovered, scanResult, ownerSymbol, document.Content, knownTypes, localTypes);
            AddForwardedHelperTargets(
                discovered,
                scanResult,
                ownerSymbol,
                document.Content,
                helperBodies,
                knownTypes,
                HelperForwardingDepth);
        }

        return discovered
            .OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddDocumentTargets(
        ISet<string> discovered,
        WorkspaceScanResult scanResult,
        string ownerSymbol,
        string source,
        IReadOnlyDictionary<string, string> knownTypes,
        IReadOnlyDictionary<string, string> localTypes)
    {
        foreach (var referenced in PackAnalysisHelpers.FindReferencedTypeCandidates(
                     scanResult,
                     source,
                     PackAnalysisHelpers.IsSystemExpansionRelevantName))
        {
            if (string.IsNullOrWhiteSpace(referenced.ResolvedSymbol)
                || string.Equals(referenced.ResolvedSymbol, ownerSymbol, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            discovered.Add(referenced.ResolvedSymbol);
        }

        foreach (Match match in MethodCallRegex.Matches(source))
        {
            var rawTarget = match.Groups["target"].Value.Trim();
            var targetTypeName = ResolveTargetTypeName(rawTarget, knownTypes, localTypes);
            if (string.IsNullOrWhiteSpace(targetTypeName))
            {
                continue;
            }

            AddResolvedSymbol(discovered, scanResult, targetTypeName, ownerSymbol);
        }
    }

    private static void AddForwardedHelperTargets(
        ISet<string> discovered,
        WorkspaceScanResult scanResult,
        string ownerSymbol,
        string source,
        IReadOnlyDictionary<string, HelperMethodBody> helperBodies,
        IReadOnlyDictionary<string, string> knownTypes,
        int remainingDepth)
    {
        if (remainingDepth <= 0 || helperBodies.Count == 0)
        {
            return;
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in SelfMethodCallRegex.Matches(source))
        {
            if (IsMethodDeclarationMatch(source, match))
            {
                continue;
            }

            var methodName = match.Groups["method"].Value;
            if (!ShouldFollowHelper(methodName, helperBodies, visited))
            {
                continue;
            }

            var helperBody = helperBodies[methodName];
            var localTypes = ExtractLocalTypes(helperBody.Body, knownTypes);
            AddDocumentTargets(discovered, scanResult, ownerSymbol, helperBody.Body, knownTypes, localTypes);
            AddForwardedHelperTargets(
                discovered,
                scanResult,
                ownerSymbol,
                helperBody.Body,
                helperBodies,
                knownTypes,
                remainingDepth - 1);
        }
    }

    private static bool ShouldFollowHelper(
        string methodName,
        IReadOnlyDictionary<string, HelperMethodBody> helperBodies,
        ISet<string> visited)
    {
        if (!helperBodies.ContainsKey(methodName))
        {
            return false;
        }

        if (string.Equals(methodName, "if", StringComparison.Ordinal)
            || string.Equals(methodName, "for", StringComparison.Ordinal)
            || string.Equals(methodName, "foreach", StringComparison.Ordinal)
            || string.Equals(methodName, "while", StringComparison.Ordinal)
            || string.Equals(methodName, "switch", StringComparison.Ordinal)
            || string.Equals(methodName, "catch", StringComparison.Ordinal)
            || string.Equals(methodName, "return", StringComparison.Ordinal))
        {
            return false;
        }

        return visited.Add(methodName);
    }

    private static bool IsMethodDeclarationMatch(string source, Match match)
    {
        var lineStartIndex = source.LastIndexOf('\n', Math.Max(0, match.Index - 1));
        var segmentStart = lineStartIndex < 0 ? 0 : lineStartIndex + 1;
        var segment = source[segmentStart..match.Index];

        return Regex.IsMatch(
            segment,
            @"^\s*(?:(?:public|private|internal|protected|static|sealed|virtual|override|async|partial|extern|new)\s+)*[A-Za-z_][A-Za-z0-9_<>\.\?,\[\]]*\s+$",
            RegexOptions.CultureInvariant);
    }

    private static void AddResolvedSymbol(
        ISet<string> discovered,
        WorkspaceScanResult scanResult,
        string targetTypeName,
        string ownerSymbol)
    {
        var resolved = PackAnalysisHelpers.ResolveTypeSymbol(scanResult, targetTypeName);
        if (string.IsNullOrWhiteSpace(resolved)
            || string.Equals(resolved, ownerSymbol, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!PackAnalysisHelpers.IsSystemExpansionRelevantName(PackExtractionConventions.GetSimpleTypeName(resolved)))
        {
            return;
        }

        discovered.Add(resolved);
        foreach (var implementationSymbol in PackAnalysisHelpers.FindImplementationSymbolsForService(scanResult, resolved))
        {
            if (PackAnalysisHelpers.IsSystemExpansionRelevantName(PackExtractionConventions.GetSimpleTypeName(implementationSymbol)))
            {
                discovered.Add(implementationSymbol);
            }
        }
    }

    private async Task<IReadOnlyList<string>> GetSymbolFilePathsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        string symbol,
        CancellationToken cancellationToken)
    {
        var key = $"{workspaceRoot}|{symbol}";
        if (documentCache.TryGetValue(key, out var cached))
        {
            return cached.Select(static document => document.RelativePath).ToArray();
        }

        var aggregate = PartialSymbolAggregateResolver.Build(scanResult, [symbol]);
        if (!aggregate.TryGetValue(symbol, out var resolved))
        {
            return [];
        }

        await ReadSourceDocumentsAsync(workspaceRoot, resolved.FilePaths, cancellationToken).ConfigureAwait(false);
        return resolved.FilePaths;
    }

    private async Task<IReadOnlyList<SystemRouteSourceDocument>> ReadSourceDocumentsAsync(
        string workspaceRoot,
        IReadOnlyList<string> filePaths,
        CancellationToken cancellationToken)
    {
        var documents = new List<SystemRouteSourceDocument>();
        foreach (var relativePath in filePaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var key = $"{workspaceRoot}|{relativePath}";
            if (!documentCache.TryGetValue(key, out var cachedDocuments))
            {
                var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                var source = await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
                cachedDocuments = [new SystemRouteSourceDocument(relativePath, source)];
                documentCache[key] = cachedDocuments;
            }

            documents.AddRange(cachedDocuments);
        }

        return documents;
    }

    private static Dictionary<string, string> ExtractKnownTypes(
        IReadOnlyList<SystemRouteSourceDocument> documents,
        string ownerClassName)
    {
        var knownTypes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var source in documents.Select(static document => document.Content))
        {
            foreach (Match match in FieldRegex.Matches(source))
            {
                knownTypes[match.Groups["name"].Value] = match.Groups["type"].Value;
            }

            foreach (Match match in PropertyRegex.Matches(source))
            {
                knownTypes[match.Groups["name"].Value] = match.Groups["type"].Value;
            }

            foreach (var pair in ExtractConstructorParameterTypes(source, ownerClassName))
            {
                knownTypes[pair.Key] = pair.Value;
            }
        }

        return knownTypes;
    }

    private static Dictionary<string, string> ExtractLocalTypes(
        string source,
        IReadOnlyDictionary<string, string> knownTypes)
    {
        var localTypes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in LocalRegex.Matches(source))
        {
            var localName = match.Groups["name"].Value;
            var declaredType = match.Groups["type"].Value;
            var rightHandSide = match.Groups["rhs"].Value.Trim();
            var resolvedType = ResolveTargetTypeName(declaredType, knownTypes, localTypes)
                ?? ResolveTargetTypeName(rightHandSide, knownTypes, localTypes);
            if (!string.IsNullOrWhiteSpace(resolvedType))
            {
                localTypes[localName] = resolvedType;
            }
        }

        return localTypes;
    }

    private static Dictionary<string, string> ExtractConstructorParameterTypes(
        string source,
        string className)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        var match = Regex.Match(
            source,
            $@"(?:public|internal|protected|private)\s+{Regex.Escape(className)}\s*\((?<params>.*?)\)",
            RegexOptions.CultureInvariant | RegexOptions.Singleline);
        if (!match.Success)
        {
            return parameters;
        }

        foreach (var parameter in match.Groups["params"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tokens = parameter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length < 2)
            {
                continue;
            }

            parameters[tokens[^1]] = tokens[^2];
        }

        return parameters;
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

        if (!string.Equals(rawTarget, "var", StringComparison.Ordinal))
        {
            var normalized = rawTarget.StartsWith("this.", StringComparison.Ordinal)
                ? rawTarget["this.".Length..]
                : rawTarget;
            if (localTypes.TryGetValue(normalized, out var localType))
            {
                return localType;
            }

            if (knownTypes.TryGetValue(normalized, out var knownType))
            {
                return knownType;
            }

            if (IdentifierRegex.IsMatch(rawTarget))
            {
                return null;
            }

            return rawTarget;
        }

        return null;
    }

    private static IReadOnlyList<HelperMethodBody> ExtractMethodBodies(string source)
    {
        var helperBodies = new List<HelperMethodBody>();
        var methodHeaderRegex = new Regex(
            @"(?:(?:public|private|internal|protected)\s+(?:static\s+)?[A-Za-z0-9_<>\.\?\[\],\s]+\s+(?<name>[A-Za-z_]\w*)\s*\([^)]*\)\s*)\{",
            RegexOptions.Compiled);

        foreach (Match match in methodHeaderRegex.Matches(source))
        {
            var bodyRange = TryFindBodyRange(source, match.Index + match.Length - 1);
            if (bodyRange is null)
            {
                continue;
            }

            helperBodies.Add(new HelperMethodBody(
                match.Groups["name"].Value,
                source.Substring(
                    bodyRange.Value.BodyStartIndex,
                    bodyRange.Value.EndIndex - bodyRange.Value.BodyStartIndex)));
        }

        return helperBodies;
    }

    private static (int BodyStartIndex, int EndIndex)? TryFindBodyRange(string source, int openingBraceIndex)
    {
        if (openingBraceIndex < 0 || openingBraceIndex >= source.Length || source[openingBraceIndex] != '{')
        {
            return null;
        }

        var depth = 0;
        for (var index = openingBraceIndex; index < source.Length; index++)
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
                return (openingBraceIndex + 1, index);
            }
        }

        return null;
    }
}

internal sealed record SystemRouteSourceDocument(
    string RelativePath,
    string Content);

internal sealed record HelperMethodBody(
    string Name,
    string Body);
