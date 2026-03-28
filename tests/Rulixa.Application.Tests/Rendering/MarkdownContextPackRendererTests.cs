using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Infrastructure.Rendering;

namespace Rulixa.Application.Tests.Rendering;

public sealed class MarkdownContextPackRendererTests
{
    [Fact]
    public void Render_UsesJapaneseLabelsAndRendersSelectedSnippetsInPriorityOrder()
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
                    ContractKind.Navigation,
                    "選択から表示への反映",
                    "SelectedItem の更新が CurrentPage の表示切り替えを駆動します。",
                    ["src/App/ViewModels/ShellViewModel.cs"],
                    ["SelectedItem", "CurrentPage"])
            ],
            Indexes:
            [
                new IndexSection("View-ViewModel", ["MainWindow.xaml <-> App.ViewModels.ShellViewModel (ルート DataContext: MainWindow.xaml.cs)"]),
                new IndexSection("DI", ["ShellViewModel (Singleton)"]),
                new IndexSection("ナビゲーション更新点", ["App.ViewModels.ShellViewModel.Select(...) -> CurrentPage = item.PageViewModel (line: 42)"])
            ],
            SelectedSnippets:
            [
                new SelectedSnippet(
                    @".\src\App\Views\ShellView.xaml",
                    "navigation-xaml-binding",
                    8,
                    true,
                    "SelectedItem",
                    12,
                    18,
                    "<ListBox ItemsSource=\"{Binding Items}\"\n         SelectedItem=\"{Binding SelectedItem}\" />"),
                new SelectedSnippet(
                    @".\src\App\ViewModels\ShellViewModel.cs",
                    "navigation-update",
                    10,
                    true,
                    "Select(...)",
                    40,
                    48,
                    "private void Select(NavItemViewModel item)\n{\n    CurrentPage = item.PageViewModel;\n}"),
                new SelectedSnippet(
                    @".\src\App\ServiceRegistration.cs",
                    "dependency-injection",
                    5,
                    true,
                    "ShellViewModel (Singleton)",
                    8,
                    10,
                    "services.AddSingleton<ShellViewModel>();"),
                new SelectedSnippet(
                    @".\src\App\Views\MainWindow.xaml.cs",
                    "root-binding-source",
                    -10,
                    true,
                    "ルート DataContext",
                    6,
                    9,
                    "public MainWindow(ShellViewModel shellViewModel)\n{\n    DataContext = shellViewModel;\n}")
            ],
            SelectedFiles:
            [
                new SelectedFile(@".\src\App\Views\ShellView.xaml", "entry", 0, true, 42),
                new SelectedFile(@".\MainWindow.xaml.cs", "startup", 10, true, 10)
            ],
            DecisionTraces: [],
            Unknowns:
            [
                new Diagnostic("entry.unresolved", "候補が複数あります。", null, DiagnosticSeverity.Warning, [])
            ]);

        var markdown = renderer.Render(contextPack);

        Assert.Contains("### src/App/Views/MainWindow.xaml.cs:6-9", markdown);
        Assert.Contains("### src/App/Views/ShellView.xaml:12-18", markdown);
        Assert.Contains("### src/App/ServiceRegistration.cs:8-10", markdown);
        Assert.Contains("### src/App/ViewModels/ShellViewModel.cs:40-48", markdown);
        Assert.Contains("```csharp", markdown);
        Assert.Contains("```xml", markdown);
        Assert.Contains("XAML ナビゲーション binding", markdown);
        Assert.DoesNotContain(@".\", markdown);

        var bindingPosition = markdown.IndexOf("### src/App/Views/MainWindow.xaml.cs:6-9", StringComparison.Ordinal);
        var xamlPosition = markdown.IndexOf("### src/App/Views/ShellView.xaml:12-18", StringComparison.Ordinal);
        var registrationPosition = markdown.IndexOf("### src/App/ServiceRegistration.cs:8-10", StringComparison.Ordinal);
        var navigationPosition = markdown.IndexOf("### src/App/ViewModels/ShellViewModel.cs:40-48", StringComparison.Ordinal);
        var filePathPosition = markdown.LastIndexOf("`src/App/Views/ShellView.xaml`", StringComparison.Ordinal);

        Assert.True(bindingPosition >= 0);
        Assert.True(xamlPosition > bindingPosition);
        Assert.True(registrationPosition > bindingPosition);
        Assert.True(navigationPosition > registrationPosition);
        Assert.True(filePathPosition > navigationPosition);
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

        Assert.Contains("##", markdown);
        Assert.Contains("-", markdown);
    }
}
