using System.Text;

namespace Rulixa.Cli;

internal sealed class EvidenceBundleDiffRenderer
{
    public string Render(string baseDirectory, EvidenceManifestDto before, string targetDirectory, EvidenceManifestDto after)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var builder = new StringBuilder();
        builder.AppendLine("# Evidence 差分");
        builder.AppendLine();
        builder.AppendLine("## 比較対象");
        builder.AppendLine($"- base: `{NormalizePath(Path.GetFullPath(baseDirectory))}`");
        builder.AppendLine($"- target: `{NormalizePath(Path.GetFullPath(targetDirectory))}`");
        builder.AppendLine();

        AppendSystemPackDiff(builder, before.SelectionSummary.Contracts, after.SelectionSummary.Contracts);
        AppendMetadataDiff(builder, before, after);
        AppendContractDiff(builder, before.SelectionSummary.Contracts, after.SelectionSummary.Contracts);
        AppendSelectedFileDiff(builder, before.SelectionSummary.SelectedFiles, after.SelectionSummary.SelectedFiles);
        AppendSelectedSnippetDiff(builder, before.SelectionSummary.SelectedSnippets, after.SelectionSummary.SelectedSnippets);
        AppendDecisionTraceDiff(builder, before.DecisionTraces, after.DecisionTraces);

