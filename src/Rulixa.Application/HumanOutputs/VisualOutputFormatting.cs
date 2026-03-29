namespace Rulixa.Application.HumanOutputs;

internal static class VisualOutputFormatting
{
    public static VisualHeaderSummary BuildHeader(HumanOutputFactSet facts) =>
        new(
            RootEntry: facts.Entry,
            Goal: facts.Goal,
            ResolvedKind: facts.ResolvedKind,
            ResolvedPath: facts.ResolvedPath,
            ResolvedSymbol: facts.ResolvedSymbol,
            SystemSummary: facts.SystemSummary,
            CenterStates: facts.CenterStates,
            NextCandidates: facts.NextCandidates,
            PrimaryUnknown: facts.KnownUnknowns.FirstOrDefault() ?? "unknown: 未確定事項は見つかっていません。");

    public static VisualItem CreateItem(
        string id,
        string title,
        string summary,
        string searchText,
        string? inspectorId,
        IReadOnlyList<string> meta) =>
        new(
            Id: id,
            Title: title,
            Summary: summary,
            SearchText: searchText,
            InspectorId: inspectorId,
            Meta: meta);

    public static void AddCard(
        ICollection<VisualItem> cards,
        string id,
        string title,
        IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }

        cards.Add(CreateItem(
            id,
            title,
            string.Join(" / ", lines.Take(2)),
            $"{title} {string.Join(' ', lines)}",
            null,
            [$"count:{lines.Count}"]));
    }

    public static string SanitizeUnknownMessage(string message)
    {
        const string prefix = "既知の限界: ";
        return message.StartsWith(prefix, StringComparison.Ordinal)
            ? message[prefix.Length..]
            : message;
    }

    public static string NormalizeId(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
        }

        return builder.ToString().Trim('-');
    }

    public static string? NormalizeDisplayPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        var normalized = path.Replace('\\', '/');
        if (Path.IsPathRooted(normalized))
        {
            normalized = normalized[(Path.GetPathRoot(normalized)?.Length ?? 0)..];
        }

        return normalized.TrimStart('.', '/');
    }
}
