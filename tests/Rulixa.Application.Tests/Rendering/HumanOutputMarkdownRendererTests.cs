using Rulixa.Application.HumanOutputs;
using Rulixa.Infrastructure.Rendering;

namespace Rulixa.Application.Tests.Rendering;

public sealed class HumanOutputMarkdownRendererTests
{
    [Fact]
    public void Render_WritesMarkdownSectionsAndBullets()
    {
        var renderer = new HumanOutputMarkdownRenderer();
        var document = new HumanOutputDocument(
            Title: "Review Brief",
            Mode: HumanOutputMode.Review,
            Sections:
            [
                new HumanOutputSection(
                    "概要",
                    ["この文書は review 用の一次説明です。"],
                    ["中心状態は Shell と ProjectDocument です。", "Drafting へ主要 workflow が伸びます。"]),
                new HumanOutputSection(
                    "Unknown / Risk",
                    [],
                    ["unknown: downstream はまだ特定できていません。"])
            ]);

        var markdown = renderer.Render(document);

        Assert.Contains("# Review Brief", markdown, StringComparison.Ordinal);
        Assert.Contains("- mode: `review`", markdown, StringComparison.Ordinal);
        Assert.Contains("## 概要", markdown, StringComparison.Ordinal);
        Assert.Contains("この文書は review 用の一次説明です。", markdown, StringComparison.Ordinal);
        Assert.Contains("- 中心状態は Shell と ProjectDocument です。", markdown, StringComparison.Ordinal);
        Assert.Contains("## Unknown / Risk", markdown, StringComparison.Ordinal);
        Assert.Contains("- unknown: downstream はまだ特定できていません。", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WhenSectionHasNoBullets_WritesNoneMarker()
    {
        var renderer = new HumanOutputMarkdownRenderer();
        var document = new HumanOutputDocument(
            Title: "Audit Snapshot",
            Mode: HumanOutputMode.Audit,
            Sections:
            [
                new HumanOutputSection(
                    "Compare-Evidence",
                    ["evidence bundle を後から比較できます。"],
                    [])
            ]);

        var markdown = renderer.Render(document);

        Assert.Contains("# Audit Snapshot", markdown, StringComparison.Ordinal);
        Assert.Contains("## Compare-Evidence", markdown, StringComparison.Ordinal);
        Assert.Contains("evidence bundle を後から比較できます。", markdown, StringComparison.Ordinal);
        Assert.Contains("- なし", markdown, StringComparison.Ordinal);
    }
}
