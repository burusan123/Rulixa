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
            && binding.ViewModelSymbol.EndsWith(".ShellViewModel", StringComparison.Ordinal));
        Assert.Contains(result.ViewModelBindings, binding =>
            binding.ViewPath.EndsWith("Views/ShellView.xaml", StringComparison.Ordinal)
            && binding.BindingKind == ViewModelBindingKind.DataTemplate);
        Assert.Contains(result.NavigationTransitions, transition =>
            transition.ViewModelSymbol.EndsWith(".ShellViewModel", StringComparison.Ordinal)
            && transition.UpdateMethodName == "Select"
            && transition.UpdateExpressionSummary == "CurrentPage = item.PageViewModel");
        Assert.Contains(result.NavigationTransitions, transition =>
            transition.UpdateExpressionSummary == "SelectedItem = match"
            && transition.StartLine > 0);
        Assert.Contains(result.Commands, command => command.PropertyName == "OpenSettingsCommand");
        Assert.Contains(result.ServiceRegistrations, registration => registration.ServiceType == "ShellViewModel");
        Assert.Contains(result.WindowActivations, activation => activation.WindowSymbol == "SettingWindow");
    }

    [Fact]
    public async Task ExtractAsync_ForShellViewFile_SelectsCoreFiles()
    {
        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);

        var scanResult = await scanner.ScanAsync(FixtureRoot);
        var entry = new Entry(EntryKind.File, "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml");
        var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);
        var ingredients = await extractor.ExtractAsync(FixtureRoot, scanResult, resolvedEntry);
        var pack = ContextPackFactory.Create(
            "Add a new page to the shell.",
            entry,
            resolvedEntry,
            ingredients,
            scanResult,
            new Budget(MaxFiles: 10, MaxTotalLines: 5000, MaxSnippetsPerFile: 3));

        var selectedPaths = pack.SelectedFiles.Select(static file => file.Path).ToArray();

        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/App.xaml.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/ServiceRegistration.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Common/DelegateCommand.cs", selectedPaths);
    }

    [Fact]
    public async Task ExtractAsync_ForShellViewModelSymbol_ExcludesDataTemplateViewModelsFromDefaultBudget()
    {
        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);

        var scanResult = await scanner.ScanAsync(FixtureRoot);
        var entry = new Entry(EntryKind.Symbol, "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel");
        var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);
        var ingredients = await extractor.ExtractAsync(FixtureRoot, scanResult, resolvedEntry);
        var pack = ContextPackFactory.Create(
            "Add a new page to the shell.",
            entry,
            resolvedEntry,
            ingredients,
            scanResult,
            Budget.Default);

        var selectedFiles = pack.SelectedFiles.ToArray();
        var selectedPaths = selectedFiles.Select(static file => file.Path).ToArray();

        Assert.Contains("src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/App.xaml.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/ServiceRegistration.cs", selectedPaths);
        Assert.Contains("src/AssessMeister.Presentation.Wpf/Common/DelegateCommand.cs", selectedPaths);
        Assert.DoesNotContain(selectedPaths, path => path.Contains("/Pages/", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(selectedFiles, file =>
            file.Path == "src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs"
            && file.Reason == "navigation-update");
        Assert.True(pack.SelectedFiles.Count <= 8);
    }

    [Fact]
    public async Task ExtractAsync_ForShellViewModelSymbol_AddsNavigationContractAndUpdateIndex()
    {
        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);

        var scanResult = await scanner.ScanAsync(FixtureRoot);
        var entry = new Entry(EntryKind.Symbol, "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel");
        var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);
        var ingredients = await extractor.ExtractAsync(FixtureRoot, scanResult, resolvedEntry);

        Assert.Contains(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.Navigation
            && contract.Summary.Contains("SelectedItem", StringComparison.Ordinal)
            && contract.Summary.Contains("CurrentPage", StringComparison.Ordinal));
        Assert.Contains(ingredients.Contracts, contract =>
            contract.Kind == ContractKind.Navigation
            && contract.Summary.Contains(".Select(...)", StringComparison.Ordinal)
            && contract.Summary.Contains("CurrentPage = item.PageViewModel", StringComparison.Ordinal));
        Assert.Contains(ingredients.Indexes, index =>
            index.Title == "ナビゲーション更新点"
            && index.Lines.Any(line =>
                line.Contains("Select(...)", StringComparison.Ordinal)
                && line.Contains("CurrentPage = item.PageViewModel", StringComparison.Ordinal)));
        Assert.Contains(ingredients.Indexes, index =>
            index.Title == "ナビゲーション更新点"
            && index.Lines.Any(line =>
                line.Contains("SelectedItem = match", StringComparison.Ordinal)
                && line.Contains("line:", StringComparison.Ordinal)));
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
}
