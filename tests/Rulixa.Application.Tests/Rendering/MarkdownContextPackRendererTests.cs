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
                    "一覧・選択・表示の対応",
                    "src/App/Views/ShellView.xaml では 一覧を Items にバインド、選択状態を SelectedItem にバインド、表示コンテンツを CurrentPage にバインド してページ切り替えを表現します。",
                    ["src/App/Views/ShellView.xaml"],
                    ["App.ViewModels.ShellViewModel", "Items", "SelectedItem", "CurrentPage"]),
                new Contract(
                    ContractKind.Navigation,
                    "選択から表示への因果",
                    "SelectedItem の選択更新が CurrentPage の表示切り替えを駆動します。RestoreSelection(...) が SelectedItem = match、Select(...) が CurrentPage = item.PageViewModel を実行します。",
                    ["src/App/ViewModels/ShellViewModel.cs"],
                    ["App.ViewModels.ShellViewModel", "SelectedItem", "CurrentPage"]),
                new Contract(
                    ContractKind.Navigation,
                    "ViewModel 更新点",
                    "App.ViewModels.ShellViewModel.Select(...) が CurrentPage = item.PageViewModel を実行します。",
                    ["src/App/ViewModels/ShellViewModel.cs"],
                    ["App.ViewModels.ShellViewModel", "Select", "CurrentPage = item.PageViewModel"]),
                new Contract(
                    ContractKind.ViewModelBinding,
                    "ルート DataContext",
                    "MainWindow.xaml.cs が MainWindow.xaml の DataContext に App.ViewModels.ShellViewModel を設定します。",
                    ["MainWindow.xaml", "MainWindow.xaml.cs"],
                    ["MainWindow", "App.ViewModels.ShellViewModel"]),
                new Contract(
                    ContractKind.ViewModelBinding,
                    "DataTemplate 二次文脈",
                    "src/App/Views/ShellView.xaml には 13 件の DataTemplate 二次文脈があります。例: CeilingPageViewModel, ConstructionPageViewModel, EquipmentPageViewModel。",
                    ["src/App/Views/ShellView.xaml"],
                    ["CeilingPageViewModel", "ConstructionPageViewModel", "EquipmentPageViewModel"])
            ],
            Indexes:
            [
                new IndexSection("ナビゲーション", ["src/App/Views/ShellView.xaml: Items=Items, SelectedItem=SelectedItem, Content=CurrentPage"]),
                new IndexSection("選択から表示への因果", ["SelectedItem -> CurrentPage (RestoreSelection: SelectedItem = match, Select: CurrentPage = item.PageViewModel)"]),
                new IndexSection("ナビゲーション更新点", ["App.ViewModels.ShellViewModel.Select(...) -> CurrentPage = item.PageViewModel (line: 42)"]),
                new IndexSection("View-ViewModel", ["MainWindow.xaml <-> App.ViewModels.ShellViewModel (ルート DataContext: MainWindow.xaml.cs)", "src/App/Views/ShellView.xaml <-> DataTemplate 二次文脈 13件 (例: CeilingPageViewModel, ConstructionPageViewModel, EquipmentPageViewModel)"]),
                new IndexSection("起動経路", ["App.xaml.cs -> App.ViewModels.ShellViewModel"]),
                new IndexSection("DI", ["ShellViewModel (Singleton)", "直接依存 3件: Singleton 2、Transient 1 (例: IProjectWorkspaceService, ISettingWindowService, ILicenseService)"]),
                new IndexSection("コマンド", ["OpenSettingsCommand -> App.ViewModels.ShellViewModel.OpenSettings"])
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
        Assert.Contains("### 選択から表示への因果", markdown);
        Assert.Contains("CurrentPage = item.PageViewModel (line: 42)", markdown);
        Assert.Contains("### DI", markdown);
        Assert.Contains("ShellViewModel (Singleton)", markdown);
        Assert.Contains("直接依存のライフタイム", markdown);
        Assert.Contains("DataTemplate 二次文脈", markdown);
        Assert.DoesNotContain("向けの DataTemplate", markdown);
        Assert.Contains("[警告] entry.unresolved", markdown);

        var rootBindingPosition = markdown.IndexOf("[View と ViewModel の対応] ルート DataContext", StringComparison.Ordinal);
        var diViewModelPosition = markdown.IndexOf("[依存関係の構成] 主要 ViewModel の登録", StringComparison.Ordinal);
        var diDependencyPosition = markdown.IndexOf("[依存関係の構成] 直接依存のライフタイム", StringComparison.Ordinal);
        var causePosition = markdown.IndexOf("[ナビゲーション] 選択から表示への因果", StringComparison.Ordinal);
        var navigationPosition = markdown.IndexOf("[ナビゲーション] 一覧・選択・表示の対応", StringComparison.Ordinal);
        var dataTemplatePosition = markdown.IndexOf("[View と ViewModel の対応] DataTemplate 二次文脈", StringComparison.Ordinal);
        Assert.True(rootBindingPosition >= 0);
        Assert.True(diViewModelPosition > rootBindingPosition);
        Assert.True(diDependencyPosition > diViewModelPosition);
        Assert.True(causePosition > rootBindingPosition);
        Assert.True(navigationPosition > causePosition);
        Assert.True(dataTemplatePosition > navigationPosition);

        var navigationIndexPosition = markdown.IndexOf("### ナビゲーション", StringComparison.Ordinal);
        var causeIndexPosition = markdown.IndexOf("### 選択から表示への因果", StringComparison.Ordinal);
        var navigationUpdateIndexPosition = markdown.IndexOf("### ナビゲーション更新点", StringComparison.Ordinal);
        var viewModelIndexPosition = markdown.IndexOf("### View-ViewModel", StringComparison.Ordinal);
        var startupIndexPosition = markdown.IndexOf("### 起動経路", StringComparison.Ordinal);
        var diIndexPosition = markdown.IndexOf("### DI", StringComparison.Ordinal);
        var commandIndexPosition = markdown.IndexOf("### コマンド", StringComparison.Ordinal);
        Assert.True(navigationIndexPosition >= 0);
        Assert.True(causeIndexPosition > navigationIndexPosition);
        Assert.True(navigationUpdateIndexPosition > causeIndexPosition);
        Assert.True(viewModelIndexPosition > navigationUpdateIndexPosition);
        Assert.True(startupIndexPosition > viewModelIndexPosition);
        Assert.True(diIndexPosition > startupIndexPosition);
        Assert.True(commandIndexPosition > diIndexPosition);
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
