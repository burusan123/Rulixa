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

public sealed class LegacyWpfCompatibilityTests
{
    [Fact]
    public async Task ScanAsync_WithCommentedDuplicateXmlns_DoesNotCrashAndMarksStartupRoot()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-legacy-comment-alias-{Guid.NewGuid():N}");

        try
        {
            await CreateWorkspaceAsync(
                workspaceRoot,
                new WorkspaceFileDefinition(
                    "LegacyApp/App.xaml",
                    """
                    <Application StartupUri="Predict3DWindow.xaml" />
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/Predict3DWindow.xaml",
                    """
                    <Window x:Class="LegacyApp.Predict3DWindow"
                            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:local="clr-namespace:LegacyApp.ViewModels">
                        <Grid />
                    </Window>
                    <!--<Window xmlns:local="clr-namespace:LegacyApp.OtherViewModels"></Window>-->
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/Predict3DWindow.xaml.cs",
                    """
                    namespace LegacyApp;

                    public partial class Predict3DWindow
                    {
                        public Predict3DWindow()
                        {
                            InitializeComponent();
                            this.DataContext = this;
                        }
                    }
                    """));

            var scanner = new WpfNet8WorkspaceScanner(new WorkspaceFileSystem());
            var result = await scanner.ScanAsync(workspaceRoot);

            Assert.Contains(result.ViewModelBindings, binding =>
                binding.ViewPath == "LegacyApp/Predict3DWindow.xaml"
                && binding.BindingKind == ViewModelBindingKind.RootDataContext
                && binding.ViewModelSymbol == "LegacyApp.Predict3DWindow");
            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }
        finally
        {
            CleanupWorkspace(workspaceRoot);
        }
    }

    [Fact]
    public async Task ScanAsync_WithCompetingXmlnsAlias_UsesFirstDeclarationAndAddsDiagnostic()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-legacy-competing-alias-{Guid.NewGuid():N}");

        try
        {
            await CreateWorkspaceAsync(
                workspaceRoot,
                new WorkspaceFileDefinition(
                    "LegacyApp/App.xaml",
                    """
                    <Application />
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/SharedTemplates.xaml",
                    """
                    <ResourceDictionary x:Class="LegacyApp.SharedTemplates"
                                        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                        xmlns:local="clr-namespace:LegacyApp.ViewModels"
                                        xmlns:local="clr-namespace:LegacyApp.OtherViewModels">
                        <DataTemplate DataType="{x:Type local:ShellViewModel}" />
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

            Assert.Contains(result.ViewModelBindings, binding =>
                binding.ViewPath == "LegacyApp/SharedTemplates.xaml"
                && binding.ViewModelSymbol == "LegacyApp.ViewModels.ShellViewModel");
            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Code == "xaml.parse-degraded"
                && diagnostic.FilePath == "LegacyApp/SharedTemplates.xaml");
        }
        finally
        {
            CleanupWorkspace(workspaceRoot);
        }
    }

    [Fact]
    public async Task ScanAsync_ExtractsWindowActivationFromLegacyCodeBehind()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-legacy-window-activation-{Guid.NewGuid():N}");

        try
        {
            await CreateWorkspaceAsync(
                workspaceRoot,
                new WorkspaceFileDefinition(
                    "LegacyApp/App.xaml",
                    """
                    <Application StartupUri="Predict3DWindow.xaml" />
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/Predict3DWindow.xaml",
                    """
                    <Window x:Class="LegacyApp.Predict3DWindow"
                            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" />
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/Predict3DWindow.xaml.cs",
                    """
                    namespace LegacyApp;

                    public partial class Predict3DWindow
                    {
                        public void OpenSettings()
                        {
                            var window = new SettingsWindow();
                            window.ShowDialog();
                        }
                    }
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/SettingsWindow.xaml",
                    """
                    <Window x:Class="LegacyApp.SettingsWindow"
                            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" />
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/SettingsWindow.xaml.cs",
                    """
                    namespace LegacyApp;

                    public partial class SettingsWindow
                    {
                    }
                    """));

            var scanner = new WpfNet8WorkspaceScanner(new WorkspaceFileSystem());
            var result = await scanner.ScanAsync(workspaceRoot);

            Assert.Contains(result.WindowActivations, activation =>
                activation.ServiceSymbol == "LegacyApp.Predict3DWindow"
                && activation.WindowSymbol == "LegacyApp.SettingsWindow"
                && activation.ActivationKind == "show-dialog");
        }
        finally
        {
            CleanupWorkspace(workspaceRoot);
        }
    }

    [Fact]
    public async Task BuildPack_WithStartupRootAndCodeBehindDataContext_CreatesSystemPack()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-legacy-root-pack-{Guid.NewGuid():N}");

        try
        {
            await CreateWorkspaceAsync(
                workspaceRoot,
                new WorkspaceFileDefinition(
                    "LegacyApp/App.xaml",
                    """
                    <Application StartupUri="Predict3DWindow.xaml" />
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/Predict3DWindow.xaml",
                    """
                    <Window x:Class="LegacyApp.Predict3DWindow"
                            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" />
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/Predict3DWindow.xaml.cs",
                    """
                    namespace LegacyApp;

                    public partial class Predict3DWindow
                    {
                        public Predict3DWindow()
                        {
                            InitializeComponent();
                            DataContext = this;
                        }

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
                    """),
                new WorkspaceFileDefinition(
                    "LegacyApp/Models/HouseInfo.cs",
                    """
                    namespace LegacyApp.Models;

                    public sealed class HouseInfo
                    {
                        public bool IsDirty { get; private set; }

                        public void MarkDirty()
                        {
                            IsDirty = true;
                        }
                    }
                    """));

            var fileSystem = new WorkspaceFileSystem();
            var scanner = new WpfNet8WorkspaceScanner(fileSystem);
            var resolver = new ScanBackedEntryResolver();
            var extractor = new WpfNet8ContractExtractor(fileSystem);
            var buildPackUseCase = new BuildContextPackUseCase(extractor);

            var entry = new Entry(EntryKind.File, "LegacyApp/Predict3DWindow.xaml");
            var scanResult = await scanner.ScanAsync(workspaceRoot);
            var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);
            var pack = await buildPackUseCase.ExecuteAsync(
                workspaceRoot,
                scanResult,
                entry,
                resolvedEntry,
                "legacy system",
                Budget.Default);

            Assert.Equal(ResolvedEntryKind.File, resolvedEntry.ResolvedKind);
            Assert.Contains(pack.Contracts, contract => contract.Title == "System Pack");
            Assert.True(
                pack.Indexes.Any()
                || pack.Unknowns.Any()
                || pack.Contracts.Any(contract => contract.Kind == ContractKind.DialogActivation));
        }
        finally
        {
            CleanupWorkspace(workspaceRoot);
        }
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
