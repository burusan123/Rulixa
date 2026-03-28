using System.Text.RegularExpressions;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class GoalDrivenExpansionPlanner
{
    private static readonly IReadOnlyDictionary<string, string[]> GoalAliases = new Dictionary<string, string[]>
    {
        ["system"] =
        [
            "system", "understand", "explain", "overview", "structure",
            "全体", "理解", "説明", "概要", "構成"
        ],
        ["setting"] = ["setting", "settings", "設定"],
        ["license"] = ["license", "ライセンス"],
        ["import"] = ["import", "インポート"],
        ["export"] = ["export", "エクスポート"],
        ["share"] = ["share", "共有"],
        ["result"] = ["result", "results", "output", "結果", "出力"],
        ["open"] = ["open", "起動", "開く"],
        ["save"] = ["save", "保存"],
        ["reset"] = ["reset", "初期化", "リセット"],
        ["new"] = ["new", "新規"],
        ["project"] =
        [
            "project", "projects", "workspace", "open", "save", "new", "import", "export",
            "プロジェクト", "ワークスペース", "開く", "保存", "新規", "インポート", "エクスポート"
        ],
        ["drafting"] =
        [
            "drafting", "ocr", "ai", "analyze", "analysis", "diagram", "wall",
            "作図", "図面", "解析", "壁"
        ],
        ["architecture"] =
        [
            "architecture", "layer", "dependency", "dependencies", "golden", "regression",
            "アーキテクチャ", "層", "依存", "依存関係"
        ]
    };

    internal static GoalExpansionProfile Analyze(string goal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(goal);

        var terms = ExtractGoalTerms(goal)
            .OrderBy(static term => term, StringComparer.Ordinal)
            .ToArray();
        var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var term in terms)
        {
            if (GoalAliases.ContainsKey(term))
            {
                categories.Add(term);
            }
        }

        if (categories.Count == 0)
        {
            categories.Add("system");
        }

        return new GoalExpansionProfile(
            goal,
            categories.OrderBy(static category => category, StringComparer.Ordinal).ToArray(),
            terms);
    }

    internal static HashSet<string> ExtractGoalTerms(string goal)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (canonical, aliases) in GoalAliases)
        {
            if (aliases.Any(alias => goal.Contains(alias, StringComparison.OrdinalIgnoreCase)))
            {
                terms.Add(canonical);
            }
        }

        foreach (Match match in Regex.Matches(goal, "[A-Za-z0-9]+"))
        {
            terms.Add(Canonicalize(match.Value));
        }

        return terms;
    }

    internal static IEnumerable<string> ExtractIdentifierTerms(string raw)
    {
        foreach (var segment in raw.Split(['.', '_'], StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (Match match in Regex.Matches(segment, "[A-Z]?[a-z]+|[A-Z]+(?![a-z])|[0-9]+"))
            {
                yield return Canonicalize(match.Value);
            }
        }
    }

    private static string Canonicalize(string value)
    {
        foreach (var (canonical, aliases) in GoalAliases)
        {
            if (aliases.Any(alias => string.Equals(alias, value, StringComparison.OrdinalIgnoreCase)))
            {
                return canonical;
            }
        }

        return value.ToLowerInvariant();
    }
}

internal sealed record GoalExpansionProfile(
    string Goal,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Terms)
{
    internal bool HasCategory(string category) =>
        Categories.Contains(category, StringComparer.OrdinalIgnoreCase);
}
