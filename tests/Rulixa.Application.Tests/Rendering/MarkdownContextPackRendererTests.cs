using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Infrastructure.Rendering;

namespace Rulixa.Application.Tests.Rendering;

public sealed class MarkdownContextPackRendererTests
{
    [Fact]
    public void Render_UsesJapaneseLabelsAndRendersUnknownGuidance()
    {
        var renderer = new MarkdownContextPackRenderer();
        var contextPack = new ContextPack(
            Goal: "Shell 画面に新しいページを追加したい",
            Entry: new Entry(EntryKind.Symbol, "App.ViewModels.ShellViewModel"),
            ResolvedEntry: new ResolvedEntry(
                "symbol:App.ViewModels.ShellViewModel",
                ResolvedEntryKind.Symbol,
                @".\src\App\Views\ShellView.xaml",
                "App.ViewModels.ShellViewModel",
                ConfidenceLevel.High,
                []),
            Contracts:
            [
                new Contract(
                    ContractKind.ViewModelBinding,
                    "ルート DataContext",
                    "MainWindow.xaml.cs が MainWindow.xaml の DataContext に ShellViewModel を設定します。",
                    ["MainWindow.xaml", "MainWindow.xaml.cs"],
                    ["MainWindow", "App.ViewModels.ShellViewModel"]),
                new Contract(
                    ContractKind.Command,
                    "Workflow",
                    "この画面は 2 本の代表チェーンで処理を流し、persistence / hub-object へ接続します。",
                    ["ServiceRegistration.cs"],
                    ["App.ViewModels.ShellViewModel"])
            ],
            Indexes:
            [
                new IndexSection("View-ViewModel", ["MainWindow.xaml <-> App.ViewModels.ShellViewModel (ルート DataContext: MainWindow.xaml.cs)"]),
                new IndexSection("Workflow", ["高信頼チェーン: ShellViewModel -> ProjectWorkflowService -> ProjectDocument"])
            ],
            SelectedSnippets:
            [
                new SelectedSnippet(
                    @".\src\App\Views\MainWindow.xaml.cs",
                    "root-binding-source",
                    -10,
                    true,
                    "ルート DataContext",
                    6,
                    9,
                    "public MainWindow(ShellViewModel shellViewModel)\n{\n    DataContext = shellViewModel;\n}"),
                new SelectedSnippet(
                    @".\src\App\ServiceRegistration.cs",
                    "dependency-injection",
                    5,
                    true,
                    "ShellViewModel (Singleton)",
                    8,
                    10,
                    "services.AddSingleton<ShellViewModel>();")
            ],
            SelectedFiles:
            [
                new SelectedFile(@".\src\App\Views\ShellView.xaml", "entry", 0, true, 42)
            ],
            DecisionTraces: [],
            Unknowns:
            [
                new Diagnostic(
                    "workflow.missing-downstream",
                    "追跡できた範囲: IDraftingWorkflowService。停止点: algorithm / analyzer に到達する経路を 2 hop 以内で確定できませんでした。",
                    null,
                    DiagnosticSeverity.Info,
                    ["App.Services.DraftingAiDiagramAnalysisService", "App.Algorithms.WallAlgorithmRunner"])
            ]);

        var markdown = renderer.Render(contextPack);

        Assert.Contains("# コンテキストパック", markdown);
        Assert.Contains("## 目的", markdown);
        Assert.Contains("## 未解決事項", markdown);
        Assert.Contains("既知の範囲:", markdown);
        Assert.Contains("次に見る候補:", markdown);
        Assert.Contains("Workflow の探索ガイド", markdown);
        Assert.Contains("App.Services.DraftingAiDiagramAnalysisService", markdown);
        Assert.DoesNotContain(@".\", markdown);
    }

    [Fact]
    public void Render_WhenUnknownsAndSnippetsAreEmpty_DisplaysNone()
    {
        var renderer = new MarkdownContextPackRenderer();
        var contextPack = new ContextPack(
            Goal: "goal",
            Entry: new Entry(EntryKind.File, "Views/ShellView.xaml"),
            ResolvedEntry: new ResolvedEntry("file:Views/ShellView.xaml", ResolvedEntryKind.File, "Views/ShellView.xaml", null, ConfidenceLevel.High, []),
            Contracts: [],
            Indexes: [],
            SelectedSnippets: [],
            SelectedFiles: [],
            DecisionTraces: [],
            Unknowns: []);

        var markdown = renderer.Render(contextPack);

        Assert.Contains("## 抜粋スニペット", markdown);
        Assert.Contains("## 未解決事項", markdown);
        Assert.Contains("- なし", markdown);
    }
}
