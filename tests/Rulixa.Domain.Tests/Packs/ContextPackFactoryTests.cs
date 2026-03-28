using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Domain.Tests.Packs;

public sealed class ContextPackFactoryTests
{
    [Fact]
    public void Create_RequiredFilesSurviveBudgetReduction()
    {
        var scanResult = new WorkspaceScanResult(
            "phase1.v1",
            "D:/workspace",
            DateTimeOffset.UtcNow,
            new ProjectSummary([], [], ["net8.0-windows"], true, [], []),
            [
                new ScanFile("Views/ShellView.xaml", ScanFileKind.Xaml, "App", "hash-1", 400, ["view"]),
                new ScanFile("ViewModels/ShellViewModel.cs", ScanFileKind.ViewModel, "App", "hash-2", 300, ["viewmodel"]),
                new ScanFile("Services/SettingWindowService.cs", ScanFileKind.Service, "App", "hash-3", 500, ["service"])
            ],
            [],
            [],
            [],
            [],
            [],
            [],
            []);

        var ingredients = new PackIngredients(
            Contracts: [],
            Indexes: [],
            FileCandidates:
            [
                new FileSelectionCandidate("Views/ShellView.xaml", "entry", 0, true),
                new FileSelectionCandidate("ViewModels/ShellViewModel.cs", "viewmodel", 1, true),
                new FileSelectionCandidate("Services/SettingWindowService.cs", "optional", 10, false)
            ],
            Unknowns: []);

        var pack = ContextPackFactory.Create(
            "Add a new page to the shell.",
            new Entry(EntryKind.File, "Views/ShellView.xaml"),
            new ResolvedEntry("file:Views/ShellView.xaml", ResolvedEntryKind.File, "Views/ShellView.xaml", null, ConfidenceLevel.High, []),
            ingredients,
            scanResult,
            new Budget(MaxFiles: 2, MaxTotalLines: 450, MaxSnippetsPerFile: 3));

        Assert.Collection(
            pack.SelectedFiles,
            file => Assert.Equal("Views/ShellView.xaml", file.Path),
            file => Assert.Equal("ViewModels/ShellViewModel.cs", file.Path));
    }

    [Fact]
    public void Create_PrefersLowPriorityOptionalFilesWhenBudgetIsTight()
    {
        var scanResult = new WorkspaceScanResult(
            "phase1.v1",
            "D:/workspace",
            DateTimeOffset.UtcNow,
            new ProjectSummary([], [], ["net8.0-windows"], true, [], []),
            [
                new ScanFile("Views/ShellView.xaml", ScanFileKind.Xaml, "App", "hash-1", 100, ["view"]),
                new ScanFile("Views/ShellView.xaml.cs", ScanFileKind.CodeBehind, "App", "hash-2", 60, ["view"]),
                new ScanFile("ViewModels/ShellViewModel.cs", ScanFileKind.ViewModel, "App", "hash-3", 120, ["viewmodel"]),
                new ScanFile("ViewModels/Pages/FloorPageViewModel.cs", ScanFileKind.ViewModel, "App", "hash-4", 220, ["viewmodel"])
            ],
            [],
            [],
            [],
            [],
            [],
            [],
            []);

        var ingredients = new PackIngredients(
            Contracts: [],
            Indexes: [],
            FileCandidates:
            [
                new FileSelectionCandidate("Views/ShellView.xaml", "primary-view", 5, false),
                new FileSelectionCandidate("Views/ShellView.xaml.cs", "primary-code-behind", 10, false),
                new FileSelectionCandidate("ViewModels/ShellViewModel.cs", "primary-viewmodel", 5, false),
                new FileSelectionCandidate("ViewModels/Pages/FloorPageViewModel.cs", "data-template", 40, false)
            ],
            Unknowns: []);

        var pack = ContextPackFactory.Create(
            "Build a shell pack from symbol entry.",
            new Entry(EntryKind.Symbol, "App.ShellViewModel"),
            new ResolvedEntry("symbol:App.ShellViewModel", ResolvedEntryKind.Symbol, "ViewModels/ShellViewModel.cs", "App.ShellViewModel", ConfidenceLevel.High, []),
            ingredients,
            scanResult,
            new Budget(MaxFiles: 3, MaxTotalLines: 300, MaxSnippetsPerFile: 3));

        Assert.DoesNotContain(pack.SelectedFiles, file => file.Path == "ViewModels/Pages/FloorPageViewModel.cs");
        Assert.Contains(pack.SelectedFiles, file => file.Path == "ViewModels/ShellViewModel.cs");
    }
}
