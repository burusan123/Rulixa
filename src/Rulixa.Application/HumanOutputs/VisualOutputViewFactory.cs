using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Packs;

namespace Rulixa.Application.HumanOutputs;

internal static class VisualOutputViewFactory
{
    public static IReadOnlyList<VisualView> Create(ContextPack contextPack, HumanOutputFactSet facts) =>
    [
        BuildOverviewView(facts),
        BuildWorkflowView(contextPack, facts),
        BuildEvidenceView(contextPack, facts),
        BuildUnknownsView(contextPack, facts),
        BuildArchitectureView(facts)
    ];

    private static VisualView BuildOverviewView(HumanOutputFactSet facts) =>
        new(
            Id: "overview",
            Label: "Overview",
            Summary: "root と中心状態、主要サブマップ、次の候補を確認します。",
            Sections:
            [
                new VisualSection(
                    Id: "overview-root",
                    Title: "Root Summary",
                    Kind: "facts",
                    Collapsible: false,
                    Items: BuildOverviewRootItems(facts),
                    Graph: null),
                new VisualSection(
                    Id: "overview-subsystems",
                    Title: "Subsystem Cards",
                    Kind: "cards",
                    Collapsible: true,
                    Items: BuildSubsystemCards(facts),
                    Graph: null),
                new VisualSection(
                    Id: "overview-next-candidates",
                    Title: "Next Candidates",
                    Kind: "list",
                    Collapsible: true,
                    Items: facts.NextCandidates
                        .Select((candidate, index) => VisualOutputFormatting.CreateItem(
                            $"next-{index}",
                            candidate,
                            "次に読む候補です。",
                            $"candidate {candidate}",
                            null,
                            ["next-candidate"]))
                        .ToArray(),
                    Graph: null)
            ]);

    private static VisualView BuildWorkflowView(ContextPack contextPack, HumanOutputFactSet facts)
    {
        var routes = BuildWorkflowRouteItems(facts);
        return new VisualView(
            Id: "workflow",
            Label: "Workflow",
            Summary: "代表チェーンと dependency seam を局所図で探索します。",
            Sections:
            [
                new VisualSection(
                    Id: "workflow-routes",
                    Title: "Representative Routes",
                    Kind: "graph-list",
                    Collapsible: false,
                    Items: routes.Items,
                    Graph: routes.Graph),
                new VisualSection(
                    Id: "workflow-seams",
                    Title: "Dependency Seams",
                    Kind: "list",
                    Collapsible: true,
                    Items: facts.DependencySeams
                        .Select((seam, index) => VisualOutputFormatting.CreateItem(
                            $"seam-{index}",
                            $"Seam {index + 1}",
                            seam,
                            seam,
                            BuildSnippetInspectorId(contextPack, seam),
                            ["dependency-seam"]))
                        .ToArray(),
                    Graph: null)
            ]);
    }

    private static VisualView BuildEvidenceView(ContextPack contextPack, HumanOutputFactSet facts) =>
        new(
            Id: "evidence",
            Label: "Evidence",
            Summary: "selected files と selected snippets の根拠へ戻ります。",
            Sections:
            [
                new VisualSection(
                    Id: "evidence-files",
                    Title: "Selected Files",
                    Kind: "table",
                    Collapsible: false,
                    Items: contextPack.SelectedFiles
                        .Select((file, index) => VisualOutputFormatting.CreateItem(
                            $"evidence-file-{index}",
                            VisualOutputFormatting.NormalizeDisplayPath(file.Path) ?? file.Path,
                            $"{file.Reason} / required={file.IsRequired} / lines={file.LineCount}",
                            $"{file.Path} {file.Reason}",
                            $"file-{index}",
                            [$"reason:{file.Reason}", file.IsRequired ? "required" : "optional"]))
                        .ToArray(),
                    Graph: null),
                new VisualSection(
                    Id: "evidence-snippets",
                    Title: "Selected Snippets",
                    Kind: "table",
                    Collapsible: true,
                    Items: contextPack.SelectedSnippets
                        .Select((snippet, index) => VisualOutputFormatting.CreateItem(
                            $"evidence-snippet-{index}",
                            $"{VisualOutputFormatting.NormalizeDisplayPath(snippet.Path)}:{snippet.StartLine}-{snippet.EndLine}",
                            snippet.Reason,
                            $"{snippet.Path} {snippet.Reason} {snippet.Content}",
                            $"snippet-{index}",
                            [$"reason:{snippet.Reason}"]))
                        .ToArray(),
                    Graph: null),
                new VisualSection(
                    Id: "evidence-bundle",
                    Title: "Evidence Bundle",
                    Kind: "facts",
                    Collapsible: true,
                    Items: BuildEvidenceBundleItems(facts),
                    Graph: null)
            ]);