        return builder.ToString();
    }

    private static void AppendSystemPackDiff(
        StringBuilder builder,
        IReadOnlyList<EvidenceContractDto> beforeContracts,
        IReadOnlyList<EvidenceContractDto> afterContracts)
    {
        var beforeSystem = beforeContracts.FirstOrDefault(static contract => contract.Title == "System Pack");
        var afterSystem = afterContracts.FirstOrDefault(static contract => contract.Title == "System Pack");
        if (beforeSystem is null && afterSystem is null)
        {
            return;
        }

        builder.AppendLine("## システム地図の差分");
        builder.AppendLine($"- before: {beforeSystem?.Summary ?? "なし"}");
        builder.AppendLine($"- after: {afterSystem?.Summary ?? "なし"}");
        builder.AppendLine();
    }

    private static void AppendMetadataDiff(StringBuilder builder, EvidenceManifestDto before, EvidenceManifestDto after)
    {
        builder.AppendLine("## メタデータ差分");
        var differences = new List<string>();
        AppendValueDiff(differences, "directoryName", before.DirectoryName, after.DirectoryName);
        AppendValueDiff(differences, "workspaceRoot", before.WorkspaceRoot, after.WorkspaceRoot);
        AppendValueDiff(differences, "generatedAtUtc", before.GeneratedAtUtc, after.GeneratedAtUtc);
        AppendValueDiff(differences, "entry", before.Entry, after.Entry);
        AppendValueDiff(differences, "goal", before.Goal, after.Goal);
        AppendValueDiff(differences, "budget.maxFiles", before.Budget.MaxFiles.ToString(), after.Budget.MaxFiles.ToString());
        AppendValueDiff(differences, "budget.maxTotalLines", before.Budget.MaxTotalLines.ToString(), after.Budget.MaxTotalLines.ToString());
        AppendValueDiff(differences, "budget.maxSnippetsPerFile", before.Budget.MaxSnippetsPerFile.ToString(), after.Budget.MaxSnippetsPerFile.ToString());
        AppendValueDiff(differences, "resolvedEntry.kind", before.ResolvedEntry.ResolvedKind, after.ResolvedEntry.ResolvedKind);
        AppendValueDiff(differences, "resolvedEntry.path", before.ResolvedEntry.ResolvedPath, after.ResolvedEntry.ResolvedPath);
        AppendValueDiff(differences, "resolvedEntry.symbol", before.ResolvedEntry.Symbol, after.ResolvedEntry.Symbol);
        AppendValueDiff(differences, "resolvedEntry.confidence", before.ResolvedEntry.Confidence, after.ResolvedEntry.Confidence);
        AppendDifferenceList(builder, differences);
    }

    private static void AppendContractDiff(
        StringBuilder builder,
        IReadOnlyList<EvidenceContractDto> before,
        IReadOnlyList<EvidenceContractDto> after)
    {
        builder.AppendLine();
        builder.AppendLine("## 契約差分");
        var beforeMap = BuildIndexedMap(before, static item => $"{item.Kind}|{item.Title}");
        var afterMap = BuildIndexedMap(after, static item => $"{item.Kind}|{item.Title}");
        AppendAddedRemovedChanged(
            builder,
            beforeMap,
            afterMap,
            static item => $"[{item.Kind}] {item.Title}: {item.Summary}",
            static (oldItem, newItem) =>
                string.Equals(oldItem.Summary, newItem.Summary, StringComparison.Ordinal)
                    ? null
                    : $"[{oldItem.Kind}] {oldItem.Title}{Environment.NewLine}before: {oldItem.Summary}{Environment.NewLine}after: {newItem.Summary}");
    }

    private static void AppendSelectedFileDiff(
        StringBuilder builder,
        IReadOnlyList<EvidenceSelectedFileDto> before,
        IReadOnlyList<EvidenceSelectedFileDto> after)
    {
        builder.AppendLine();
        builder.AppendLine("## 選択ファイル差分");
        var beforeMap = before.ToDictionary(static item => item.Path, static item => item, StringComparer.Ordinal);
        var afterMap = after.ToDictionary(static item => item.Path, static item => item, StringComparer.Ordinal);
        AppendAddedRemovedChanged(
            builder,
            beforeMap,
            afterMap,
            static item => $"{item.Path} (reason: {item.Reason}, required: {ToFlag(item.IsRequired)}, lines: {item.LineCount})",
            static (oldItem, newItem) =>
                oldItem == newItem
                    ? null
                    : $"{oldItem.Path}{Environment.NewLine}before: reason={oldItem.Reason}, required={ToFlag(oldItem.IsRequired)}, lines={oldItem.LineCount}{Environment.NewLine}after: reason={newItem.Reason}, required={ToFlag(newItem.IsRequired)}, lines={newItem.LineCount}");
    }

    private static void AppendSelectedSnippetDiff(
        StringBuilder builder,
        IReadOnlyList<EvidenceSelectedSnippetDto> before,
        IReadOnlyList<EvidenceSelectedSnippetDto> after)
    {
        builder.AppendLine();
        builder.AppendLine("## 選択スニペット差分");
        var beforeMap = BuildIndexedMap(before, static item => $"{item.Path}|{item.Anchor}");
        var afterMap = BuildIndexedMap(after, static item => $"{item.Path}|{item.Anchor}");
        AppendAddedRemovedChanged(
            builder,
            beforeMap,
            afterMap,
            static item => $"{item.Path}:{item.StartLine}-{item.EndLine} (reason: {item.Reason}, anchor: {item.Anchor}, required: {ToFlag(item.IsRequired)})",
            static (oldItem, newItem) =>
                oldItem == newItem
                    ? null
                    : $"{oldItem.Path}::{oldItem.Anchor}{Environment.NewLine}before: {oldItem.StartLine}-{oldItem.EndLine}, reason={oldItem.Reason}, required={ToFlag(oldItem.IsRequired)}{Environment.NewLine}after: {newItem.StartLine}-{newItem.EndLine}, reason={newItem.Reason}, required={ToFlag(newItem.IsRequired)}");
    }

    private static void AppendDecisionTraceDiff(
        StringBuilder builder,
        IReadOnlyList<EvidenceDecisionTraceDto> before,
        IReadOnlyList<EvidenceDecisionTraceDto> after)
    {
        builder.AppendLine();
        builder.AppendLine("## 選定理由差分");
        var beforeMap = BuildIndexedMap(before, static item => $"{item.Category}|{item.ItemKey}");
        var afterMap = BuildIndexedMap(after, static item => $"{item.Category}|{item.ItemKey}");
        AppendAddedRemovedChanged(
            builder,
            beforeMap,
            afterMap,
            static item => $"{FormatTrace(item)}{Environment.NewLine}summary: {item.Summary}",
            static (oldItem, newItem) =>
                oldItem == newItem
                    ? null
                    : $"[{oldItem.Category}] {oldItem.ItemKey}{Environment.NewLine}before: {FormatTraceDetails(oldItem)}{Environment.NewLine}after: {FormatTraceDetails(newItem)}{Environment.NewLine}before summary: {oldItem.Summary}{Environment.NewLine}after summary: {newItem.Summary}");
    }

    private static void AppendAddedRemovedChanged<T>(
        StringBuilder builder,
        IReadOnlyDictionary<string, T> before,
        IReadOnlyDictionary<string, T> after,
        Func<T, string> addedOrRemovedFormatter,
        Func<T, T, string?> changedFormatter)
    {
        var added = after.Keys.Except(before.Keys, StringComparer.Ordinal).OrderBy(static key => key, StringComparer.Ordinal).ToArray();
        var removed = before.Keys.Except(after.Keys, StringComparer.Ordinal).OrderBy(static key => key, StringComparer.Ordinal).ToArray();
        var changed = before.Keys.Intersect(after.Keys, StringComparer.Ordinal)
            .OrderBy(static key => key, StringComparer.Ordinal)
            .Select(key => changedFormatter(before[key], after[key]))
            .Where(static text => !string.IsNullOrWhiteSpace(text))
            .ToArray();

        builder.AppendLine("### 追加");
        AppendDifferenceList(builder, added.Select(key => addedOrRemovedFormatter(after[key])));
        builder.AppendLine("### 削除");
        AppendDifferenceList(builder, removed.Select(key => addedOrRemovedFormatter(before[key])));
        builder.AppendLine("### 変更");
        AppendDifferenceList(builder, changed!);
    }

    private static void AppendDifferenceList(StringBuilder builder, IEnumerable<string> differences)
    {
        var items = differences.Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray();
        if (items.Length == 0)
        {
            builder.AppendLine("- なし");
            return;
        }

        foreach (var item in items)
        {
            builder.AppendLine($"- {item}");
        }
    }

    private static void AppendValueDiff(ICollection<string> differences, string label, string? before, string? after)
    {
        if (!string.Equals(before, after, StringComparison.Ordinal))
        {
            differences.Add($"{label}: `{before ?? "(null)"}` -> `{after ?? "(null)"}`");
        }
    }

    private static IReadOnlyDictionary<string, T> BuildIndexedMap<T>(
        IEnumerable<T> items,
        Func<T, string> keySelector)
    {
        var indexByKey = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new Dictionary<string, T>(StringComparer.Ordinal);

        foreach (var item in items)
        {
            var logicalKey = keySelector(item);
            indexByKey.TryGetValue(logicalKey, out var currentIndex);
            currentIndex++;
            indexByKey[logicalKey] = currentIndex;
            result[$"{logicalKey}#{currentIndex}"] = item;
        }

        return result;
    }

    private static string FormatTrace(EvidenceDecisionTraceDto item) =>
        $"[{item.Category}] {item.ItemKey} ({item.DecisionKind}, score: {item.Score}, rank: {item.Rank}, matched: {FormatTerms(item.MatchedTerms)})";

    private static string FormatTraceDetails(EvidenceDecisionTraceDto item) =>
        $"kind={item.DecisionKind}, score={item.Score}, rank={item.Rank}, matched={FormatTerms(item.MatchedTerms)}, sources={FormatSources(item.MatchedSources)}";

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static string ToFlag(bool value) => value ? "required" : "optional";

    private static string FormatTerms(IReadOnlyList<string> terms) =>
        terms.Count == 0 ? "(none)" : string.Join(", ", terms);

    private static string FormatSources(IReadOnlyList<EvidenceDecisionMatchedSourceDto> sources) =>
        sources.Count == 0
            ? "(none)"
            : string.Join(" / ", sources.Select(source => $"{source.Source}={string.Join(",", source.Terms)}"));
}
