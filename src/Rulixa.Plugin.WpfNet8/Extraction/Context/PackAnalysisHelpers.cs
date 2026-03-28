using System.Text.RegularExpressions;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class PackAnalysisHelpers
{
    private static readonly Regex IdentifierRegex = new(@"[A-Za-z_][A-Za-z0-9_\.]*", RegexOptions.Compiled);

    internal static IReadOnlyList<string> ExtractConstructorDependencyTypeNames(string source, string className)
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

    internal static string? ResolveTypeSymbol(WorkspaceScanResult scanResult, string targetTypeName)
    {
        if (scanResult.Symbols.Any(symbol => string.Equals(symbol.QualifiedName, targetTypeName, StringComparison.OrdinalIgnoreCase)))
        {
            return scanResult.Symbols
                .First(symbol => string.Equals(symbol.QualifiedName, targetTypeName, StringComparison.OrdinalIgnoreCase))
                .QualifiedName;
        }

        var simpleTypeName = PackExtractionConventions.GetSimpleTypeName(targetTypeName);
        var matches = scanResult.Symbols
            .Where(symbol => symbol.Kind is SymbolKind.Class or SymbolKind.Interface or SymbolKind.Window)
            .Where(symbol => string.Equals(symbol.DisplayName, simpleTypeName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static symbol => symbol.QualifiedName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return matches.Length == 1 ? matches[0].QualifiedName : null;
    }

    internal static IReadOnlyList<string> FindReferencedTypeSymbols(
        WorkspaceScanResult scanResult,
        string source,
        Func<string, bool> simpleNamePredicate) =>
        FindReferencedTypeCandidates(scanResult, source, simpleNamePredicate)
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate.ResolvedSymbol))
            .Select(static candidate => candidate.ResolvedSymbol!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    internal static IReadOnlyList<TypeReferenceCandidate> FindReferencedTypeCandidates(
        WorkspaceScanResult scanResult,
        string source,
        Func<string, bool> simpleNamePredicate)
    {
        var referenced = new List<TypeReferenceCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in IdentifierRegex.Matches(source))
        {
            var token = match.Value;
            var simpleName = PackExtractionConventions.GetSimpleTypeName(token);
            if (!simpleNamePredicate(simpleName))
            {
                continue;
            }

            var candidates = FindTypeSymbolsBySimpleName(scanResult, simpleName);
            var resolvedSymbol = ResolveTypeSymbol(scanResult, token)
                ?? ResolveTypeSymbol(scanResult, simpleName);
            var key = $"{token}|{simpleName}|{resolvedSymbol}|{string.Join("|", candidates)}";
            if (!seen.Add(key))
            {
                continue;
            }

            referenced.Add(new TypeReferenceCandidate(token, simpleName, resolvedSymbol, candidates));
        }

        return referenced;
    }

    internal static bool IsPersistenceLikeName(string simpleName) =>
        simpleName.EndsWith("Repository", StringComparison.Ordinal)
        || simpleName.EndsWith("Query", StringComparison.Ordinal)
        || simpleName.EndsWith("Saver", StringComparison.Ordinal)
        || simpleName.EndsWith("Store", StringComparison.Ordinal)
        || simpleName.EndsWith("Settings", StringComparison.Ordinal)
        || simpleName.EndsWith("SettingsQuery", StringComparison.Ordinal)
        || simpleName.EndsWith("SettingsSaver", StringComparison.Ordinal);

    internal static bool IsWorkflowLikeName(string simpleName) =>
        simpleName.EndsWith("Service", StringComparison.Ordinal)
        || simpleName.EndsWith("Workflow", StringComparison.Ordinal)
        || simpleName.EndsWith("UseCase", StringComparison.Ordinal)
        || simpleName.EndsWith("Port", StringComparison.Ordinal)
        || simpleName.EndsWith("Adapter", StringComparison.Ordinal)
        || IsPersistenceLikeName(simpleName);

    internal static bool IsAlgorithmLikeName(string simpleName) =>
        simpleName.EndsWith("AlgorithmRunner", StringComparison.Ordinal)
        || simpleName.EndsWith("Algorithm", StringComparison.Ordinal);

    internal static bool IsAnalyzerLikeName(string simpleName) =>
        simpleName.EndsWith("Analyzer", StringComparison.Ordinal)
        || simpleName.EndsWith("ExecutionPlan", StringComparison.Ordinal)
        || simpleName.EndsWith("Pipeline", StringComparison.Ordinal);

    internal static bool IsUiBoundaryLikeName(string simpleName) =>
        simpleName.EndsWith("Window", StringComparison.Ordinal)
        || simpleName.EndsWith("View", StringComparison.Ordinal)
        || simpleName.EndsWith("Dialog", StringComparison.Ordinal)
        || simpleName.EndsWith("Renderer", StringComparison.Ordinal)
        || simpleName.EndsWith("Overlay", StringComparison.Ordinal)
        || simpleName.Contains("FileDialog", StringComparison.Ordinal)
        || simpleName.Contains("Prompt", StringComparison.Ordinal)
        || simpleName.Contains("ErrorMessage", StringComparison.Ordinal)
        || simpleName.Contains("UiPort", StringComparison.Ordinal);

    internal static bool IsHubObjectLikeName(string simpleName) =>
        simpleName.EndsWith("Document", StringComparison.Ordinal)
        || simpleName.EndsWith("State", StringComparison.Ordinal)
        || simpleName.EndsWith("Context", StringComparison.Ordinal)
        || simpleName.EndsWith("Session", StringComparison.Ordinal)
        || simpleName.EndsWith("Workspace", StringComparison.Ordinal);

    internal static bool HasHubObjectSignals(string source) =>
        source.Contains("IsDirty", StringComparison.Ordinal)
        || source.Contains("DirtyStateChanged", StringComparison.Ordinal)
        || source.Contains("CreateSnapshot", StringComparison.Ordinal)
        || source.Contains("RestoreFromSnapshot", StringComparison.Ordinal)
        || source.Contains("MarkDirty", StringComparison.Ordinal)
        || source.Contains("MarkSaved", StringComparison.Ordinal);

    internal static string NormalizeTypeIdentity(string symbolOrName)
    {
        var simpleName = PackExtractionConventions.GetSimpleTypeName(symbolOrName);
        return simpleName.Length > 1 && simpleName[0] == 'I' && char.IsUpper(simpleName[1])
            ? simpleName[1..]
            : simpleName;
    }

    internal static bool HasSameTypeIdentity(string left, string right) =>
        string.Equals(
            NormalizeTypeIdentity(left),
            NormalizeTypeIdentity(right),
            StringComparison.OrdinalIgnoreCase);

    internal static string ClassifyWorkflowFamily(string symbolOrName)
    {
        var simpleName = PackExtractionConventions.GetSimpleTypeName(symbolOrName);
        if (IsUiBoundaryLikeName(simpleName))
        {
            return "ui";
        }

        if (simpleName.EndsWith("UseCase", StringComparison.Ordinal))
        {
            return "use-case";
        }

        if (simpleName.EndsWith("Workflow", StringComparison.Ordinal))
        {
            return "workflow";
        }

        if (simpleName.EndsWith("Service", StringComparison.Ordinal))
        {
            return "service";
        }

        if (simpleName.EndsWith("Adapter", StringComparison.Ordinal))
        {
            return "adapter";
        }

        if (simpleName.EndsWith("Port", StringComparison.Ordinal))
        {
            return "port";
        }

        if (simpleName.EndsWith("Loader", StringComparison.Ordinal))
        {
            return "loader";
        }

        if (simpleName.EndsWith("Query", StringComparison.Ordinal))
        {
            return "query";
        }

        if (simpleName.EndsWith("Repository", StringComparison.Ordinal))
        {
            return "repository";
        }

        if (simpleName.EndsWith("Saver", StringComparison.Ordinal))
        {
            return "saver";
        }

        if (simpleName.EndsWith("Store", StringComparison.Ordinal))
        {
            return "store";
        }

        if (simpleName.Contains("Settings", StringComparison.Ordinal))
        {
            return "settings";
        }

        return "other";
    }

    internal static string ClassifyPersistenceFamily(string symbolOrName)
    {
        var simpleName = PackExtractionConventions.GetSimpleTypeName(symbolOrName);
        if (simpleName.EndsWith("Repository", StringComparison.Ordinal))
        {
            return "repository";
        }

        if (simpleName.EndsWith("Query", StringComparison.Ordinal))
        {
            return "query";
        }

        if (simpleName.EndsWith("Saver", StringComparison.Ordinal))
        {
            return "saver";
        }

        if (simpleName.EndsWith("Store", StringComparison.Ordinal))
        {
            return "store";
        }

        if (simpleName.Contains("Settings", StringComparison.Ordinal))
        {
            return "settings";
        }

        return "other";
    }

    internal static string ClassifyHubObjectFamily(string symbolOrName)
    {
        var simpleName = PackExtractionConventions.GetSimpleTypeName(symbolOrName);
        if (simpleName.EndsWith("Document", StringComparison.Ordinal))
        {
            return "document";
        }

        if (simpleName.EndsWith("Session", StringComparison.Ordinal))
        {
            return "session";
        }

        if (simpleName.EndsWith("State", StringComparison.Ordinal))
        {
            return "state";
        }

        if (simpleName.EndsWith("Context", StringComparison.Ordinal))
        {
            return "context";
        }

        if (simpleName.EndsWith("Workspace", StringComparison.Ordinal))
        {
            return "workspace";
        }

        return "other";
    }

    internal static string ClassifyAssetFamily(IEnumerable<string> descriptors)
    {
        var descriptorSet = descriptors.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (descriptorSet.Contains(".onnx"))
        {
            return "onnx-model";
        }

        if (descriptorSet.Contains(".xlsx"))
        {
            return "excel";
        }

        if (descriptorSet.Contains(".json"))
        {
            return "json";
        }

        if (descriptorSet.Contains(".pdf"))
        {
            return "pdf";
        }

        if (descriptorSet.Contains(".template"))
        {
            return "template";
        }

        return "other";
    }

    internal static string ClassifyDownstreamFamily(IEnumerable<string> symbols)
    {
        var familySet = symbols
            .Select(ClassifySingleDownstreamFamily)
            .Where(static family => family != "other")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (familySet.Length == 0)
        {
            return "none";
        }

        var priority = new[]
        {
            "algorithm",
            "analyzer",
            "persistence",
            "hub-object",
            "settings",
            "workflow",
            "ui"
        };
        foreach (var family in priority)
        {
            if (familySet.Contains(family, StringComparer.OrdinalIgnoreCase))
            {
                return family;
            }
        }

        return familySet[0];
    }

    internal static PartialSymbolAggregate? ResolveAggregate(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        string symbol)
    {
        if (relevantContext.SymbolAggregates.TryGetValue(symbol, out var aggregate))
        {
            return aggregate;
        }

        var resolved = PartialSymbolAggregateResolver.Build(scanResult, [symbol]);
        return resolved.TryGetValue(symbol, out aggregate) ? aggregate : null;
    }

    internal static IReadOnlyList<string> GetSymbolFilePaths(
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        string symbol) =>
        ResolveAggregate(scanResult, relevantContext, symbol)?.FilePaths ?? [];

    internal static IReadOnlyList<string> FindTypeSymbolsBySimpleName(
        WorkspaceScanResult scanResult,
        string simpleName) =>
        scanResult.Symbols
            .Where(symbol => symbol.Kind is SymbolKind.Class or SymbolKind.Interface or SymbolKind.Window)
            .Where(symbol => string.Equals(symbol.DisplayName, simpleName, StringComparison.OrdinalIgnoreCase))
            .Select(static symbol => symbol.QualifiedName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    internal static IReadOnlyList<string> FindImplementationSymbolsForService(
        WorkspaceScanResult scanResult,
        string serviceSymbol)
    {
        return scanResult.ServiceRegistrations
            .Where(registration =>
                string.Equals(registration.ServiceType, serviceSymbol, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(registration.ServiceType, registration.ImplementationType, StringComparison.OrdinalIgnoreCase))
            .Select(static registration => registration.ImplementationType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static int CountServiceRegistrationMatches(
        WorkspaceScanResult scanResult,
        IEnumerable<string> symbols)
    {
        var symbolSet = symbols
            .Where(static symbol => !string.IsNullOrWhiteSpace(symbol))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (symbolSet.Count == 0)
        {
            return 0;
        }

        return scanResult.ServiceRegistrations.Count(registration =>
            symbolSet.Contains(registration.ServiceType)
            || symbolSet.Contains(registration.ImplementationType));
    }

    internal static int CountFileKindMatches(
        WorkspaceScanResult scanResult,
        IEnumerable<string> filePaths,
        params ScanFileKind[] expectedKinds)
    {
        var expected = expectedKinds.ToHashSet();
        return filePaths
            .Select(path => scanResult.Files.FirstOrDefault(file => string.Equals(file.Path, path, StringComparison.OrdinalIgnoreCase)))
            .Where(static file => file is not null)
            .Count(file => expected.Contains(file!.Kind));
    }

    private static string ClassifySingleDownstreamFamily(string symbolOrName)
    {
        var simpleName = PackExtractionConventions.GetSimpleTypeName(symbolOrName);
        if (IsUiBoundaryLikeName(simpleName))
        {
            return "ui";
        }

        if (IsAlgorithmLikeName(simpleName))
        {
            return "algorithm";
        }

        if (IsAnalyzerLikeName(simpleName))
        {
            return "analyzer";
        }

        if (IsPersistenceLikeName(simpleName))
        {
            return simpleName.Contains("Settings", StringComparison.Ordinal) ? "settings" : "persistence";
        }

        if (IsHubObjectLikeName(simpleName))
        {
            return "hub-object";
        }

        if (IsWorkflowLikeName(simpleName))
        {
            return "workflow";
        }

        return "other";
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

    private static IEnumerable<string> SplitTopLevel(string value, char separator)
    {
        var startIndex = 0;
        var angleDepth = 0;
        var parenDepth = 0;
        for (var index = 0; index < value.Length; index++)
        {
            switch (value[index])
            {
                case '<':
                    angleDepth++;
                    break;
                case '>':
                    angleDepth = Math.Max(0, angleDepth - 1);
                    break;
                case '(':
                    parenDepth++;
                    break;
                case ')':
                    parenDepth = Math.Max(0, parenDepth - 1);
                    break;
                default:
                    if (value[index] == separator && angleDepth == 0 && parenDepth == 0)
                    {
                        yield return value[startIndex..index];
                        startIndex = index + 1;
                    }
                    break;
            }
        }

        if (startIndex < value.Length)
        {
            yield return value[startIndex..];
        }
    }

    private static string ExtractParameterTypeExpression(string parameter)
    {
        var normalized = parameter.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var equalsIndex = normalized.IndexOf('=');
        if (equalsIndex >= 0)
        {
            normalized = normalized[..equalsIndex].Trim();
        }

        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length <= 1)
        {
            return tokens.FirstOrDefault() ?? string.Empty;
        }

        var modifiers = new HashSet<string>(StringComparer.Ordinal)
        {
            "this", "ref", "out", "in", "params"
        };
        return tokens
            .Take(tokens.Length - 1)
            .FirstOrDefault(token => !modifiers.Contains(token))
            ?? string.Empty;
    }

    private static IEnumerable<string> ExtractRelevantTypeNames(string typeExpression)
    {
        if (string.IsNullOrWhiteSpace(typeExpression))
        {
            return [];
        }

        var cleaned = typeExpression.Trim();
        if (cleaned.EndsWith("?", StringComparison.Ordinal))
        {
            cleaned = cleaned[..^1];
        }

        var collectionMatch = Regex.Match(cleaned, @"<(?<inner>[A-Za-z_][A-Za-z0-9_<>\.\?,\[\]]*)>");
        if (collectionMatch.Success)
        {
            cleaned = collectionMatch.Groups["inner"].Value;
        }

        return [cleaned];
    }
}

internal sealed record TypeReferenceCandidate(
    string Token,
    string SimpleName,
    string? ResolvedSymbol,
    IReadOnlyList<string> CandidateSymbols)
{
    internal bool IsAmbiguous => string.IsNullOrWhiteSpace(ResolvedSymbol) && CandidateSymbols.Count > 1;
}
