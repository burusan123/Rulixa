using Rulixa.Application.Ports;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class XamlSnippetCandidateFactory
{
    private const int MaxSnippetLines = 80;
    private const int LineWindowBefore = 5;
    private const int LineWindowAfter = 20;
    private readonly IWorkspaceFileSystem workspaceFileSystem;

    internal XamlSnippetCandidateFactory(IWorkspaceFileSystem workspaceFileSystem)
    {
        this.workspaceFileSystem = workspaceFileSystem ?? throw new ArgumentNullException(nameof(workspaceFileSystem));
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
        var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var source = await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
        var range = BuildLineWindow(source, sourceSpan);
        var content = ExtractContent(source, range);

        return new SnippetSelectionCandidate(
            relativePath,
            reason,
            priority,
            required,
            anchor,
            range.StartLine,
            range.EndLine,
            content);
    }

    private static SourceSpan BuildLineWindow(string source, SourceSpan sourceSpan)
    {
        var lines = SplitLines(source);
        var startLine = Math.Max(1, sourceSpan.StartLine - LineWindowBefore);
        var endLine = Math.Min(lines.Count, sourceSpan.EndLine + LineWindowAfter);
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

    private static IReadOnlyList<string> SplitLines(string source) =>
        source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
}