    private static VisualView BuildUnknownsView(ContextPack contextPack, HumanOutputFactSet facts) =>
        new(
            Id: "unknowns",
            Label: "Unknowns",
            Summary: "未確定事項と next candidates を確認します。",
            Sections:
            [
                new VisualSection(
                    Id: "unknowns-list",
                    Title: "Unknown Guidance",
                    Kind: "table",
                    Collapsible: false,
                    Items: contextPack.Unknowns
                        .Select((unknown, index) => VisualOutputFormatting.CreateItem(
                            $"unknown-{index}",
                            unknown.Code,
                            BuildUnknownSummary(unknown),
                            $"{unknown.Code} {unknown.Message} {string.Join(' ', unknown.Candidates)}",
                            $"unknown-{index}",
                            BuildUnknownMeta(unknown)))
                        .ToArray(),
                    Graph: null),
                new VisualSection(
                    Id: "unknowns-risk",
                    Title: "Risk Summary",
                    Kind: "list",
                    Collapsible: true,
                    Items: facts.RiskLines
                        .Select((line, index) => VisualOutputFormatting.CreateItem(
                            $"risk-{index}",
                            $"Risk {index + 1}",
                            line,
                            line,
                            null,
                            ["risk"]))
                        .ToArray(),
                    Graph: null)
            ]);

    private static VisualView BuildArchitectureView(HumanOutputFactSet facts) =>
        new(
            Id: "architecture",
            Label: "Architecture",
            Summary: "constraints と known unknowns を分けて読みます。",
            Sections:
            [
                new VisualSection(
                    Id: "architecture-constraints",
                    Title: "Constraints",
                    Kind: "list",
                    Collapsible: false,
                    Items: facts.ArchitecturalConstraints
                        .Select((line, index) => VisualOutputFormatting.CreateItem(
                            $"constraint-{index}",
                            $"Constraint {index + 1}",
                            line,
                            line,
                            null,
                            ["constraint"]))
                        .ToArray(),
                    Graph: null),
                new VisualSection(
                    Id: "architecture-known-unknowns",
                    Title: "Known Unknowns",
                    Kind: "list",
                    Collapsible: true,
                    Items: facts.KnownUnknowns
                        .Select((line, index) => VisualOutputFormatting.CreateItem(
                            $"known-unknown-{index}",
                            $"Known Unknown {index + 1}",
                            line,
                            line,
                            null,
                            ["known-unknown"]))
                        .ToArray(),
                    Graph: null),
                new VisualSection(
                    Id: "architecture-design-points",
                    Title: "Design Knowledge Points",
                    Kind: "list",
                    Collapsible: true,
                    Items: facts.DependencySeams
                        .Concat(facts.CenterStates.Select(static item => $"center-state: {item}"))
                        .Take(8)
                        .Select((line, index) => VisualOutputFormatting.CreateItem(
                            $"design-point-{index}",
                            $"Design Point {index + 1}",
                            line,
                            line,
                            null,
                            ["design-knowledge"]))
                        .ToArray(),
                    Graph: null)
            ]);

    private static IReadOnlyList<VisualItem> BuildOverviewRootItems(HumanOutputFactSet facts)
    {
        var items = new List<VisualItem>
        {
            VisualOutputFormatting.CreateItem(
                "root-entry",
                facts.Entry,
                facts.SystemSummary ?? "system summary は未確定です。",
                $"{facts.Entry} {facts.SystemSummary}",
                "root-entry",
                ["root"]),
            VisualOutputFormatting.CreateItem(
                "resolved-target",
                facts.ResolvedSymbol ?? facts.ResolvedPath ?? "resolved-target",
                $"kind: {facts.ResolvedKind}",
                $"{facts.ResolvedPath} {facts.ResolvedSymbol}",
                "root-entry",
                ["resolved"]),
            VisualOutputFormatting.CreateItem(
                "primary-unknown",
                "Primary Unknown",
                facts.KnownUnknowns.FirstOrDefault() ?? "未確定事項は見つかっていません。",
                string.Join(' ', facts.KnownUnknowns),
                null,
                ["unknown"])
        };

        items.AddRange(facts.CenterStates.Select((state, index) => VisualOutputFormatting.CreateItem(
            $"center-state-{index}",
            state,
            "中心状態です。",
            state,
            null,
            ["center-state"])));
        return items;
    }

