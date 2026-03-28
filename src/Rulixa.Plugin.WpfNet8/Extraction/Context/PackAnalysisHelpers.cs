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
        Func<string, bool> simpleNamePredicate)
    {
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in IdentifierRegex.Matches(source))
        {
            var token = match.Value;
            var simpleName = PackExtractionConventions.GetSimpleTypeName(token);
            if (!simpleNamePredicate(simpleName))
            {
                continue;
            }

            var symbol = ResolveTypeSymbol(scanResult, token)
                ?? ResolveTypeSymbol(scanResult, simpleName);
            if (!string.IsNullOrWhiteSpace(symbol))
            {
                referenced.Add(symbol);
            }
        }

        return referenced
            .OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
