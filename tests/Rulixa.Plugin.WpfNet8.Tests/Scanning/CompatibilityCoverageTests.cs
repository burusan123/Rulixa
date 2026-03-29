using Rulixa.Application.UseCases;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;
using Rulixa.Infrastructure.FileSystem;
using Rulixa.Infrastructure.Resolution;
using Rulixa.Plugin.WpfNet8.Extraction;
using Rulixa.Plugin.WpfNet8.Scanning;

namespace Rulixa.Plugin.WpfNet8.Tests.Scanning;

public sealed class CompatibilityCoverageTests
{
    [Fact]
    public async Task BuildPack_WithForwardingHelperAndAdapter_PromotesReportExportFamily()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-compatibility-helper-route-{Guid.NewGuid():N}");

        try
        {
            await CreateWorkspaceAsync(
                workspaceRoot,
                new WorkspaceFileDefinition(
                    "LegacyApp/App.xaml",
                    """
                    <Application StartupUri="ShellWindow.xaml" />
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/ShellWindow.xaml",
                    """
                    <Window x:Class="LegacyApp.ShellWindow"
                            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" />
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/ShellWindow.xaml.cs",
                    """
                    namespace LegacyApp;

                    public partial class ShellWindow
                    {
                        public ShellWindow()
                        {
                            InitializeComponent();
                            DataContext = this;
                        }

                        public void ExportReport()
                        {
                            ForwardToExport();
                        }

                        private void ForwardToExport()
                        {
                            var adapter = new ReportExportAdapter();
                            adapter.Export();
                        }
                    }
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/ReportExportAdapter.cs",
                    """
                    namespace LegacyApp;

                    public sealed class ReportExportAdapter
                    {
                        public void Export()
                        {
                            var window = new ReportWindow();
                            window.ShowDialog();
                        }
                    }
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/ReportWindow.xaml",
                    """
                    <Window x:Class="LegacyApp.ReportWindow"
                            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" />
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/ReportWindow.xaml.cs",
                    """
                    namespace LegacyApp;

                    public partial class ReportWindow
                    {
                    }
                    """));

            var pack = await BuildPackAsync(workspaceRoot, new Entry(EntryKind.File, "LegacyApp/ShellWindow.xaml"), "legacy system");

            Assert.Contains(pack.Contracts, contract =>
                contract.Title == "System Pack"
                && contract.Summary.Contains("Report/Export", StringComparison.Ordinal));
            Assert.Contains(pack.Contracts, contract =>
                contract.Kind == ContractKind.DialogActivation
                && contract.Summary.Contains("ReportWindow", StringComparison.Ordinal));
        }
        finally
        {
            CleanupWorkspace(workspaceRoot);
        }
    }

    [Fact]
    public async Task ScanAsync_WithResourceDictionaryHeavyLegacyXaml_DoesNotCrashAndReturnsPartialBinding()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-compatibility-resource-dictionary-{Guid.NewGuid():N}");

        try
        {
            await CreateWorkspaceAsync(
                workspaceRoot,
                new WorkspaceFileDefinition(
                    "LegacyApp/App.xaml",
                    """
                    <Application StartupUri="ShellWindow.xaml">
                      <Application.Resources>
                        <ResourceDictionary>
                          <ResourceDictionary.MergedDictionaries>
                            <ResourceDictionary Source="Themes/SharedTemplates.xaml" />
                          </ResourceDictionary.MergedDictionaries>
                        </ResourceDictionary>
                      </Application.Resources>
                    </Application>
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/ShellWindow.xaml",
                    """
                    <Window x:Class="LegacyApp.ShellWindow"
                            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" />
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/ShellWindow.xaml.cs",
                    """
                    namespace LegacyApp;

                    public partial class ShellWindow
                    {
                        public ShellWindow()
                        {
                            InitializeComponent();
                            DataContext = this;
                        }
                    }
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/Themes/SharedTemplates.xaml",
                    """
                    <ResourceDictionary x:Class="LegacyApp.Themes.SharedTemplates"
                                        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                        xmlns:local="clr-namespace:LegacyApp.ViewModels"
                                        xmlns:local="clr-namespace:LegacyApp.OtherViewModels">
                        <!--<DataTemplate DataType="{x:Type local:IgnoredShellViewModel}" />-->
                        <Style x:Key="BodyText" TargetType="TextBlock">
                            <Setter Property="Margin" Value="4" />
                        </Style>
                        <DataTemplate DataType="{x:Type local:ShellViewModel}">
                            <TextBlock Text="Shell" Style="{StaticResource BodyText}" />
                        </DataTemplate>
                    </ResourceDictionary>
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/ViewModels/ShellViewModel.cs",
                    """
                    namespace LegacyApp.ViewModels;

                    public sealed class ShellViewModel
                    {
                    }
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/OtherViewModels/ShellViewModel.cs",
                    """
                    namespace LegacyApp.OtherViewModels;

                    public sealed class ShellViewModel
                    {
                    }
                    """));

            var scanner = new WpfNet8WorkspaceScanner(new WorkspaceFileSystem());
            var result = await scanner.ScanAsync(workspaceRoot);

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.Contains(result.ViewModelBindings, binding =>
                binding.ViewPath == "LegacyApp/Themes/SharedTemplates.xaml"
                && binding.BindingKind == ViewModelBindingKind.DataTemplate
                && binding.ViewModelSymbol == "LegacyApp.ViewModels.ShellViewModel");
            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Code == "xaml.parse-degraded"
                && diagnostic.FilePath == "LegacyApp/Themes/SharedTemplates.xaml");
            Assert.Contains(result.ViewModelBindings, binding =>
                binding.ViewPath == "LegacyApp/ShellWindow.xaml"
                && binding.BindingKind == ViewModelBindingKind.RootDataContext
                && binding.ViewModelSymbol == "LegacyApp.ShellWindow");
        }
        finally
        {
            CleanupWorkspace(workspaceRoot);
        }
    }

    [Fact]
    public async Task BuildPack_WithWeakLegacySignals_PrefersUnknownGuidanceOverFalsePersistence()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-compatibility-weak-signals-{Guid.NewGuid():N}");

        try
        {
            await CreateWorkspaceAsync(
                workspaceRoot,
                new WorkspaceFileDefinition(
                    "LegacyApp/App.xaml",
                    """
                    <Application StartupUri="ShellWindow.xaml" />
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/ShellWindow.xaml",
                    """
                    <Window x:Class="LegacyApp.ShellWindow"
                            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" />
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/ShellWindow.xaml.cs",
                    """
                    namespace LegacyApp;

                    public partial class ShellWindow
                    {
                        public ShellWindow()
                        {
                            InitializeComponent();
                            DataContext = this;
                        }

                        public void OpenAssistant()
                        {
                            LaunchAssistant();
                        }

                        private void LaunchAssistant()
                        {
                            var helper = new AssistantOverlay();
                            helper.Show();
                        }
                    }
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/AssistantOverlay.cs",
                    """
                    namespace LegacyApp;

                    public sealed class AssistantOverlay
                    {
                        public void Show()
                        {
                        }
                    }
                    """));

            var pack = await BuildPackAsync(workspaceRoot, new Entry(EntryKind.File, "LegacyApp/ShellWindow.xaml"), "legacy system");

            Assert.DoesNotContain(pack.Indexes.Where(index => index.Title == "Persistence").SelectMany(static index => index.Lines),
                line => line.Contains("Repository", StringComparison.Ordinal)
                    || line.Contains("Query", StringComparison.Ordinal)
                    || line.Contains("Store", StringComparison.Ordinal));
            Assert.Contains(pack.Unknowns, unknown =>
                unknown.Code == "persistence.missing-owner"
                || unknown.Code.StartsWith("workflow.", StringComparison.Ordinal));
        }
        finally
        {
            CleanupWorkspace(workspaceRoot);
        }
    }

    private static async Task<ContextPack> BuildPackAsync(string workspaceRoot, Entry entry, string goal)
    {
        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);
        var buildPackUseCase = new BuildContextPackUseCase(extractor);

        var scanResult = await scanner.ScanAsync(workspaceRoot);
        var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);
        return await buildPackUseCase.ExecuteAsync(
            workspaceRoot,
            scanResult,
            entry,
            resolvedEntry,
            goal,
            Budget.Default);
    }

    private static async Task CreateWorkspaceAsync(string workspaceRoot, params WorkspaceFileDefinition[] files)
    {
        foreach (var file in files)
        {
            var absolutePath = Path.Combine(workspaceRoot, file.Path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            await File.WriteAllTextAsync(absolutePath, file.Content);
        }
    }

    private static void CleanupWorkspace(string workspaceRoot)
    {
        if (Directory.Exists(workspaceRoot))
        {
            Directory.Delete(workspaceRoot, recursive: true);
        }
    }

    private sealed record WorkspaceFileDefinition(string Path, string Content);
}