    private static IReadOnlyList<VisualItem> BuildSubsystemCards(HumanOutputFactSet facts)
    {
        var cards = new List<VisualItem>();
        VisualOutputFormatting.AddCard(cards, "workflow-card", "Workflow", facts.WorkflowLines);
        VisualOutputFormatting.AddCard(cards, "persistence-card", "Persistence", facts.PersistenceLines);
        VisualOutputFormatting.AddCard(cards, "assets-card", "External Assets", facts.ExternalAssetLines);
        VisualOutputFormatting.AddCard(cards, "architecture-card", "Architecture", facts.ArchitecturalConstraints);
        VisualOutputFormatting.AddCard(cards, "unknown-card", "Known Unknowns", facts.KnownUnknowns);
        return cards;
    }

    private static (IReadOnlyList<VisualItem> Items, VisualGraph? Graph) BuildWorkflowRouteItems(HumanOutputFactSet facts)
    {
        var routeLines = facts.WorkflowLines.Where(static line => line.Contains("->", StringComparison.Ordinal)).ToArray();
        var items = routeLines.Select((line, index) => VisualOutputFormatting.CreateItem(
                $"route-{index}",
                $"Route {index + 1}",
                line,
                line,
                null,
                ["workflow-route"]))
            .ToArray();

        if (routeLines.Length == 0)
        {
            return (items, null);
        }

        var nodeMap = new Dictionary<string, VisualGraphNode>(StringComparer.Ordinal);
        var edges = new List<VisualGraphEdge>();
        foreach (var route in routeLines)
        {
            AddRouteNodes(nodeMap, edges, route);
        }

        return (items, new VisualGraph("Representative Routes", nodeMap.Values.ToArray(), edges));
    }

    private static void AddRouteNodes(
        IDictionary<string, VisualGraphNode> nodeMap,
        ICollection<VisualGraphEdge> edges,
        string route)
    {
        var nodes = route
            .Split("->", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(static node => node.Trim())
            .ToArray();

        for (var index = 0; index < nodes.Length; index++)
        {
            var nodeId = $"node-{VisualOutputFormatting.NormalizeId(nodes[index])}";
            nodeMap[nodeId] = new VisualGraphNode(nodeId, nodes[index], index == 0);
            if (index < nodes.Length - 1)
            {
                edges.Add(new VisualGraphEdge(nodeId, $"node-{VisualOutputFormatting.NormalizeId(nodes[index + 1])}", "flows-to"));
            }
        }
    }

    private static IReadOnlyList<VisualItem> BuildEvidenceBundleItems(HumanOutputFactSet facts)
    {
        var items = new List<VisualItem>();
        if (!string.IsNullOrWhiteSpace(facts.EvidenceDirectory))
        {
            items.Add(VisualOutputFormatting.CreateItem(
                "evidence-directory",
                "Evidence Directory",
                facts.EvidenceDirectory,
                facts.EvidenceDirectory,
                null,
                ["evidence-dir"]));
        }

        items.AddRange(facts.EvidenceSources.Select((source, index) => VisualOutputFormatting.CreateItem(
            $"evidence-source-{index}",
            $"Source {index + 1}",
            source,
            source,
            null,
            ["evidence-source"])));
        return items;
    }

    private static IReadOnlyList<string> BuildUnknownMeta(Diagnostic unknown)
    {
        var meta = new List<string> { $"severity:{unknown.Severity}" };
        if (unknown.Candidates.Count > 0)
        {
            meta.Add($"first:{unknown.Candidates[0]}");
        }

        return meta;
    }

    private static string BuildUnknownSummary(Diagnostic unknown)
    {
        var firstCandidate = unknown.Candidates.FirstOrDefault();
        return firstCandidate is null
            ? VisualOutputFormatting.SanitizeUnknownMessage(unknown.Message)
            : $"{VisualOutputFormatting.SanitizeUnknownMessage(unknown.Message)} / first candidate: {firstCandidate}";
    }

    private static string? BuildSnippetInspectorId(ContextPack contextPack, string seam)
    {
        for (var index = 0; index < contextPack.SelectedSnippets.Count; index++)
        {
            var displayPath = VisualOutputFormatting.NormalizeDisplayPath(contextPack.SelectedSnippets[index].Path) ?? string.Empty;
            if (seam.Contains(displayPath, StringComparison.Ordinal))
            {
                return $"snippet-{index}";
            }
        }

        return null;
    }
}
