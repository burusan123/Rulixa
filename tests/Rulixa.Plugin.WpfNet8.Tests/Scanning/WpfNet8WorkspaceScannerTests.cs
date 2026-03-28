using System.Text.Json;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;
using Rulixa.Infrastructure.FileSystem;
using Rulixa.Infrastructure.Resolution;
using Rulixa.Plugin.WpfNet8.Extraction;
using Rulixa.Plugin.WpfNet8.Scanning;

namespace Rulixa.Plugin.WpfNet8.Tests.Scanning;

public sealed class WpfNet8WorkspaceScannerTests
{
    private static readonly string FixtureRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "AssessMeisterLike"));

    [Fact]
    public async Task ScanAsync_ExtractsWpfFactsFromFixture()
    {
        var scanner = new WpfNet8WorkspaceScanner(new WorkspaceFileSystem());

        var result = await scanner.ScanAsync(FixtureRoot);

        Assert.True(result.ProjectSummary.UsesWpf);
        Assert.Contains("net8.0-windows", result.ProjectSummary.TargetFrameworks);
        Assert.Contains(result.ProjectSummary.RootViewModels, value => value.EndsWith(".ShellViewModel", StringComparison.Ordinal));

        Assert.Contains(result.ViewModelBindings, binding =>
            binding.ViewPath.EndsWith("Views/MainWindow.xaml", StringComparison.Ordinal)
            && binding.BindingKind == ViewModelBindingKind.RootDataContext
            && binding.ViewModelSymbol.EndsWith(".ShellViewModel", StringComparison.Ordinal)
            && binding.SourceSpan.StartLine > 0
            && binding.SourceSpan.EndLine >= binding.SourceSpan.StartLine);
        Assert.Contains(result.ViewModelBindings, binding =>
            binding.ViewPath.EndsWith("Views/ShellView.xaml", StringComparison.Ordinal)
            && binding.BindingKind == ViewModelBindingKind.DataTemplate
            && binding.SourceSpan.StartLine > 0);

        Assert.Contains(result.NavigationTransitions, transition =>
            transition.ViewModelSymbol.EndsWith(".ShellViewModel", StringComparison.Ordinal)
            && transition.UpdateMethodName == "Select"
            && transition.UpdateExpressionSummary == "CurrentPage = item.PageViewModel"
            && transition.SourceSpan.StartLine > 0);
        Assert.Contains(result.NavigationTransitions, transition =>
            transition.UpdateExpressionSummary == "SelectedItem = match"
            && transition.SourceSpan.StartLine > 0);

        Assert.Contains(result.Commands, command => command.PropertyName == "OpenSettingsCommand");
        Assert.Contains(result.ServiceRegistrations, registration =>
            registration.ServiceType == "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel"
            && registration.Lifetime == ServiceRegistrationLifetime.Singleton
            && registration.SourceSpan.StartLine == 11
            && registration.SourceSpan.EndLine == 13);
        Assert.Contains(result.ServiceRegistrations, registration =>
            registration.ServiceType == "AssessMeister.Presentation.Wpf.Services.IProjectWorkspaceService"
            && registration.Lifetime == ServiceRegistrationLifetime.Singleton
            && registration.SourceSpan.StartLine == 14
            && registration.SourceSpan.EndLine == 14);
        Assert.Contains(result.ServiceRegistrations, registration =>
            registration.ServiceType == "AssessMeister.Presentation.Wpf.Services.ISettingWindowService"
            && registration.Lifetime == ServiceRegistrationLifetime.Transient
            && registration.SourceSpan.StartLine == 15
            && registration.SourceSpan.EndLine == 15);
        Assert.Contains(result.WindowActivations, activation =>
            activation.ServiceSymbol == "AssessMeister.Presentation.Wpf.Services.SettingWindowService"
            && activation.WindowSymbol == "AssessMeister.Presentation.Wpf.Views.SettingWindow");
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public async Task ExtractAsync_ForShellViewFile_UsesSourceSpansForBindingAndRegistrationSnippets()
    {
        var (scanResult, ingredients, pack) = await BuildPackAsync(
            new Entry(EntryKind.File, "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml"),
            new Budget(MaxFiles: 10, MaxTotalLines: 5000, MaxSnippetsPerFile: 3),
            PromoteShellViewModelToLargeFile);

        var selectedPaths = pack.SelectedFiles.Select(static file => file.Path).ToArray();
        var dataTemplateBindings = scanResult.ViewModelBindings
            .Where(binding =>
                binding.ViewPath == "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml"
                && binding.BindingKind == ViewModelBindingKind.DataTemplate)
            .ToArray();

        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/App.xaml.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/ServiceRegistration.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Common/DelegateCommand.cs", selectedPaths);
        Assert.DoesNotContain("src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs", selectedPaths);

        Assert.Contains(pack.SelectedSnippets, snippet =>
            snippet.Path == "src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml.cs"
            && snippet.Reason == "root-binding-source"
            && snippet.Content.Contains("DataContext = shellViewModel;", StringComparison.Ordinal));
        Assert.Contains(pack.SelectedSnippets, snippet =>
            snippet.Path == "src/AssessMeister.Presentation.Wpf/ServiceRegistration.cs"
            && snippet.Reason == "dependency-injection"
            && snippet.Content.Contains("services.AddSingleton<", StringComparison.Ordinal)
            && snippet.Content.Contains("ShellViewModel", StringComparison.Ordinal)
            && snippet.Content.Contains(">();", StringComparison.Ordinal)
            && snippet.StartLine <= 11
            && snippet.EndLine >= 13);
        Assert.Contains(pack.SelectedSnippets, snippet =>
            snippet.Path == "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml"
            && snippet.Reason == "navigation-xaml-binding"
            && snippet.Content.Contains("SelectedItem=\"{Binding SelectedItem", StringComparison.Ordinal));
        Assert.Contains(pack.SelectedSnippets, snippet =>
            snippet.Path == "src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs"
            && snippet.Anchor.Contains("ShellViewModel(...)", StringComparison.Ordinal)
            && snippet.Anchor.Contains("Select(...)", StringComparison.Ordinal));

        Assert.Contains(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.ViewModelBinding
            && contract.Summary.Contains("DataTemplate", StringComparison.Ordinal)
            && contract.Summary.Contains(dataTemplateBindings.Length.ToString(), StringComparison.Ordinal));
        Assert.Contains(ingredients.Indexes, index =>
            index.Title == "View-ViewModel"
            && index.Lines.Any(line =>
                line.Contains("MainWindow.xaml <-> AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel", StringComparison.Ordinal)));
        Assert.Contains(ingredients.Indexes.SelectMany(static index => index.Lines), line =>
            line.Contains("SelectedItem = match", StringComparison.Ordinal));
        Assert.Contains(ingredients.Indexes.SelectMany(static index => index.Lines), line =>
            line.Contains("CurrentPage = item.PageViewModel", StringComparison.Ordinal));
        Assert.Contains(ingredients.Indexes, index =>
            index.Title == "DI"
            && index.Lines.Any(line => line.Contains("ShellViewModel (Singleton)", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ExtractAsync_ForShellViewModelSymbol_ReplacesLargeFileWithinDefaultBudget()
    {
        var (_, ingredients, pack) = await BuildPackAsync(
            new Entry(EntryKind.Symbol, "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel"),
            Budget.Default,
            PromoteShellViewModelToLargeFile);

        var selectedFiles = pack.SelectedFiles.ToArray();
        var selectedPaths = selectedFiles.Select(static file => file.Path).ToArray();

        Assert.DoesNotContain(selectedPaths, path => path == "src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs");
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/App.xaml.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/SettingWindow.xaml", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/ServiceRegistration.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Common/DelegateCommand.cs", selectedPaths);
        Assert.DoesNotContain(selectedPaths, path => path.Contains("/Pages/", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(pack.SelectedSnippets, snippet =>
            snippet.Path == "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml"
            && snippet.Reason == "navigation-xaml-binding");
        Assert.Contains(pack.SelectedSnippets, snippet =>
            snippet.Path == "src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml.cs"
            && snippet.Reason == "root-binding-source");
        Assert.Contains(pack.SelectedSnippets, snippet =>
            snippet.Path == "src/AssessMeister.Presentation.Wpf/ServiceRegistration.cs"
            && snippet.Reason == "dependency-injection"
            && snippet.Content.Contains("services.AddSingleton<", StringComparison.Ordinal)
            && snippet.Content.Contains("ShellViewModel", StringComparison.Ordinal)
            && snippet.Content.Contains(">();", StringComparison.Ordinal)
            && snippet.StartLine <= 11
            && snippet.EndLine >= 13);
        Assert.Contains(pack.SelectedSnippets, snippet =>
            snippet.Path == "src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs"
            && snippet.Anchor.Contains("ShellViewModel(...)", StringComparison.Ordinal)
            && snippet.Anchor.Contains("Select(...)", StringComparison.Ordinal));

        Assert.Contains(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.DependencyInjection
            && contract.Summary.Contains("ShellViewModel", StringComparison.Ordinal)
            && contract.Summary.Contains("Singleton", StringComparison.Ordinal));
        Assert.Contains(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.DialogActivation
            && contract.Summary.Contains("show-dialog", StringComparison.Ordinal)
            && contract.Summary.Contains("SettingWindow", StringComparison.Ordinal));
        Assert.Contains(ingredients.Indexes, index =>
            index.Title == "DI"
            && index.Lines.Any(line =>
                line.Contains("Singleton", StringComparison.Ordinal)
                && !line.Equals("ShellViewModel (Singleton)", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ExtractAsync_ForShellViewModelSymbol_AddsNavigationContractAndOrderedSnippets()
    {
        var (_, ingredients, pack) = await BuildPackAsync(
            new Entry(EntryKind.Symbol, "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel"),
            Budget.Default,
            PromoteShellViewModelToLargeFile);

        Assert.Contains(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.Navigation
            && contract.Summary.Contains("SelectedItem", StringComparison.Ordinal)
            && contract.Summary.Contains("CurrentPage", StringComparison.Ordinal));
        Assert.Contains(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.Navigation
            && contract.Summary.Contains("SelectedItem = match", StringComparison.Ordinal)
            && contract.Summary.Contains("CurrentPage = item.PageViewModel", StringComparison.Ordinal));
        Assert.Contains(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.Navigation
            && contract.Summary.Contains(".Select(...)", StringComparison.Ordinal)
            && contract.Summary.Contains("CurrentPage = item.PageViewModel", StringComparison.Ordinal));
        Assert.Contains(ingredients.Indexes.SelectMany(static index => index.Lines), line =>
            line.Contains("SelectedItem -> CurrentPage", StringComparison.Ordinal)
            && line.Contains("SelectedItem = match", StringComparison.Ordinal)
            && line.Contains("CurrentPage = item.PageViewModel", StringComparison.Ordinal));
        Assert.Contains(ingredients.Indexes.SelectMany(static index => index.Lines), line =>
            line.Contains("Select(...)", StringComparison.Ordinal)
            && line.Contains("CurrentPage = item.PageViewModel", StringComparison.Ordinal));
        Assert.Contains(ingredients.Indexes.SelectMany(static index => index.Lines), line =>
            line.Contains("SelectedItem = match", StringComparison.Ordinal)
            && line.Contains("line:", StringComparison.Ordinal));

        Assert.DoesNotContain(pack.SelectedFiles, file => file.Path.EndsWith("ShellViewModel.cs", StringComparison.Ordinal));

        var orderedSnippetPaths = pack.SelectedSnippets.Select(static snippet => snippet.Path).ToArray();
        var bindingIndex = Array.IndexOf(orderedSnippetPaths, "src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml.cs");
        var viewBindingIndex = Array.IndexOf(orderedSnippetPaths, "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml.cs");
        var xamlIndex = Array.IndexOf(orderedSnippetPaths, "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml");
        var registrationIndex = Array.IndexOf(orderedSnippetPaths, "src/AssessMeister.Presentation.Wpf/ServiceRegistration.cs");
        var navigationIndex = Array.IndexOf(orderedSnippetPaths, "src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs");

        Assert.True(bindingIndex >= 0);
        Assert.True(viewBindingIndex > bindingIndex);
        Assert.True(registrationIndex > viewBindingIndex);
        Assert.True(xamlIndex > registrationIndex);
        Assert.True(navigationIndex > registrationIndex);
    }

    [Fact]
    public async Task ResolveEntryAsync_WithAutoShell_ExcludesSecondaryDataTemplateViewModels()
    {
        var scanner = new WpfNet8WorkspaceScanner(new WorkspaceFileSystem());
        var resolver = new ScanBackedEntryResolver();

        var scanResult = await scanner.ScanAsync(FixtureRoot);
        var resolved = await resolver.ResolveAsync(new Entry(EntryKind.Auto, "Shell"), scanResult);

        Assert.Equal(ResolvedEntryKind.Unresolved, resolved.ResolvedKind);
        Assert.Contains(resolved.Candidates, candidate =>
            candidate.Kind == CandidateKind.View
            && candidate.Path == "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml");
        Assert.Contains(resolved.Candidates, candidate =>
            candidate.Kind == CandidateKind.ViewModel
            && candidate.Symbol == "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel");
        Assert.DoesNotContain(resolved.Candidates, candidate =>
            candidate.Symbol == "AssessMeister.Presentation.Wpf.ViewModels.DashboardPageViewModel");
        Assert.DoesNotContain(resolved.Candidates, candidate =>
            candidate.Path == "src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml");
    }

    [Fact]
    public async Task ExtractAsync_WhenCommandsAreMany_SummarizesCommandContracts()
    {
        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);

        var scanResult = await scanner.ScanAsync(FixtureRoot);
        var baseCommand = Assert.Single(scanResult.Commands);
        var expandedCommands = new[]
            {
                baseCommand
            }
            .Concat(Enumerable.Range(1, 6)
                .Select(index => new CommandBinding(
                    baseCommand.ViewModelSymbol,
                    $"SampleCommand{index}",
                    baseCommand.CommandType,
                    $"{baseCommand.ViewModelSymbol}.ExecuteSample{index}",
                    baseCommand.CanExecuteSymbol,
                    baseCommand.BoundViews)))
            .ToArray();
        var expandedScanResult = scanResult with
        {
            Commands = expandedCommands,
            Files = scanResult.Files
                .Select(file =>
                    file.Path is "src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs"
                        or "src/AssessMeister.Presentation.Wpf/Services/SettingWindowService.cs"
                        ? file with { LineCount = 300 }
                        : file)
                .ToArray()
        };
        var entry = new Entry(EntryKind.Symbol, "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel");
        var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);

        var ingredients = await extractor.ExtractAsync(
            FixtureRoot,
            expandedScanResult,
            resolvedEntry,
            "\u8A2D\u5B9A\u753B\u9762\u3092\u958B\u304D\u305F\u3044");

        Assert.Contains(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.Command
            && contract.Summary.Contains("7", StringComparison.Ordinal)
            && contract.Summary.Contains("OpenSettingsCommand", StringComparison.Ordinal));
        Assert.Contains(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.Command
            && contract.Title == "OpenSettingsCommand"
            && contract.Summary.Contains("OpenSettings(...)", StringComparison.Ordinal)
            && contract.Summary.Contains("OpenSettingsCore(...)", StringComparison.Ordinal)
            && contract.Summary.Contains("ISettingWindowService.Show(...)", StringComparison.Ordinal)
            && contract.Summary.Contains("SettingWindow", StringComparison.Ordinal)
            && contract.Summary.Contains("show-dialog", StringComparison.Ordinal));
        Assert.DoesNotContain(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.Command
            && contract.Title == "SampleCommand1");
        Assert.Contains(ingredients.Indexes.SelectMany(static index => index.Lines), line =>
            line.Contains("7", StringComparison.Ordinal)
            && line.Contains("OpenSettingsCommand", StringComparison.Ordinal));
        Assert.Contains(ingredients.Indexes.SelectMany(static index => index.Lines), line =>
            line.Contains("OpenSettingsCommand", StringComparison.Ordinal)
            && line.Contains("OpenSettingsCore(...)", StringComparison.Ordinal)
            && line.Contains("ISettingWindowService.Show(...)", StringComparison.Ordinal)
            && line.Contains("SettingWindow", StringComparison.Ordinal)
            && line.Contains("show-dialog", StringComparison.Ordinal));
        Assert.DoesNotContain(ingredients.SnippetCandidates, snippet =>
            snippet.Anchor.Contains("ExecuteSample1", StringComparison.Ordinal));
        Assert.Contains(ingredients.SnippetCandidates, snippet =>
            snippet.Anchor.Contains("OpenSettings(...)", StringComparison.Ordinal));
        Assert.Contains(ingredients.SnippetCandidates, snippet =>
            snippet.Anchor.Contains("OpenSettingsCore(...)", StringComparison.Ordinal));
        Assert.Contains(ingredients.SnippetCandidates, snippet =>
            snippet.Anchor.Contains("Show(...)", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExtractAsync_WhenCommandUsesThisHelper_TracksHelperRouteAndSnippet()
    {
        var (_, ingredients, _) = await BuildPackAsync(
            new Entry(EntryKind.Symbol, "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel"),
            Budget.Default,
            scanResult => scanResult with
            {
                Files = scanResult.Files
                    .Select(file =>
                        file.Path is "src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs"
                            or "src/AssessMeister.Presentation.Wpf/Services/SettingWindowService.cs"
                            ? file with { LineCount = 300 }
                            : file)
                    .ToArray()
            },
            "\u8A2D\u5B9A\u753B\u9762\u3092\u958B\u304D\u305F\u3044");

        Assert.Contains(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.Command
            && contract.Title == "OpenSettingsCommand"
            && contract.Summary.Contains("OpenSettings(...)", StringComparison.Ordinal)
            && contract.Summary.Contains("OpenSettingsCore(...)", StringComparison.Ordinal)
            && contract.Summary.Contains("ISettingWindowService.Show(...)", StringComparison.Ordinal)
            && contract.Summary.Contains("show-dialog", StringComparison.Ordinal));
        Assert.Contains(ingredients.Indexes.SelectMany(static index => index.Lines), line =>
            line.Contains("OpenSettingsCommand", StringComparison.Ordinal)
            && line.Contains("ShellViewModel.OpenSettingsCore(...)", StringComparison.Ordinal)
            && line.Contains("ISettingWindowService.Show(...)", StringComparison.Ordinal));
        Assert.Contains(ingredients.SnippetCandidates, snippet =>
            snippet.Anchor.Contains("OpenSettingsCore(...)", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExtractAsync_WhenCommandsAreManyAndGoalDoesNotMatch_UsesSummaryOnly()
    {
        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);

        var scanResult = await scanner.ScanAsync(FixtureRoot);
        var baseCommand = Assert.Single(scanResult.Commands);
        var expandedCommands = new[]
            {
                baseCommand
            }
            .Concat(Enumerable.Range(1, 6)
                .Select(index => new CommandBinding(
                    baseCommand.ViewModelSymbol,
                    $"SampleCommand{index}",
                    baseCommand.CommandType,
                    $"{baseCommand.ViewModelSymbol}.ExecuteSample{index}",
                    baseCommand.CanExecuteSymbol,
                    baseCommand.BoundViews)))
            .ToArray();
        var expandedScanResult = scanResult with { Commands = expandedCommands };
        var entry = new Entry(EntryKind.Symbol, "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel");
        var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);

        var ingredients = await extractor.ExtractAsync(
            FixtureRoot,
            expandedScanResult,
            resolvedEntry,
            "\u30E9\u30A4\u30BB\u30F3\u30B9\u901A\u77E5\u3092\u78BA\u8A8D\u3057\u305F\u3044");

        Assert.Contains(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.Command
            && contract.Summary.Contains("7", StringComparison.Ordinal));
        Assert.DoesNotContain(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.Command
            && contract.Title == "OpenSettingsCommand");
        Assert.DoesNotContain(ingredients.Indexes.SelectMany(static index => index.Lines), line =>
            line.Contains("OpenSettingsCommand", StringComparison.Ordinal)
            && line.Contains("ISettingWindowService.Show(...)", StringComparison.Ordinal));
        Assert.DoesNotContain(ingredients.SnippetCandidates, snippet =>
            snippet.Anchor.Contains("OpenSettings(...)", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExtractAsync_ForDirectServiceCallWithoutDialog_DoesNotInventWindowActivation()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-command-impact-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ViewModels"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Services"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Common"));

        try
        {
            await File.WriteAllTextAsync(Path.Combine(workspaceRoot, "Sample.sln"), string.Empty);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Sample.Presentation.Wpf.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0-windows</TargetFramework>
                    <UseWPF>true</UseWPF>
                  </PropertyGroup>
                </Project>
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Services", "ExportService.cs"),
                """
                namespace Sample.Presentation.Wpf.Services;

                public interface IExportService
                {
                    void Save();
                }

                public sealed class ExportService : IExportService
                {
                    public void Save()
                    {
                    }
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ViewModels", "ShellViewModel.cs"),
                """
                using Sample.Presentation.Wpf.Common;
                using Sample.Presentation.Wpf.Services;

                namespace Sample.Presentation.Wpf.ViewModels;

                public sealed class ShellViewModel
                {
                    private readonly IExportService exportService;

                    public DelegateCommand ExportCommand { get; }

                    public ShellViewModel(IExportService exportService)
                    {
                        this.exportService = exportService;
                        ExportCommand = new DelegateCommand(Export);
                    }

                    private void Export()
                    {
                        exportService.Save();
                    }
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views", "ShellView.xaml"),
                """
                <Window
                    x:Class="Sample.Presentation.Wpf.Views.ShellView"
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                </Window>
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views", "ShellView.xaml.cs"),
                """
                using Sample.Presentation.Wpf.ViewModels;

                namespace Sample.Presentation.Wpf.Views;

                public partial class ShellView
                {
                    public ShellView(ShellViewModel shellViewModel)
                    {
                        DataContext = shellViewModel;
                    }
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Common", "DelegateCommand.cs"),
                """
                using System;
                using System.Windows.Input;

                namespace Sample.Presentation.Wpf.Common;

                public sealed class DelegateCommand : ICommand
                {
                    private readonly Action execute;

                    public DelegateCommand(Action execute)
                    {
                        this.execute = execute;
                    }

                    public event EventHandler? CanExecuteChanged;

                    public bool CanExecute(object? parameter) => true;

                    public void Execute(object? parameter) => execute();
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ServiceRegistration.cs"),
                """
                using Microsoft.Extensions.DependencyInjection;
                using Sample.Presentation.Wpf.Services;
                using Sample.Presentation.Wpf.ViewModels;

                namespace Sample.Presentation.Wpf;

                public static class ServiceRegistration
                {
                    public static IServiceCollection AddPresentation(this IServiceCollection services)
                    {
                        services.AddSingleton<ShellViewModel>();
                        services.AddTransient<IExportService, ExportService>();
                        return services;
                    }
                }
                """);

            var fileSystem = new WorkspaceFileSystem();
            var scanner = new WpfNet8WorkspaceScanner(fileSystem);
            var resolver = new ScanBackedEntryResolver();
            var extractor = new WpfNet8ContractExtractor(fileSystem);

            var scanResult = await scanner.ScanAsync(workspaceRoot);
            var entry = new Entry(EntryKind.Symbol, "Sample.Presentation.Wpf.ViewModels.ShellViewModel");
            var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);

            var ingredients = await extractor.ExtractAsync(
                workspaceRoot,
                scanResult,
                resolvedEntry,
                "\u30A8\u30AF\u30B9\u30DD\u30FC\u30C8\u3057\u305F\u3044");

            Assert.Contains(ingredients.Contracts, contract =>
                contract.Kind == ContractKind.Command
                && contract.Title == "ExportCommand"
                && contract.Summary.Contains("IExportService.Save(...)", StringComparison.Ordinal));
            Assert.DoesNotContain(ingredients.Contracts, contract =>
                contract.Kind == ContractKind.Command
                && contract.Title == "ExportCommand"
                && contract.Summary.Contains("ShellViewModel.Save(...)", StringComparison.Ordinal));
            Assert.DoesNotContain(ingredients.Contracts, contract =>
                contract.Kind == ContractKind.Command
                && contract.Title == "ExportCommand"
                && contract.Summary.Contains("show-dialog", StringComparison.Ordinal));
            Assert.DoesNotContain(ingredients.Indexes.SelectMany(static index => index.Lines), line =>
                line.Contains("show-dialog", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExtractAsync_ForHelperRouteWithoutDialog_DoesNotInventWindowActivation()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-command-helper-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ViewModels"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Services"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Common"));

        try
        {
            await File.WriteAllTextAsync(Path.Combine(workspaceRoot, "Sample.sln"), string.Empty);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Sample.Presentation.Wpf.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0-windows</TargetFramework>
                    <UseWPF>true</UseWPF>
                  </PropertyGroup>
                </Project>
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Services", "ExportService.cs"),
                """
                namespace Sample.Presentation.Wpf.Services;

                public interface IExportService
                {
                    void Save();
                }

                public sealed class ExportService : IExportService
                {
                    public void Save()
                    {
                    }
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ViewModels", "ShellViewModel.cs"),
                """
                using Sample.Presentation.Wpf.Common;
                using Sample.Presentation.Wpf.Services;

                namespace Sample.Presentation.Wpf.ViewModels;

                public sealed class ShellViewModel
                {
                    private readonly IExportService exportService;

                    public DelegateCommand ExportCommand { get; }

                    public ShellViewModel(IExportService exportService)
                    {
                        this.exportService = exportService;
                        ExportCommand = new DelegateCommand(Export);
                    }

                    private void Export()
                    {
                        this.ExportCore();
                    }

                    private void ExportCore()
                    {
                        exportService.Save();
                    }
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views", "ShellView.xaml"),
                """
                <Window
                    x:Class="Sample.Presentation.Wpf.Views.ShellView"
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                </Window>
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views", "ShellView.xaml.cs"),
                """
                using Sample.Presentation.Wpf.ViewModels;

                namespace Sample.Presentation.Wpf.Views;

                public partial class ShellView
                {
                    public ShellView(ShellViewModel shellViewModel)
                    {
                        DataContext = shellViewModel;
                    }
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Common", "DelegateCommand.cs"),
                """
                using System;
                using System.Windows.Input;

                namespace Sample.Presentation.Wpf.Common;

                public sealed class DelegateCommand : ICommand
                {
                    private readonly Action execute;

                    public DelegateCommand(Action execute)
                    {
                        this.execute = execute;
                    }

                    public event EventHandler? CanExecuteChanged;

                    public bool CanExecute(object? parameter) => true;

                    public void Execute(object? parameter) => execute();
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ServiceRegistration.cs"),
                """
                using Microsoft.Extensions.DependencyInjection;
                using Sample.Presentation.Wpf.Services;
                using Sample.Presentation.Wpf.ViewModels;

                namespace Sample.Presentation.Wpf;

                public static class ServiceRegistration
                {
                    public static IServiceCollection AddPresentation(this IServiceCollection services)
                    {
                        services.AddSingleton<ShellViewModel>();
                        services.AddTransient<IExportService, ExportService>();
                        return services;
                    }
                }
                """);

            var fileSystem = new WorkspaceFileSystem();
            var scanner = new WpfNet8WorkspaceScanner(fileSystem);
            var resolver = new ScanBackedEntryResolver();
            var extractor = new WpfNet8ContractExtractor(fileSystem);

            var scanResult = await scanner.ScanAsync(workspaceRoot);
            var entry = new Entry(EntryKind.Symbol, "Sample.Presentation.Wpf.ViewModels.ShellViewModel");
            var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);

            var ingredients = await extractor.ExtractAsync(
                workspaceRoot,
                scanResult,
                resolvedEntry,
                "\u30A8\u30AF\u30B9\u30DD\u30FC\u30C8\u3057\u305F\u3044");

            Assert.Contains(ingredients.Contracts, contract =>
                contract.Kind == ContractKind.Command
                && contract.Title == "ExportCommand"
                && contract.Summary.Contains("ExportCore(...)", StringComparison.Ordinal)
                && contract.Summary.Contains("IExportService.Save(...)", StringComparison.Ordinal));
            Assert.DoesNotContain(ingredients.Contracts, contract =>
                contract.Kind == ContractKind.Command
                && contract.Title == "ExportCommand"
                && contract.Summary.Contains("show-dialog", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExtractAsync_ForHelperRoute_LimitsExpansionToOneHop()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-command-helper-depth-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ViewModels"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Services"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Common"));

        try
        {
            await File.WriteAllTextAsync(Path.Combine(workspaceRoot, "Sample.sln"), string.Empty);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Sample.Presentation.Wpf.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0-windows</TargetFramework>
                    <UseWPF>true</UseWPF>
                  </PropertyGroup>
                </Project>
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Services", "ExportService.cs"),
                """
                namespace Sample.Presentation.Wpf.Services;

                public interface IExportService
                {
                    void Save();
                }

                public sealed class ExportService : IExportService
                {
                    public void Save()
                    {
                    }
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ViewModels", "ShellViewModel.cs"),
                """
                using Sample.Presentation.Wpf.Common;
                using Sample.Presentation.Wpf.Services;

                namespace Sample.Presentation.Wpf.ViewModels;

                public sealed class ShellViewModel
                {
                    private readonly IExportService exportService;

                    public DelegateCommand ExportCommand { get; }

                    public ShellViewModel(IExportService exportService)
                    {
                        this.exportService = exportService;
                        ExportCommand = new DelegateCommand(Export);
                    }

                    private void Export()
                    {
                        ExportCore();
                    }

                    private void ExportCore()
                    {
                        ExportCoreInner();
                    }

                    private void ExportCoreInner()
                    {
                        exportService.Save();
                    }
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views", "ShellView.xaml"),
                """
                <Window
                    x:Class="Sample.Presentation.Wpf.Views.ShellView"
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                </Window>
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views", "ShellView.xaml.cs"),
                """
                using Sample.Presentation.Wpf.ViewModels;

                namespace Sample.Presentation.Wpf.Views;

                public partial class ShellView
                {
                    public ShellView(ShellViewModel shellViewModel)
                    {
                        DataContext = shellViewModel;
                    }
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Common", "DelegateCommand.cs"),
                """
                using System;
                using System.Windows.Input;

                namespace Sample.Presentation.Wpf.Common;

                public sealed class DelegateCommand : ICommand
                {
                    private readonly Action execute;

                    public DelegateCommand(Action execute)
                    {
                        this.execute = execute;
                    }

                    public event EventHandler? CanExecuteChanged;

                    public bool CanExecute(object? parameter) => true;

                    public void Execute(object? parameter) => execute();
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ServiceRegistration.cs"),
                """
                using Microsoft.Extensions.DependencyInjection;
                using Sample.Presentation.Wpf.Services;
                using Sample.Presentation.Wpf.ViewModels;

                namespace Sample.Presentation.Wpf;

                public static class ServiceRegistration
                {
                    public static IServiceCollection AddPresentation(this IServiceCollection services)
                    {
                        services.AddSingleton<ShellViewModel>();
                        services.AddTransient<IExportService, ExportService>();
                        return services;
                    }
                }
                """);

            var fileSystem = new WorkspaceFileSystem();
            var scanner = new WpfNet8WorkspaceScanner(fileSystem);
            var resolver = new ScanBackedEntryResolver();
            var extractor = new WpfNet8ContractExtractor(fileSystem);

            var scanResult = await scanner.ScanAsync(workspaceRoot);
            var entry = new Entry(EntryKind.Symbol, "Sample.Presentation.Wpf.ViewModels.ShellViewModel");
            var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);

            var ingredients = await extractor.ExtractAsync(
                workspaceRoot,
                scanResult,
                resolvedEntry,
                "\u30A8\u30AF\u30B9\u30DD\u30FC\u30C8\u3057\u305F\u3044");

            Assert.DoesNotContain(ingredients.Contracts, contract =>
                contract.Kind == ContractKind.Command
                && contract.Title == "ExportCommand"
                && contract.Summary.Contains("IExportService.Save(...)", StringComparison.Ordinal));
            Assert.DoesNotContain(ingredients.Contracts, contract =>
                contract.Kind == ContractKind.Command
                && contract.Title == "ExportCommand"
                && contract.Summary.Contains("ExportCore(...)", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ScanAsync_IgnoresGeneratedPublishAndWpfTempFiles()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-scan-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspaceRoot);

        try
        {
            var projectDirectory = Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf");
            var viewsDirectory = Path.Combine(projectDirectory, "Views");
            Directory.CreateDirectory(viewsDirectory);
            Directory.CreateDirectory(Path.Combine(workspaceRoot, "publish"));

            await File.WriteAllTextAsync(Path.Combine(workspaceRoot, "Sample.sln"), string.Empty);
            await File.WriteAllTextAsync(
                Path.Combine(projectDirectory, "Sample.Presentation.Wpf.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0-windows</TargetFramework>
                    <UseWPF>true</UseWPF>
                  </PropertyGroup>
                </Project>
                """);
            await File.WriteAllTextAsync(
                Path.Combine(projectDirectory, "Sample.Presentation.Wpf_abcd_wpftmp.csproj"),
                "<Project />");
            await File.WriteAllTextAsync(Path.Combine(workspaceRoot, "publish", "Generated.cs"), "public class Generated {}");
            await File.WriteAllTextAsync(Path.Combine(projectDirectory, "App.xaml"), "<Application />");
            await File.WriteAllTextAsync(
                Path.Combine(viewsDirectory, "MainWindow.xaml"),
                "<Window x:Class=\"Sample.Presentation.Wpf.Views.MainWindow\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" />");

            var scanner = new WpfNet8WorkspaceScanner(new WorkspaceFileSystem());

            var result = await scanner.ScanAsync(workspaceRoot);

            Assert.DoesNotContain(result.Files, file => file.Path.StartsWith("publish/", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(result.ProjectSummary.ProjectFiles, path => path.Contains("_wpftmp.csproj", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ScanAsync_UsesLatestFileTimestampAsGeneratedAtUtc()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-scan-time-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views"));

        try
        {
            var solutionPath = Path.Combine(workspaceRoot, "Sample.sln");
            var projectPath = Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Sample.Presentation.Wpf.csproj");
            var appPath = Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "App.xaml");
            var viewPath = Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views", "MainWindow.xaml");

            await File.WriteAllTextAsync(solutionPath, string.Empty);
            await File.WriteAllTextAsync(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF></PropertyGroup></Project>");
            await File.WriteAllTextAsync(appPath, "<Application />");
            await File.WriteAllTextAsync(viewPath, "<Window x:Class=\"Sample.Presentation.Wpf.Views.MainWindow\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" />");

            File.SetLastWriteTimeUtc(solutionPath, new DateTime(2026, 03, 28, 1, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(projectPath, new DateTime(2026, 03, 28, 2, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(appPath, new DateTime(2026, 03, 28, 3, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(viewPath, new DateTime(2026, 03, 28, 4, 0, 0, DateTimeKind.Utc));

            var scanner = new WpfNet8WorkspaceScanner(new WorkspaceFileSystem());

            var result = await scanner.ScanAsync(workspaceRoot);

            Assert.Equal(new DateTimeOffset(2026, 03, 28, 4, 0, 0, TimeSpan.Zero), result.GeneratedAtUtc);
        }
        finally
        {
            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ScanAsync_AddsDiagnosticWhenBindingViewModelIsAmbiguous()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-scan-ambiguous-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ViewModels", "A"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ViewModels", "B"));

        try
        {
            await File.WriteAllTextAsync(Path.Combine(workspaceRoot, "Sample.sln"), string.Empty);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Sample.Presentation.Wpf.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF></PropertyGroup></Project>");
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views", "ShellView.xaml"),
                "<Window x:Class=\"Sample.Presentation.Wpf.Views.ShellView\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" />");
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views", "ShellView.xaml.cs"),
                """
                namespace Sample.Presentation.Wpf.Views;

                public partial class ShellView
                {
                    public ShellView(ShellViewModel shellViewModel)
                    {
                        DataContext = shellViewModel;
                    }
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ViewModels", "A", "ShellViewModel.cs"),
                "namespace Sample.Presentation.Wpf.ViewModels.A; public sealed class ShellViewModel { }");
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ViewModels", "B", "ShellViewModel.cs"),
                "namespace Sample.Presentation.Wpf.ViewModels.B; public sealed class ShellViewModel { }");

            var scanner = new WpfNet8WorkspaceScanner(new WorkspaceFileSystem());

            var result = await scanner.ScanAsync(workspaceRoot);

            Assert.Contains(result.Diagnostics, diagnostic =>
                diagnostic.Code == "binding.viewmodel.ambiguous"
                && diagnostic.FilePath == "src/Sample.Presentation.Wpf/Views/ShellView.xaml");
        }
        finally
        {
            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ScanAsync_MergesPartialClassSymbolsForResolveEntry()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-scan-partial-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ViewModels"));

        try
        {
            await File.WriteAllTextAsync(Path.Combine(workspaceRoot, "Sample.sln"), string.Empty);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Sample.Presentation.Wpf.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF></PropertyGroup></Project>");
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ViewModels", "ShellViewModel.cs"),
                "namespace Sample.Presentation.Wpf.ViewModels; public sealed partial class ShellViewModel { public string Name { get; } = \"A\"; }");
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "ViewModels", "ShellViewModel.Partial.cs"),
                "namespace Sample.Presentation.Wpf.ViewModels; public sealed partial class ShellViewModel { private void OpenSettings() { } }");

            var scanner = new WpfNet8WorkspaceScanner(new WorkspaceFileSystem());
            var resolver = new ScanBackedEntryResolver();

            var scanResult = await scanner.ScanAsync(workspaceRoot);
            var resolved = await resolver.ResolveAsync(new Entry(EntryKind.Symbol, "Sample.Presentation.Wpf.ViewModels.ShellViewModel"), scanResult);

            Assert.Single(scanResult.Symbols, symbol => symbol.QualifiedName == "Sample.Presentation.Wpf.ViewModels.ShellViewModel");
            Assert.Equal(ResolvedEntryKind.Symbol, resolved.ResolvedKind);
            Assert.Equal(
                "src/Sample.Presentation.Wpf/ViewModels/ShellViewModel.cs",
                resolved.ResolvedPath,
                ignoreCase: true);
        }
        finally
        {
            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ScanAsync_DistinguishesShowAndShowDialogPerInvocation()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"rulixa-scan-dialog-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Services"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views"));

        try
        {
            await File.WriteAllTextAsync(Path.Combine(workspaceRoot, "Sample.sln"), string.Empty);
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Sample.Presentation.Wpf.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "App.xaml"), "<Application />");
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views", "OwnedWindow.xaml"),
                "<Window x:Class=\"Sample.Presentation.Wpf.Views.OwnedWindow\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" />");
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Views", "LooseWindow.xaml"),
                "<Window x:Class=\"Sample.Presentation.Wpf.Views.LooseWindow\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" />");
            await File.WriteAllTextAsync(
                Path.Combine(workspaceRoot, "src", "Sample.Presentation.Wpf", "Services", "WindowService.cs"),
                """
                using Sample.Presentation.Wpf.Views;

                namespace Sample.Presentation.Wpf.Services;

                public sealed class WindowService
                {
                    public void ShowOwned()
                    {
                        var ownedWindow = new OwnedWindow();
                        ownedWindow.Owner = App.Current.MainWindow;
                        ownedWindow.ShowDialog();
                    }

                    public void ShowLoose()
                    {
                        var looseWindow = new LooseWindow();
                        looseWindow.Show();
                    }
                }
                """);

            var scanner = new WpfNet8WorkspaceScanner(new WorkspaceFileSystem());

            var result = await scanner.ScanAsync(workspaceRoot);

            Assert.Contains(result.WindowActivations, activation =>
                activation.CallerSymbol == "Sample.Presentation.Wpf.Services.WindowService.ShowOwned"
                && activation.WindowSymbol == "Sample.Presentation.Wpf.Views.OwnedWindow"
                && activation.ActivationKind == "show-dialog"
                && activation.OwnerKind == "main-window");
            Assert.Contains(result.WindowActivations, activation =>
                activation.CallerSymbol == "Sample.Presentation.Wpf.Services.WindowService.ShowLoose"
                && activation.WindowSymbol == "Sample.Presentation.Wpf.Views.LooseWindow"
                && activation.ActivationKind == "show"
                && activation.OwnerKind == "none");
        }
        finally
        {
            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExtractAsync_ForShellViewModelSymbol_IsDeterministic()
    {
        var first = await BuildPackAsync(
            new Entry(EntryKind.Symbol, "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel"),
            Budget.Default,
            PromoteShellViewModelToLargeFile);
        var second = await BuildPackAsync(
            new Entry(EntryKind.Symbol, "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel"),
            Budget.Default,
            PromoteShellViewModelToLargeFile);

        Assert.Equal(JsonSerializer.Serialize(first.ScanResult), JsonSerializer.Serialize(second.ScanResult));
        Assert.Equal(JsonSerializer.Serialize(first.Ingredients), JsonSerializer.Serialize(second.Ingredients));
        Assert.Equal(JsonSerializer.Serialize(first.Pack), JsonSerializer.Serialize(second.Pack));
    }

    private static async Task<(WorkspaceScanResult ScanResult, PackIngredients Ingredients, ContextPack Pack)> BuildPackAsync(
        Entry entry,
        Budget budget,
        Func<WorkspaceScanResult, WorkspaceScanResult>? transform = null,
        string goal = "Add a new page to the shell.")
    {
        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);

        var scanResult = await scanner.ScanAsync(FixtureRoot);
        if (transform is not null)
        {
            scanResult = transform(scanResult);
        }

        var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);
        var ingredients = await extractor.ExtractAsync(FixtureRoot, scanResult, resolvedEntry, goal);
        var pack = ContextPackFactory.Create(
            goal,
            entry,
            resolvedEntry,
            ingredients,
            scanResult,
            budget);
        return (scanResult, ingredients, pack);
    }

    private static WorkspaceScanResult PromoteShellViewModelToLargeFile(WorkspaceScanResult scanResult) =>
        scanResult with
        {
            Files = scanResult.Files
                .Select(file => file.Path == "src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs"
                    ? file with { LineCount = 300 }
                    : file)
                .ToArray()
        };
}
