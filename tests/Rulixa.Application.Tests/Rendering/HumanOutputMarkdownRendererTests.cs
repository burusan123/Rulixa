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
                    ["この文書は review 用です。"],
                    ["断定: Shell を起点にします。", "推定: Drafting へ接続します。"]),
                new HumanOutputSection(
                    "Unknown / Risk",
                    [],
                    ["unknown: downstream は未確定です。"])
            ]);

        var markdown = renderer.Render(document);

        Assert.Contains("# Review Brief", markdown, StringComparison.Ordinal);
        Assert.Contains("- mode: `review`", markdown, StringComparison.Ordinal);
        Assert.Contains("## 概要", markdown, StringComparison.Ordinal);
        Assert.Contains("この文書は review 用です。", markdown, StringComparison.Ordinal);
        Assert.Contains("- 断定: Shell を起点にします。", markdown, StringComparison.Ordinal);
        Assert.Contains("## Unknown / Risk", markdown, StringComparison.Ordinal);
        Assert.Contains("- unknown: downstream は未確定です。", markdown, StringComparison.Ordinal);
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
                    ["evidence bundle を基点に参照します。"],
                    [])
            ]);

        var markdown = renderer.Render(document);

        Assert.Contains("# Audit Snapshot", markdown, StringComparison.Ordinal);
        Assert.Contains("## Compare-Evidence", markdown, StringComparison.Ordinal);
        Assert.Contains("evidence bundle を基点に参照します。", markdown, StringComparison.Ordinal);
        Assert.Contains("- なし", markdown, StringComparison.Ordinal);
    }
}
