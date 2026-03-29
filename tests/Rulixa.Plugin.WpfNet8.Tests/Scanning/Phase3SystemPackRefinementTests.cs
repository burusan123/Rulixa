using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Infrastructure.FileSystem;
using Rulixa.Infrastructure.Resolution;
using Rulixa.Plugin.WpfNet8.Extraction;
using Rulixa.Plugin.WpfNet8.Scanning;

namespace Rulixa.Plugin.WpfNet8.Tests.Scanning;

public sealed class Phase3SystemPackRefinementTests
{
    [Fact]
    public async Task BuildPack_ForRootViewModel_AggregatesDraftingUnknownsAtSystemLevel()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-phase3-system-refine-{Guid.NewGuid():N}");

        try
        {
            await CreateDraftingUnknownWorkspaceAsync(workspaceRoot);

            var pack = await BuildContextPackFromWorkspaceAsync(
                workspaceRoot,
                new Entry(EntryKind.Symbol, "Sample.Presentation.Wpf.ViewModels.ShellViewModel"),
                "drafting ai analyze");

            var diagnostic = Assert.Single(
                pack.Unknowns,
                static unknown => unknown.Code == "workflow.missing-downstream");
            Assert.InRange(diagnostic.Candidates.Count, 1, 3);
            Assert.Contains(diagnostic.Candidates, candidate => candidate.Contains("WallAlgorithmRunner", StringComparison.Ordinal));
            Assert.Contains(diagnostic.Candidates, candidate => candidate.Contains("DiagramAnalyzer", StringComparison.Ordinal));
            Assert.Contains(pack.Contracts, contract =>
                contract.Title == "System Pack"
                && contract.Summary.Contains("Drafting", StringComparison.Ordinal)
                && contract.Summary.Contains("Settings", StringComparison.Ordinal)
                && contract.Summary.Contains("Report/Export", StringComparison.Ordinal));
        }
        finally
        {
            DeleteWorkspace(workspaceRoot);
        }
    }

    [Fact]
    public async Task BuildPack_ForDialogServiceRoute_PromotesDraftingFamilyThroughAdapter()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-phase3-dialog-route-{Guid.NewGuid():N}");

        try
        {
            await CreateDialogServiceWorkspaceAsync(workspaceRoot);

            var pack = await BuildContextPackFromWorkspaceAsync(
                workspaceRoot,
                new Entry(EntryKind.Symbol, "Sample.Presentation.Wpf.ViewModels.ShellViewModel"),
                "project system");

            var systemPack = Assert.Single(pack.Contracts, contract => contract.Title == "System Pack");
            Assert.Contains("Drafting", systemPack.Summary, StringComparison.Ordinal);
            Assert.Contains("ProjectDocument", systemPack.Summary, StringComparison.Ordinal);
            Assert.DoesNotContain(pack.Unknowns, unknown =>
                unknown.Code == "workflow.missing-downstream"
                && unknown.Candidates.Any(candidate => candidate.Contains("DraftingWindowViewModel", StringComparison.Ordinal)));
        }
        finally
        {
            DeleteWorkspace(workspaceRoot);
        }
    }

    [Fact]
    public async Task BuildPack_ForSiblingViewModels_StabilizesThreeDAndReportFamilies()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-phase3-sibling-route-{Guid.NewGuid():N}");

        try
        {
            await CreateSiblingWorkspaceAsync(workspaceRoot);

            var pack = await BuildContextPackFromWorkspaceAsync(
                workspaceRoot,
                new Entry(EntryKind.Symbol, "Sample.Presentation.Wpf.ViewModels.ShellViewModel"),
                "system");

            var systemPack = Assert.Single(pack.Contracts, contract => contract.Title == "System Pack");
            Assert.Contains("3D", systemPack.Summary, StringComparison.Ordinal);
            Assert.Contains("Report/Export", systemPack.Summary, StringComparison.Ordinal);
            Assert.Contains("ProjectDocument", systemPack.Summary, StringComparison.Ordinal);
            Assert.Single(pack.Unknowns, static unknown => unknown.Code == "workflow.missing-downstream");
        }
        finally
        {
            DeleteWorkspace(workspaceRoot);
        }
    }

    private static async Task<ContextPack> BuildContextPackFromWorkspaceAsync(
        string workspaceRoot,
        Entry entry,
        string goal)
    {
        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);

        var scanResult = await scanner.ScanAsync(workspaceRoot);
        var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);
        var ingredients = await extractor.ExtractAsync(workspaceRoot, scanResult, resolvedEntry, goal);
        return ContextPackFactory.Create(goal, entry, resolvedEntry, ingredients, scanResult, Budget.Default);
    }

    private static async Task CreateDraftingUnknownWorkspaceAsync(string workspaceRoot)
    {
        await CreateWorkspaceAsync(
            workspaceRoot,
            [
                ProjectFile(),
                AppXaml(),
                MainWindowXaml(),
                MainWindowCodeBehind("ShellViewModel"),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/ViewModels/ShellViewModel.cs",
                    """
                    using Sample.Presentation.Wpf.Models;

                    namespace Sample.Presentation.Wpf.ViewModels;

                    public sealed class ShellViewModel
                    {
                        public ShellViewModel(
                            DraftingWindowViewModel draftingWindowViewModel,
                            SettingsViewModel settingsViewModel,
                            ReportViewModel reportViewModel,
                            ProjectDocument projectDocument)
                        {
                        }
                    }
                    """),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/ViewModels/DraftingWindowViewModel.cs",
                    """
                    using Sample.Presentation.Wpf.Services;

                    namespace Sample.Presentation.Wpf.ViewModels;

                    public sealed class DraftingWindowViewModel
                    {
                        public DraftingWindowViewModel(
                            IDraftingWorkflowPort draftingWorkflowPort,
                            IDraftingReviewPort draftingReviewPort)
                        {
                        }
                    }
                    """),
                EmptyViewModel("SettingsViewModel"),
                EmptyViewModel("ReportViewModel"),
                ProjectDocumentModel(),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/Services/DraftingWorkflowPort.cs",
                    """
                    namespace Sample.Presentation.Wpf.Services;

                    public interface IDraftingWorkflowPort
                    {
                        void Run();
                    }

                    public sealed class DraftingWorkflowPort : IDraftingWorkflowPort
                    {
                        private const string Guidance = "DiagramAnalyzer WallAlgorithmRunner";

                        public void Run()
                        {
                        }
                    }
                    """),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/Services/DraftingReviewPort.cs",
                    """
                    namespace Sample.Presentation.Wpf.Services;

                    public interface IDraftingReviewPort
                    {
                        void Review();
                    }

                    public sealed class DraftingReviewPort : IDraftingReviewPort
                    {
                        private const string Guidance = "DiagramAnalyzer WallAlgorithmRunner";

                        public void Review()
                        {
                        }
                    }
                    """),
                SettingsQueryService(),
                ReportExportService(),
                ServiceRegistration(
                    """
                    services.AddSingleton<ShellViewModel>();
                    services.AddScoped<DraftingWindowViewModel>();
                    services.AddScoped<SettingsViewModel>();
                    services.AddScoped<ReportViewModel>();
                    services.AddSingleton<ProjectDocument>();
                    services.AddScoped<IDraftingWorkflowPort, DraftingWorkflowPort>();
                    services.AddScoped<IDraftingReviewPort, DraftingReviewPort>();
                    services.AddScoped<ISettingsQuery, SettingsQuery>();
                    services.AddScoped<IReportExportService, ReportExportService>();
                    """),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/Algorithms/A/DiagramAnalyzer.cs",
                    "namespace Sample.Algorithms.A; public sealed class DiagramAnalyzer { }"),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/Algorithms/B/DiagramAnalyzer.cs",
                    "namespace Sample.Algorithms.B; public sealed class DiagramAnalyzer { }"),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/Algorithms/A/WallAlgorithmRunner.cs",
                    "namespace Sample.Algorithms.A; public sealed class WallAlgorithmRunner { }"),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/Algorithms/B/WallAlgorithmRunner.cs",
                    "namespace Sample.Algorithms.B; public sealed class WallAlgorithmRunner { }"),
                ArchitectureTest()
            ]);
    }

    private static async Task CreateDialogServiceWorkspaceAsync(string workspaceRoot)
    {
        await CreateWorkspaceAsync(
            workspaceRoot,
            [
                ProjectFile(),
                AppXaml(),
                MainWindowXaml(),
                MainWindowCodeBehind("ShellViewModel"),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/ViewModels/ShellViewModel.cs",
                    """
                    using Sample.Presentation.Wpf.Models;
                    using Sample.Presentation.Wpf.Services;

                    namespace Sample.Presentation.Wpf.ViewModels;

                    public sealed class ShellViewModel
                    {
                        private readonly IDraftingWindowService draftingWindowService;

                        public ShellViewModel(
                            IDraftingWindowService draftingWindowService,
                            SettingsViewModel settingsViewModel,
                            ProjectDocument projectDocument)
                        {
                            this.draftingWindowService = draftingWindowService;
                        }

                        public void OpenDrafting()
                        {
                            draftingWindowService.Show();
                        }
                    }
                    """),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/ViewModels/DraftingWindowViewModel.cs",
                    """
                    using Sample.Presentation.Wpf.Models;

                    namespace Sample.Presentation.Wpf.ViewModels;

                    public sealed class DraftingWindowViewModel
                    {
                        public DraftingWindowViewModel(ProjectDocument projectDocument)
                        {
                        }
                    }
                    """),
                EmptyViewModel("SettingsViewModel"),
                ProjectDocumentModel(),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/Services/DraftingWindowService.cs",
                    """
                    using Sample.Presentation.Wpf.ViewModels;

                    namespace Sample.Presentation.Wpf.Services;

                    public interface IDraftingWindowService
                    {
                        void Show();
                    }

                    public sealed class DraftingWindowService : IDraftingWindowService
                    {
                        private readonly DraftingDialogAdapter adapter;

                        public DraftingWindowService(DraftingDialogAdapter adapter)
                        {
                            this.adapter = adapter;
                        }

                        public void Show()
                        {
                            adapter.Show();
                        }
                    }

                    public sealed class DraftingDialogAdapter
                    {
                        private readonly DraftingWindowViewModel draftingWindowViewModel;

                        public DraftingDialogAdapter(DraftingWindowViewModel draftingWindowViewModel)
                        {
                            this.draftingWindowViewModel = draftingWindowViewModel;
                        }

                        public void Show()
                        {
                            Execute(() => draftingWindowViewModel.GetHashCode());
                        }

                        private static void Execute(Action action)
                        {
                            action();
                        }
                    }
                    """),
                SettingsQueryService(),
                ServiceRegistration(
                    """
                    services.AddSingleton<ShellViewModel>();
                    services.AddScoped<DraftingWindowViewModel>();
                    services.AddScoped<SettingsViewModel>();
                    services.AddSingleton<ProjectDocument>();
                    services.AddScoped<IDraftingWindowService, DraftingWindowService>();
                    services.AddScoped<DraftingDialogAdapter>();
                    services.AddScoped<ISettingsQuery, SettingsQuery>();
                    """),
                ArchitectureTest()
            ]);
    }

    private static async Task CreateSiblingWorkspaceAsync(string workspaceRoot)
    {
        await CreateWorkspaceAsync(
            workspaceRoot,
            [
                ProjectFile(),
                AppXaml(),
                MainWindowXaml(),
                MainWindowCodeBehind("ShellViewModel"),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/ViewModels/ShellViewModel.cs",
                    """
                    using Sample.Presentation.Wpf.Models;

                    namespace Sample.Presentation.Wpf.ViewModels;

                    public sealed class ShellViewModel
                    {
                        public ShellViewModel(
                            ShellThreeDViewModel shellThreeDViewModel,
                            ReportCenterViewModel reportCenterViewModel,
                            ProjectDocument projectDocument,
                            OrphanWorkflowService orphanWorkflowService)
                        {
                        }
                    }
                    """),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/ViewModels/ShellThreeDViewModel.cs",
                    "namespace Sample.Presentation.Wpf.ViewModels; public sealed class ShellThreeDViewModel { }"),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/ViewModels/ReportCenterViewModel.cs",
                    "namespace Sample.Presentation.Wpf.ViewModels; public sealed class ReportCenterViewModel { }"),
                ProjectDocumentModel(),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/Services/ReportExportService.cs",
                    """
                    namespace Sample.Presentation.Wpf.Services;

                    public sealed class ReportExportService
                    {
                        private const string TemplatePath = "Templates/report.template";
                    }
                    """),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/Services/ThreeDWorkspaceService.cs",
                    "namespace Sample.Presentation.Wpf.Services; public sealed class ThreeDWorkspaceService { }"),
                new WorkspaceFileDefinition(
                    "src/Sample.Presentation.Wpf/Services/OrphanWorkflowService.cs",
                    """
                    namespace Sample.Presentation.Wpf.Services;

                    public sealed class OrphanWorkflowService
                    {
                        private const string Guidance = "ReportAnalyzer ReportAnalyzer";
                    }
                    """),
                ServiceRegistration(
                    """
                    services.AddSingleton<ShellViewModel>();
                    services.AddScoped<ShellThreeDViewModel>();
                    services.AddScoped<ReportCenterViewModel>();
                    services.AddSingleton<ProjectDocument>();
                    services.AddScoped<ThreeDWorkspaceService>();
                    services.AddScoped<ReportExportService>();
                    services.AddScoped<OrphanWorkflowService>();
                    """),
                ArchitectureTest()
            ]);
    }

    private static WorkspaceFileDefinition ProjectFile() =>
        new(
            "src/Sample.Presentation.Wpf/Sample.Presentation.Wpf.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0-windows</TargetFramework>
                <UseWPF>true</UseWPF>
              </PropertyGroup>
            </Project>
            """);

    private static WorkspaceFileDefinition AppXaml() =>
        new("src/Sample.Presentation.Wpf/App.xaml", "<Application />");

    private static WorkspaceFileDefinition MainWindowXaml() =>
        new(
            "src/Sample.Presentation.Wpf/Views/MainWindow.xaml",
            """
            <Window
                x:Class="Sample.Presentation.Wpf.Views.MainWindow"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <Grid />
            </Window>
            """);

    private static WorkspaceFileDefinition MainWindowCodeBehind(string rootViewModelName) =>
        new(
            "src/Sample.Presentation.Wpf/Views/MainWindow.xaml.cs",
            $@"using Sample.Presentation.Wpf.ViewModels;

namespace Sample.Presentation.Wpf.Views;

public partial class MainWindow
{{
    public MainWindow({rootViewModelName} shellViewModel)
    {{
        DataContext = shellViewModel;
    }}
}}");

    private static WorkspaceFileDefinition EmptyViewModel(string name) =>
        new(
            $"src/Sample.Presentation.Wpf/ViewModels/{name}.cs",
            $"namespace Sample.Presentation.Wpf.ViewModels; public sealed class {name} {{ }}");

    private static WorkspaceFileDefinition ProjectDocumentModel() =>
        new(
            "src/Sample.Presentation.Wpf/Models/ProjectDocument.cs",
            """
            namespace Sample.Presentation.Wpf.Models;

            public sealed class ProjectDocument
            {
                public bool IsDirty { get; private set; }

                public void MarkDirty()
                {
                    IsDirty = true;
                }

                public void MarkSaved()
                {
                    IsDirty = false;
                }
            }
            """);

    private static WorkspaceFileDefinition SettingsQueryService() =>
        new(
            "src/Sample.Presentation.Wpf/Services/SettingsQuery.cs",
            """
            namespace Sample.Presentation.Wpf.Services;

            public interface ISettingsQuery
            {
            }

            public sealed class SettingsQuery : ISettingsQuery
            {
                private const string SettingsPath = "WorkspaceSettings.xlsx";
            }
            """);

    private static WorkspaceFileDefinition ReportExportService() =>
        new(
            "src/Sample.Presentation.Wpf/Services/ReportExportService.cs",
            """
            namespace Sample.Presentation.Wpf.Services;

            public interface IReportExportService
            {
            }

            public sealed class ReportExportService : IReportExportService
            {
                private const string TemplatePath = "report.template";
            }
            """);

    private static WorkspaceFileDefinition ServiceRegistration(string registrations) =>
        new(
            "src/Sample.Presentation.Wpf/ServiceRegistration.cs",
            $@"using Microsoft.Extensions.DependencyInjection;
using Sample.Presentation.Wpf.Models;
using Sample.Presentation.Wpf.Services;
using Sample.Presentation.Wpf.ViewModels;

namespace Sample.Presentation.Wpf;

public static class ServiceRegistration
{{
    public static void Register(IServiceCollection services)
    {{
{registrations}
    }}
}}");

    private static WorkspaceFileDefinition ArchitectureTest() =>
        new(
            "tests/Sample.Architecture.Tests/LayerGuardTests.cs",
            """
            namespace Sample.Architecture.Tests;

            public sealed class LayerGuardTests
            {
            }
            """);

    private static async Task CreateWorkspaceAsync(string workspaceRoot, IReadOnlyList<WorkspaceFileDefinition> files)
    {
        foreach (var file in files)
        {
            var absolutePath = Path.Combine(workspaceRoot, file.Path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            await File.WriteAllTextAsync(absolutePath, file.Content);
        }
    }

    private static void DeleteWorkspace(string workspaceRoot)
    {
        if (Directory.Exists(workspaceRoot))
        {
            Directory.Delete(workspaceRoot, recursive: true);
        }
    }

    private sealed record WorkspaceFileDefinition(
        string Path,
        string Content);
}
