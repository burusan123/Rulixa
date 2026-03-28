using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Infrastructure.Rendering;

namespace Rulixa.Application.Tests.Rendering;

public sealed class MarkdownContextPackRendererTests
{
    [Fact]
    public void Render_UsesJapaneseLabelsAndNormalizedPaths()
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
                    ContractKind.Startup,
                    "起動経路",
                    "App と MainWindow がルート ViewModel を構成します。",
                    ["App.xaml.cs"],
                    ["App.ViewModels.ShellViewModel"]),
                new Contract(
                    ContractKind.Navigation,
                    "一覧・選択・表示の対応",
                    "src/App/Views/ShellView.xaml では 一覧を Items にバインド、選択状態を SelectedItem にバインド、表示コンテンツを CurrentPage にバインド してページ切り替えを表現します。",
                    ["src/App/Views/ShellView.xaml"],
                    ["App.ViewModels.ShellViewModel", "Items", "SelectedItem", "CurrentPage"]),
                new Contract(
                    ContractKind.Navigation,
                    "ViewModel 更新点",
                    "App.ViewModels.ShellViewModel.Select(...) が CurrentPage = item.PageViewModel を実行します。",
                    ["src/App/ViewModels/ShellViewModel.cs"],
                    ["App.ViewModels.ShellViewModel", "Select", "CurrentPage = item.PageViewModel"])
            ],
            Indexes:
            [
                new IndexSection("起動経路", ["App.xaml.cs -> App.ViewModels.ShellViewModel"]),
                new IndexSection("ナビゲーション", ["src/App/Views/ShellView.xaml: Items=Items, SelectedItem=SelectedItem, Content=CurrentPage"]),
                new IndexSection("ナビゲーション更新点", ["App.ViewModels.ShellViewModel.Select(...) -> CurrentPage = item.PageViewModel (line: 42)"])
            ],
            SelectedFiles:
            [
                new SelectedFile(@".\src\App\Views\ShellView.xaml", "entry", 0, true, 42),
                new SelectedFile(@".\MainWindow.xaml.cs", "startup", 10, true, 10),
                new SelectedFile(@".\src\App\ViewModels\ShellViewModel.cs", "navigation-update", -1, true, 120)
            ],
            Unknowns:
            [
                new Diagnostic("entry.unresolved", "エントリを一意に解決できませんでした。", null, DiagnosticSeverity.Warning, [])
            ]);

        var markdown = renderer.Render(contextPack);

        Assert.Contains("# コンテキストパック", markdown);
        Assert.Contains("## 目的", markdown);
        Assert.Contains("## 選定ファイル", markdown);
        Assert.Contains("理由: 入口", markdown);
        Assert.Contains("理由: 起動経路", markdown);
        Assert.Contains("理由: ナビゲーション更新点", markdown);
        Assert.Contains("src/App/Views/ShellView.xaml", markdown);
        Assert.Contains("src/App/ViewModels/ShellViewModel.cs", markdown);
        Assert.DoesNotContain(@".\", markdown);
        Assert.Contains("[起動経路]", markdown);
        Assert.Contains("[ナビゲーション]", markdown);
        Assert.Contains("## 影響範囲 / インデックス", markdown);
        Assert.Contains("### ナビゲーション更新点", markdown);
        Assert.Contains("CurrentPage = item.PageViewModel (line: 42)", markdown);
        Assert.Contains("[警告] entry.unresolved", markdown);
    }

    [Fact]
    public void Render_WhenUnknownsAreEmpty_DisplaysNone()
    {
        var renderer = new MarkdownContextPackRenderer();
        var contextPack = new ContextPack(
            Goal: "goal",
            Entry: new Entry(EntryKind.File, "Views/ShellView.xaml"),
            ResolvedEntry: new ResolvedEntry("file:Views/ShellView.xaml", ResolvedEntryKind.File, "Views/ShellView.xaml", null, ConfidenceLevel.High, []),
            Contracts: [],
            Indexes: [],
            SelectedFiles: [],
            Unknowns: []);

        var markdown = renderer.Render(contextPack);

        Assert.Contains("## 未解決事項", markdown);
        Assert.Contains("- なし", markdown);
    }
}
