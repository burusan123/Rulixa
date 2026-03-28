namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class SectionCompressionSupport
{
    internal static SectionCompressionResult<T> Compress<T>(
        IReadOnlyList<SectionCompressionCandidate<T>> candidates,
        int maxSelected)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var selected = new List<T>();
        var decisionKinds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var seenCanonicalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            var decisionKind = Decide(candidate, seenCanonicalKeys, selected.Count, maxSelected);
            decisionKinds[candidate.ItemKey] = decisionKind;

            if (!string.Equals(decisionKind, "selected", StringComparison.Ordinal))
            {
                continue;
            }

            seenCanonicalKeys.Add(candidate.CanonicalKey);
            selected.Add(candidate.Item);
        }

        return new SectionCompressionResult<T>(selected, decisionKinds);
    }

    private static string Decide<T>(
        SectionCompressionCandidate<T> candidate,
        IReadOnlySet<string> seenCanonicalKeys,
        int selectedCount,
        int maxSelected)
    {
        if (!candidate.Evaluation.IsSelectable)
        {
            return candidate.Evaluation.Evidence.HasAmbiguousCandidates
                ? "omitted-ambiguous"
                : "omitted-low-score";
        }

        if (candidate.IsWeakRoute)
        {
            return "omitted-low-score";
        }

        if (candidate.IsUiBoundary)
        {
            return "omitted-ui-boundary";
        }

        if (candidate.IsSelfLoop)
        {
            return "omitted-self-loop";
        }

        if (seenCanonicalKeys.Contains(candidate.CanonicalKey))
        {
            return "omitted-duplicate-route";
        }

        return selectedCount >= maxSelected ? "omitted-section-cap" : "selected";
    }
}

internal sealed record SectionCompressionCandidate<T>(
    T Item,
    string ItemKey,
    string CanonicalKey,
    SectionSelectionEvaluation Evaluation,
    bool IsUiBoundary = false,
    bool IsSelfLoop = false,
    bool IsWeakRoute = false);

internal sealed record SectionCompressionResult<T>(
    IReadOnlyList<T> Selected,
    IReadOnlyDictionary<string, string> DecisionKinds);
