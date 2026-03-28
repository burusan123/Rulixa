using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Infrastructure.Rendering;

namespace Rulixa.Application.Tests.Rendering;

public sealed class MarkdownContextPackRendererTests
{
    [Fact]
    public void Render_UsesJapaneseLabelsAndRendersSelectedSnippets()
    {
        var renderer = new MarkdownContextPackRenderer();
        var contextPack = new ContextPack(
            Goal: "Shell 画面に新しいページを追加したい。",
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
                    ContractKind.DependencyInjection,
                    "主要 ViewModel の登録",
                    "ShellViewModel は Singleton として DI 登録されます。",
                    ["ServiceRegistration.cs"],
                    ["App.ViewModels.ShellViewModel"]),
                new Contract(
                    ContractKind.DependencyInjection,
                    "直接依存のライフタイム",
                    "ShellViewModel の直接依存 3 件は Singleton 2、Transient 1 です。例: IProjectWorkspaceService, ISettingWindowService, ILicenseService。",
                    ["ServiceRegistration.cs"],
                    ["IProjectWorkspaceService", "ISettingWindowService", "ILicenseService"]),
                new Contract(
                    ContractKind.Navigation,
                    "選択から表示への因果",
                    "SelectedItem の選択更新が CurrentPage の表示切り替えを駆動します。",
                    ["src/App/ViewModels/ShellViewModel.cs"],
                    ["SelectedItem", "CurrentPage"])
            ],
            Indexes:
            [
                new IndexSection("ナビゲーション", ["src/App/Views/ShellView.xaml: Items=Items, SelectedItem=SelectedItem, Content=CurrentPage"]),
                new IndexSection("選択から表示への因果", ["SelectedItem -> CurrentPage (RestoreSelection: SelectedItem = match, Select: CurrentPage = item.PageViewModel)"]),
                new IndexSection("ナビゲーション更新点", ["App.ViewModels.ShellViewModel.Select(...) -> CurrentPage = item.PageViewModel (line: 42)"]),
                new IndexSection("View-ViewModel", ["MainWindow.xaml <-> App.ViewModels.ShellViewModel (ルート DataContext: MainWindow.xaml.cs)"]),
                new IndexSection("起動経路", ["App.xaml.cs -> App.ViewModels.ShellViewModel"]),
                new IndexSection("DI", ["ShellViewModel (Singleton)"]),
                new IndexSection("コマンド", ["OpenSettingsCommand -> App.ViewModels.ShellViewModel.OpenSettings"])
            ],
            SelectedSnippets:
            [
                new SelectedSnippet(
                    @".\src\App\ViewModels\ShellViewModel.cs",
                    "dependency-injection",
                    0,
                    true,
                    "ShellViewModel(...)",
                    12,
                    32,
                    "public ShellViewModel(IProjectWorkspaceService workspaceService)\n{\n    _workspaceService = workspaceService;\n}")
            ],
            SelectedFiles:
            [
                new SelectedFile(@".\src\App\Views\ShellView.xaml", "entry", 0, true, 42),
                new SelectedFile(@".\MainWindow.xaml.cs", "startup", 10, true, 10)
            ],
            Unknowns:
            [
                new Diagnostic("entry.unresolved", "候補が複数あります。", null, DiagnosticSeverity.Warning, [])
            ]);

        var markdown = renderer.Render(contextPack);

        Assert.Contains("# コンテキストパック", markdown);
        Assert.Contains("## 目的", markdown);
        Assert.Contains("## 重要スニペット", markdown);
        Assert.Contains("### src/App/ViewModels/ShellViewModel.cs:12-32", markdown);
        Assert.Contains("理由: DI 登録", markdown);
        Assert.Contains("アンカー: `ShellViewModel(...)`", markdown);
        Assert.Contains("```csharp", markdown);
        Assert.Contains("## 選定ファイル", markdown);
        Assert.Contains("理由: 入口", markdown);
        Assert.DoesNotContain(@".\", markdown);

        var snippetPosition = markdown.IndexOf("## 重要スニペット", StringComparison.Ordinal);
        var filePosition = markdown.IndexOf("## 選定ファイル", StringComparison.Ordinal);
        Assert.True(snippetPosition >= 0);
        Assert.True(filePosition > snippetPosition);
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
            Unknowns: []);

        var markdown = renderer.Render(contextPack);

        Assert.Contains("## 重要スニペット", markdown);
        Assert.Contains("## 未解決事項", markdown);
        Assert.Contains("- なし", markdown);
    }
}
