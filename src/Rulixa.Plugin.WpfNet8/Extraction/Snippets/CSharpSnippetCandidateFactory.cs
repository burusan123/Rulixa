using System.Text.RegularExpressions;
using Rulixa.Application.Ports;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class CSharpSnippetCandidateFactory
{
    private const int MaxSnippetLines = 80;
    private const int LineWindowBefore = 5;
    private const int LineWindowAfter = 20;
    private readonly IWorkspaceFileSystem workspaceFileSystem;

    internal CSharpSnippetCandidateFactory(IWorkspaceFileSystem workspaceFileSystem)
    {
        this.workspaceFileSystem = workspaceFileSystem ?? throw new ArgumentNullException(nameof(workspaceFileSystem));
    }

    internal async Task<SnippetSelectionCandidate?> CreateMethodSnippetAsync(
        string workspaceRoot,
        string relativePath,
        string methodName,
        string reason,
        int priority,
        bool required,
        string anchor,
        SourceSpan? fallbackSpan,
        CancellationToken cancellationToken)
    {
        var source = await ReadSourceAsync(workspaceRoot, relativePath, cancellationToken).ConfigureAwait(false);
        var range = TryFindMethodRange(source, methodName)
            ?? TryBuildLineWindow(source, fallbackSpan);
        return range is null
            ? null
            : BuildCandidate(relativePath, reason, priority, required, anchor, source, range.Value);
    }

    internal async Task<SnippetSelectionCandidate?> CreateConstructorSnippetAsync(
        string workspaceRoot,
        string relativePath,
        string className,
        string reason,
        int priority,
        bool required,
        string anchor,
        CancellationToken cancellationToken)
    {
        var source = await ReadSourceAsync(workspaceRoot, relativePath, cancellationToken).ConfigureAwait(false);
        var range = TryFindConstructorRange(source, className);
        return range is null
            ? null
            : BuildCandidate(relativePath, reason, priority, required, anchor, source, range.Value);
    }

    internal async Task<SnippetSelectionCandidate?> CreateLineWindowSnippetAsync(
        string workspaceRoot,
        string relativePath,
        string reason,
        int priority,
        bool required,
        string anchor,
        SourceSpan sourceSpan,
        CancellationToken cancellationToken)
    {
        var source = await ReadSourceAsync(workspaceRoot, relativePath, cancellationToken).ConfigureAwait(false);
        var range = TryBuildLineWindow(source, sourceSpan);
        return range is null
            ? null
            : BuildCandidate(relativePath, reason, priority, required, anchor, source, range.Value);
    }

    private async Task<string> ReadSourceAsync(
        string workspaceRoot,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
    }

    private static SnippetSelectionCandidate BuildCandidate(
        string relativePath,
        string reason,
        int priority,
        bool required,
        string anchor,
        string source,
        SourceSpan range)
    {
        var boundedRange = BoundRange(source, range);
        var content = ExtractContent(source, boundedRange);
        return new SnippetSelectionCandidate(
            relativePath,
            reason,
            priority,
            required,
            anchor,
            boundedRange.StartLine,
            boundedRange.EndLine,
            content);
    }

    private static SourceSpan BoundRange(string source, SourceSpan range)
    {
        var lines = SplitLines(source);
        var startLine = Math.Max(1, range.StartLine);
        var endLine = Math.Min(lines.Count, range.EndLine);
        if (endLine < startLine)
        {
            endLine = startLine;
        }

        if (endLine - startLine + 1 > MaxSnippetLines)
        {
            endLine = startLine + MaxSnippetLines - 1;
        }

        return new SourceSpan(startLine, endLine);
    }

    private static string ExtractContent(string source, SourceSpan range)
    {
        var lines = SplitLines(source);
        return string.Join("\n", lines.Skip(range.StartLine - 1).Take(range.EndLine - range.StartLine + 1));
    }

    private static SourceSpan? TryFindMethodRange(string source, string methodName)
    {
        var lines = SplitLines(source);
        var pattern = new Regex(
            $@"^\s*(?:(?:public|private|internal|protected|static|virtual|override|sealed|async|partial|new|unsafe|extern)\s+)+(?:[A-Za-z_][A-Za-z0-9_<>,\.\?\[\]]*\s+)+{Regex.Escape(methodName)}\s*\(",
            RegexOptions.CultureInvariant);
        for (var index = 0; index < lines.Count; index++)
        {
            if (!pattern.IsMatch(lines[index]))
            {
                continue;
            }

            return TryFindMemberBodyRange(source, lines, index + 1);
        }

        return null;
    }

    private static SourceSpan? TryFindConstructorRange(string source, string className)
    {
        var lines = SplitLines(source);
        var pattern = new Regex(
            $@"^\s*(?:(?:public|private|internal|protected|static|unsafe|extern)\s+)*{Regex.Escape(className)}\s*\(",
            RegexOptions.CultureInvariant);
        for (var index = 0; index < lines.Count; index++)
        {
            if (!pattern.IsMatch(lines[index]))
            {
                continue;
            }

            return TryFindMemberBodyRange(source, lines, index + 1);
        }

        return null;
    }

    private static SourceSpan? TryFindMemberBodyRange(string source, IReadOnlyList<string> lines, int startLine)
    {
        var startIndex = GetLineStartIndex(source, startLine);
        if (startIndex < 0)
        {
            return null;
        }

        var braceIndex = source.IndexOf('{', startIndex);
        var expressionIndex = source.IndexOf("=>", startIndex, StringComparison.Ordinal);
        if (expressionIndex >= 0 && (braceIndex < 0 || expressionIndex < braceIndex))
        {
            var semicolonIndex = source.IndexOf(';', expressionIndex);
            if (semicolonIndex < 0)
            {
                return new SourceSpan(startLine, Math.Min(lines.Count, startLine + MaxSnippetLines - 1));
            }

            return new SourceSpan(startLine, GetLineNumberAt(source, semicolonIndex));
        }

        if (braceIndex < 0)
        {
            return new SourceSpan(startLine, Math.Min(lines.Count, startLine + MaxSnippetLines - 1));
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
                return new SourceSpan(startLine, GetLineNumberAt(source, index));
            }
        }

        return new SourceSpan(startLine, Math.Min(lines.Count, startLine + MaxSnippetLines - 1));
    }

    private static SourceSpan? TryBuildLineWindow(string source, SourceSpan? sourceSpan)
    {
        if (sourceSpan is null)
        {
            return null;
        }

        var lines = SplitLines(source);
        var boundedStart = Math.Max(1, sourceSpan.Value.StartLine - LineWindowBefore);
        var boundedEnd = Math.Min(lines.Count, sourceSpan.Value.EndLine + LineWindowAfter);
        return new SourceSpan(boundedStart, boundedEnd);
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

    private static IReadOnlyList<string> SplitLines(string source) =>
        source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

}
