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
            await CreateWorkspaceAsync(workspaceRoot);

            var pack = await BuildContextPackFromWorkspaceAsync(
                workspaceRoot,
                new Entry(EntryKind.Symbol, "Sample.Presentation.Wpf.ViewModels.ShellViewModel"),
                "drafting ai analyze");

            var diagnostic = Assert.Single(
                pack.Unknowns,
                static unknown => unknown.Code == "workflow.missing-downstream");
            Assert.InRange(pack.Unknowns.Count(static unknown => unknown.Code == "workflow.missing-downstream"), 1, 1);
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
            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, recursive: true);
            }
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

    private static async Task CreateWorkspaceAsync(string workspaceRoot)
    {
        var files = new[]
        {
            new WorkspaceFileDefinition(
                "src/Sample.Presentation.Wpf/Sample.Presentation.Wpf.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0-windows</TargetFramework>
                    <UseWPF>true</UseWPF>
                  </PropertyGroup>
                </Project>
                """),
            new WorkspaceFileDefinition("src/Sample.Presentation.Wpf/App.xaml", "<Application />"),
            new WorkspaceFileDefinition(
                "src/Sample.Presentation.Wpf/Views/MainWindow.xaml",
                """
                <Window
                    x:Class="Sample.Presentation.Wpf.Views.MainWindow"
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                    <Grid />
                </Window>
                """),
            new WorkspaceFileDefinition(
                "src/Sample.Presentation.Wpf/Views/MainWindow.xaml.cs",
                """
                using Sample.Presentation.Wpf.ViewModels;

                namespace Sample.Presentation.Wpf.Views;

                public partial class MainWindow
                {
                    public MainWindow(ShellViewModel shellViewModel)
                    {
                        DataContext = shellViewModel;
                    }
                }
                """),
            new WorkspaceFileDefinition(
                "src/Sample.Presentation.Wpf/ViewModels/ShellViewModel.cs",
                """
                using Sample.Presentation.Wpf.Services;

                namespace Sample.Presentation.Wpf.ViewModels;

                public sealed class ShellViewModel
                {
                    public ShellViewModel(
                        DraftingWindowViewModel draftingWindowViewModel,
                        SettingsViewModel settingsViewModel,
                        ReportViewModel reportViewModel)
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
            new WorkspaceFileDefinition(
                "src/Sample.Presentation.Wpf/ViewModels/SettingsViewModel.cs",
                """
                namespace Sample.Presentation.Wpf.ViewModels;

                public sealed class SettingsViewModel
                {
                }
                """),
            new WorkspaceFileDefinition(
                "src/Sample.Presentation.Wpf/ViewModels/ReportViewModel.cs",
                """
                namespace Sample.Presentation.Wpf.ViewModels;

                public sealed class ReportViewModel
                {
                }
                """),
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
            new WorkspaceFileDefinition(
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
                """),
            new WorkspaceFileDefinition(
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
                """),
            new WorkspaceFileDefinition(
                "src/Sample.Presentation.Wpf/Services/ThreeDWorkspaceService.cs",
                """
                namespace Sample.Presentation.Wpf.Services;

                public sealed class ThreeDWorkspaceService
                {
                }
                """),
            new WorkspaceFileDefinition(
                "src/Sample.Presentation.Wpf/ServiceRegistration.cs",
                """
                using Microsoft.Extensions.DependencyInjection;
                using Sample.Presentation.Wpf.Services;
                using Sample.Presentation.Wpf.ViewModels;

                namespace Sample.Presentation.Wpf;

                public static class ServiceRegistration
                {
                    public static void Register(IServiceCollection services)
                    {
                        services.AddSingleton<ShellViewModel>();
                        services.AddScoped<DraftingWindowViewModel>();
                        services.AddScoped<SettingsViewModel>();
                        services.AddScoped<ReportViewModel>();
                        services.AddScoped<IDraftingWorkflowPort, DraftingWorkflowPort>();
                        services.AddScoped<IDraftingReviewPort, DraftingReviewPort>();
                        services.AddScoped<ISettingsQuery, SettingsQuery>();
                        services.AddScoped<IReportExportService, ReportExportService>();
                        services.AddScoped<ThreeDWorkspaceService>();
                    }
                }
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
            new WorkspaceFileDefinition(
                "tests/Sample.Architecture.Tests/LayerGuardTests.cs",
                """
                namespace Sample.Architecture.Tests;

                public sealed class LayerGuardTests
                {
                }
                """)
        };

        foreach (var file in files)
        {
            var absolutePath = Path.Combine(workspaceRoot, file.Path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            await File.WriteAllTextAsync(absolutePath, file.Content);
        }
    }

    private sealed record WorkspaceFileDefinition(
        string Path,
        string Content);
}
