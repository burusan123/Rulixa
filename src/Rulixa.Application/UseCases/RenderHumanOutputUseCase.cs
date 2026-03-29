using Rulixa.Application.HumanOutputs;
using Rulixa.Application.Ports;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.UseCases;

public sealed class RenderHumanOutputUseCase
{
    private readonly IHumanOutputRenderer renderer;
    private readonly HumanOutputFactAnalyzer factAnalyzer = new();

    public RenderHumanOutputUseCase(IHumanOutputRenderer renderer)
    {
        this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public string Execute(
        ContextPack contextPack,
        WorkspaceScanResult scanResult,
        HumanOutputMode mode,
        HumanOutputRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(contextPack);
        ArgumentNullException.ThrowIfNull(scanResult);
        ArgumentNullException.ThrowIfNull(options);

        var facts = factAnalyzer.Analyze(contextPack, scanResult, options);
        var document = BuildDocument(mode, facts);
        return renderer.Render(document);
    }

    private static HumanOutputDocument BuildDocument(HumanOutputMode mode, HumanOutputFactSet facts) => mode switch
    {
        HumanOutputMode.Review => new HumanOutputDocument(
            Title: "Review Brief",
            Mode: mode,
            Sections:
            [
                BuildOverviewSection(facts),
                BuildCenterStateSection(facts),
                BuildWorkflowSection(facts),
                BuildPersistenceAndAssetsSection(facts),
                BuildUnknownAndRiskSection(facts),
                BuildNextReadingSection(facts)
            ]),
        HumanOutputMode.Audit => new HumanOutputDocument(
            Title: "Audit Snapshot",
            Mode: mode,
            Sections:
            [
                BuildRootEntrySection(facts),
                BuildObservedFactsSection(facts),
                BuildEvidenceSection(facts),
                BuildDiagnosticsSection(facts),
                BuildCompareEvidenceSection(facts),
                BuildOpenQuestionsSection(facts)
            ]),
        HumanOutputMode.Knowledge => new HumanOutputDocument(
            Title: "Design Knowledge Snapshot",
            Mode: mode,
            Sections:
            [
                BuildSubsystemMapSection(facts),
                BuildCenterStateSection(facts),
                BuildDependencySeamsSection(facts),
                BuildArchitecturalConstraintsSection(facts),
                BuildKnownUnknownsSection(facts),
                BuildFutureChangeSection(facts)
            ]),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

    private static HumanOutputSection BuildOverviewSection(HumanOutputFactSet facts)
    {
        var bullets = new List<string>
        {
            !string.IsNullOrWhiteSpace(facts.SystemSummary)
                ? $"断定: {facts.SystemSummary}"
                : $"推定: `{facts.Entry}` を起点に `{facts.Goal}` を理解するための review です。"
        };

        bullets.Add($"断定: 解決種別は `{facts.ResolvedKind}` です。");
        if (!string.IsNullOrWhiteSpace(facts.ResolvedPath))
        {
            bullets.Add($"断定: 解決 path は `{facts.ResolvedPath}` です。");
        }

        if (!string.IsNullOrWhiteSpace(facts.ResolvedSymbol))
        {
            bullets.Add($"断定: 解決 symbol は `{facts.ResolvedSymbol}` です。");
        }

        return new HumanOutputSection("概要", [], bullets);
    }

    private static HumanOutputSection BuildCenterStateSection(HumanOutputFactSet facts) =>
        new(
            "中心状態",
            [],
            facts.CenterStates.Count > 0
                ? facts.CenterStates.Select(static item => $"断定: `{item}`").ToArray()
                : ["unknown: 中心状態は pack からは確定できません。"]);

    private static HumanOutputSection BuildWorkflowSection(HumanOutputFactSet facts) =>
        new(
            "主要 workflow",
            [],
            facts.WorkflowLines.Count > 0
                ? facts.WorkflowLines.Select(static line => $"断定: {line}").ToArray()
                : ["unknown: workflow を示す signal は不足しています。"]);

    private static HumanOutputSection BuildPersistenceAndAssetsSection(HumanOutputFactSet facts)
    {
        var bullets = new List<string>();
        bullets.AddRange(
            facts.PersistenceLines.Count > 0
                ? facts.PersistenceLines.Select(static line => $"断定: {line}")
                : ["unknown: persistence は pack からは確定できません。"]);
        bullets.AddRange(
            facts.ExternalAssetLines.Count > 0
                ? facts.ExternalAssetLines.Select(static line => $"断定: {line}")
                : ["推定: external assets を示す強い signal は見えていません。"]);

        return new HumanOutputSection("Persistence / External Assets", [], bullets);
    }

    private static HumanOutputSection BuildUnknownAndRiskSection(HumanOutputFactSet facts)
    {
        var bullets = new List<string>();
        bullets.AddRange(facts.KnownUnknowns);
        bullets.AddRange(facts.RiskLines);
        return new HumanOutputSection("Unknown / Risk", [], bullets);
    }

    private static HumanOutputSection BuildNextReadingSection(HumanOutputFactSet facts) =>
        new(
            "次に読む file / symbol",
            [],
            facts.NextCandidates.Count > 0
                ? facts.NextCandidates.Select(static candidate => $"推定: `{candidate}`").ToArray()
                : ["unknown: 次に読む候補はまだ整理できていません。"]);

    private static HumanOutputSection BuildRootEntrySection(HumanOutputFactSet facts)
    {
        var bullets = new List<string>
        {
            $"断定: root entry は `{facts.Entry}` です。",
            $"断定: 解決種別は `{facts.ResolvedKind}` です。"
        };

        if (!string.IsNullOrWhiteSpace(facts.ResolvedPath))
        {
            bullets.Add($"断定: 解決 path は `{facts.ResolvedPath}` です。");
        }

        if (!string.IsNullOrWhiteSpace(facts.ResolvedSymbol))
        {
            bullets.Add($"断定: 解決 symbol は `{facts.ResolvedSymbol}` です。");
        }

        return new HumanOutputSection("Root Entry", [], bullets);
    }

    private static HumanOutputSection BuildObservedFactsSection(HumanOutputFactSet facts) =>
        new("Observed Facts", [], facts.ObservedFacts);

    private static HumanOutputSection BuildEvidenceSection(HumanOutputFactSet facts)
    {
        var bullets = new List<string>();
        if (!string.IsNullOrWhiteSpace(facts.EvidenceDirectory))
        {
            bullets.Add($"断定: evidence bundle は `{facts.EvidenceDirectory}` に出力されています。");
        }

        bullets.AddRange(
            facts.EvidenceSources.Count > 0
                ? facts.EvidenceSources.Select(static item => $"断定: {item}")
                : ["unknown: evidence source を列挙できません。"]);

        return new HumanOutputSection("Evidence Source", [], bullets);
    }

    private static HumanOutputSection BuildDiagnosticsSection(HumanOutputFactSet facts)
    {
        var bullets = new List<string>
        {
            facts.DegradedDiagnosticCount > 0
                ? $"推定: degraded diagnostics は `{facts.DegradedDiagnosticCount}` 件です。"
                : "断定: degraded diagnostics は 0 件です。"
        };
        bullets.AddRange(facts.KnownUnknowns);
        return new HumanOutputSection("Degraded Diagnostics", [], bullets);
    }

    private static HumanOutputSection BuildCompareEvidenceSection(HumanOutputFactSet facts)
    {
        var bullets = new List<string>();
        if (!string.IsNullOrWhiteSpace(facts.CompareEvidenceReference))
        {
            bullets.Add($"断定: compare-evidence 参照先は `{facts.CompareEvidenceReference}` です。");
        }
        else if (!string.IsNullOrWhiteSpace(facts.EvidenceDirectory))
        {
            bullets.Add($"推定: compare-evidence は `{facts.EvidenceDirectory}` を基点に利用できます。");
        }
        else
        {
            bullets.Add("unknown: compare-evidence の参照先は指定されていません。");
        }

        return new HumanOutputSection("Compare-Evidence", [], bullets);
    }

    private static HumanOutputSection BuildOpenQuestionsSection(HumanOutputFactSet facts) =>
        new("未確定事項", [], facts.KnownUnknowns.Concat(facts.RiskLines).ToArray());

    private static HumanOutputSection BuildSubsystemMapSection(HumanOutputFactSet facts)
    {
        var bullets = new List<string>();
        if (!string.IsNullOrWhiteSpace(facts.SystemSummary))
        {
            bullets.Add($"断定: {facts.SystemSummary}");
        }

        bullets.AddRange(
            facts.WorkflowLines.Count > 0
                ? facts.WorkflowLines.Select(static line => $"推定: {line}")
                : ["unknown: subsystem map を補強する workflow signal が不足しています。"]);

        return new HumanOutputSection("Subsystem Map", [], bullets);
    }

    private static HumanOutputSection BuildDependencySeamsSection(HumanOutputFactSet facts) =>
        new(
            "Dependency Seams",
            [],
            facts.DependencySeams.Count > 0
                ? facts.DependencySeams
                : ["unknown: dependency seam は pack からは確定できません。"]);

    private static HumanOutputSection BuildArchitecturalConstraintsSection(HumanOutputFactSet facts) =>
        new("Architectural Constraints", [], facts.ArchitecturalConstraints);

    private static HumanOutputSection BuildKnownUnknownsSection(HumanOutputFactSet facts) =>
        new("Known Unknowns", [], facts.KnownUnknowns);

    private static HumanOutputSection BuildFutureChangeSection(HumanOutputFactSet facts)
    {
        var bullets = new List<string>();
        bullets.AddRange(
            facts.NextCandidates.Count > 0
                ? facts.NextCandidates.Select(static item => $"推定: `{item}`")
                : ["unknown: change focus に使える next candidate が不足しています。"]);
        bullets.AddRange(
            facts.EvidenceSources.Take(3).Select(static item => $"断定: {item}"));
        return new HumanOutputSection("将来変更時の注目点", [], bullets);
    }
}
