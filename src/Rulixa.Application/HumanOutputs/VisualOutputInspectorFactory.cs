using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.HumanOutputs;

internal static class VisualOutputInspectorFactory
{
    public static IReadOnlyDictionary<string, VisualInspectorItem> Create(
        ContextPack contextPack,
        WorkspaceScanResult scanResult,
        HumanOutputFactSet facts)
    {
        var items = new Dictionary<string, VisualInspectorItem>(StringComparer.Ordinal);
        AddFileInspectorItems(items, contextPack);
        AddSnippetInspectorItems(items, contextPack);
        AddUnknownInspectorItems(items, contextPack);
        AddRootInspectorItem(items, facts);
        AddDiagnosticInspectorItem(items, scanResult);
        return items;
    }

    private static void AddFileInspectorItems(
        IDictionary<string, VisualInspectorItem> items,
        ContextPack contextPack)
    {
        for (var index = 0; index < contextPack.SelectedFiles.Count; index++)
        {
            var file = contextPack.SelectedFiles[index];
            items[$"file-{index}"] = new VisualInspectorItem(
                Id: $"file-{index}",
                Title: VisualOutputFormatting.NormalizeDisplayPath(file.Path) ?? file.Path,
                Category: "file",
                Facts:
                [
                    $"reason: {file.Reason}",
                    $"required: {file.IsRequired}",
                    $"line_count: {file.LineCount}"
                ],
                FilePath: VisualOutputFormatting.NormalizeDisplayPath(file.Path),
                Symbol: null,
                Snippet: null,
                Reason: file.Reason,
                Candidates: []);
        }
    }

    private static void AddSnippetInspectorItems(
        IDictionary<string, VisualInspectorItem> items,
        ContextPack contextPack)
    {
        for (var index = 0; index < contextPack.SelectedSnippets.Count; index++)
        {
            var snippet = contextPack.SelectedSnippets[index];
            items[$"snippet-{index}"] = new VisualInspectorItem(
                Id: $"snippet-{index}",
                Title: $"{VisualOutputFormatting.NormalizeDisplayPath(snippet.Path)}:{snippet.StartLine}-{snippet.EndLine}",
                Category: "snippet",
                Facts:
                [
                    $"reason: {snippet.Reason}",
                    $"required: {snippet.IsRequired}",
                    $"anchor: {snippet.Anchor}"
                ],
                FilePath: VisualOutputFormatting.NormalizeDisplayPath(snippet.Path),
                Symbol: snippet.Anchor,
                Snippet: snippet.Content,
                Reason: snippet.Reason,
                Candidates: []);
        }
    }

    private static void AddUnknownInspectorItems(
        IDictionary<string, VisualInspectorItem> items,
        ContextPack contextPack)
    {
        for (var index = 0; index < contextPack.Unknowns.Count; index++)
        {
            var unknown = contextPack.Unknowns[index];
            items[$"unknown-{index}"] = new VisualInspectorItem(
                Id: $"unknown-{index}",
                Title: unknown.Code,
                Category: "unknown",
                Facts:
                [
                    $"severity: {unknown.Severity}",
                    $"message: {VisualOutputFormatting.SanitizeUnknownMessage(unknown.Message)}"
                ],
                FilePath: null,
                Symbol: null,
                Snippet: null,
                Reason: VisualOutputFormatting.SanitizeUnknownMessage(unknown.Message),
                Candidates: unknown.Candidates);
        }
    }

    private static void AddRootInspectorItem(
        IDictionary<string, VisualInspectorItem> items,
        HumanOutputFactSet facts)
    {
        if (string.IsNullOrWhiteSpace(facts.ResolvedPath) && string.IsNullOrWhiteSpace(facts.ResolvedSymbol))
        {
            return;
        }

        items["root-entry"] = new VisualInspectorItem(
            Id: "root-entry",
            Title: facts.Entry,
            Category: "root",
            Facts:
            [
                $"resolved_kind: {facts.ResolvedKind}",
                $"goal: {facts.Goal}"
            ],
            FilePath: facts.ResolvedPath,
            Symbol: facts.ResolvedSymbol,
            Snippet: facts.SystemSummary,
            Reason: "root entry",
            Candidates: facts.NextCandidates);
    }

    private static void AddDiagnosticInspectorItem(
        IDictionary<string, VisualInspectorItem> items,
        WorkspaceScanResult scanResult)
    {
        if (scanResult.Diagnostics.Count == 0)
        {
            return;
        }

        items["scan-diagnostics"] = new VisualInspectorItem(
            Id: "scan-diagnostics",
            Title: "Scan Diagnostics",
            Category: "diagnostics",
            Facts: scanResult.Diagnostics
                .Take(6)
                .Select(static diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")
                .ToArray(),
            FilePath: null,
            Symbol: null,
            Snippet: null,
            Reason: "scan diagnostics",
            Candidates: scanResult.Diagnostics
                .SelectMany(static diagnostic => diagnostic.Candidates)
                .Distinct(StringComparer.Ordinal)
                .Take(5)
                .ToArray());
    }
}
