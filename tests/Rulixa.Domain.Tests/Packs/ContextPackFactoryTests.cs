using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Domain.Tests.Packs;

public sealed class ContextPackFactoryTests
{
    [Fact]
    public void Create_RequiredFilesSurviveBudgetReduction()
    {
        var scanResult = CreateScanResult(
            new ScanFile("Views/ShellView.xaml", ScanFileKind.Xaml, "App", "hash-1", 400, ["view"]),
            new ScanFile("ViewModels/ShellViewModel.cs", ScanFileKind.ViewModel, "App", "hash-2", 300, ["viewmodel"]),
            new ScanFile("Services/SettingWindowService.cs", ScanFileKind.Service, "App", "hash-3", 500, ["service"]));
        var ingredients = new PackIngredients(
            Contracts: [],
            Indexes: [],
            SnippetCandidates: [],
            FileCandidates:
            [
                new FileSelectionCandidate("Views/ShellView.xaml", "entry", 0, true),
                new FileSelectionCandidate("ViewModels/ShellViewModel.cs", "viewmodel", 1, true),
                new FileSelectionCandidate("Services/SettingWindowService.cs", "optional", 10, false)
            ],
            DecisionTraces: [],
            Unknowns: []);

        var pack = CreatePack(scanResult, ingredients, new Budget(MaxFiles: 2, MaxTotalLines: 450, MaxSnippetsPerFile: 3));

        Assert.Collection(
            pack.SelectedFiles,
            file => Assert.Equal("Views/ShellView.xaml", file.Path),
            file => Assert.Equal("ViewModels/ShellViewModel.cs", file.Path));
        Assert.Empty(pack.SelectedSnippets);
    }

    [Fact]
    public void Create_PrefersLowPriorityOptionalFilesWhenBudgetIsTight()
    {
        var scanResult = CreateScanResult(
            new ScanFile("Views/ShellView.xaml", ScanFileKind.Xaml, "App", "hash-1", 100, ["view"]),
            new ScanFile("Views/ShellView.xaml.cs", ScanFileKind.CodeBehind, "App", "hash-2", 60, ["view"]),
            new ScanFile("ViewModels/ShellViewModel.cs", ScanFileKind.ViewModel, "App", "hash-3", 120, ["viewmodel"]),
            new ScanFile("ViewModels/Pages/FloorPageViewModel.cs", ScanFileKind.ViewModel, "App", "hash-4", 220, ["viewmodel"]));
        var ingredients = new PackIngredients(
            Contracts: [],
            Indexes: [],
            SnippetCandidates: [],
            FileCandidates:
            [
                new FileSelectionCandidate("Views/ShellView.xaml", "primary-view", 5, false),
                new FileSelectionCandidate("Views/ShellView.xaml.cs", "primary-code-behind", 10, false),
                new FileSelectionCandidate("ViewModels/ShellViewModel.cs", "primary-viewmodel", 5, false),
                new FileSelectionCandidate("ViewModels/Pages/FloorPageViewModel.cs", "data-template", 40, false)
            ],
            DecisionTraces: [],
            Unknowns: []);

        var pack = CreatePack(scanResult, ingredients, new Budget(MaxFiles: 3, MaxTotalLines: 300, MaxSnippetsPerFile: 3));

        Assert.DoesNotContain(pack.SelectedFiles, file => file.Path == "ViewModels/Pages/FloorPageViewModel.cs");
        Assert.Contains(pack.SelectedFiles, file => file.Path == "ViewModels/ShellViewModel.cs");
    }

    [Fact]
    public void Create_WhenLargeCSharpFileHasSnippets_ReplacesFullFile()
    {
        var scanResult = CreateScanResult(
            new ScanFile("Views/ShellView.xaml", ScanFileKind.Xaml, "App", "hash-1", 42, ["view"]),
            new ScanFile("ViewModels/ShellViewModel.cs", ScanFileKind.ViewModel, "App", "hash-2", 4044, ["viewmodel"]));
        var ingredients = new PackIngredients(
            Contracts: [],
            Indexes: [],
            SnippetCandidates:
            [
                new SnippetSelectionCandidate("ViewModels/ShellViewModel.cs", "dependency-injection", 0, true, "ShellViewModel(...)", 20, 40, "constructor"),
                new SnippetSelectionCandidate("ViewModels/ShellViewModel.cs", "navigation-update", -1, true, "Select(...)", 120, 150, "select")
            ],
            FileCandidates:
            [
                new FileSelectionCandidate("Views/ShellView.xaml", "entry", 0, true),
                new FileSelectionCandidate("ViewModels/ShellViewModel.cs", "navigation-update", -1, true)
            ],
            DecisionTraces: [],
            Unknowns: []);

        var pack = CreatePack(scanResult, ingredients, Budget.Default);

        Assert.DoesNotContain(pack.SelectedFiles, file => file.Path == "ViewModels/ShellViewModel.cs");
        Assert.Contains(pack.SelectedFiles, file => file.Path == "Views/ShellView.xaml");
        Assert.Collection(
            pack.SelectedSnippets.OrderBy(static snippet => snippet.StartLine),
            snippet => Assert.Equal("ShellViewModel(...)", snippet.Anchor),
            snippet => Assert.Equal("Select(...)", snippet.Anchor));
    }

    [Fact]
    public void Create_MergesAdjacentSnippetsAndHonorsPerFileLimit()
    {
        var scanResult = CreateScanResult(
            new ScanFile("ViewModels/ShellViewModel.cs", ScanFileKind.ViewModel, "App", "hash-1", 4044, ["viewmodel"]));
        var ingredients = new PackIngredients(
            Contracts: [],
            Indexes: [],
            SnippetCandidates:
            [
                new SnippetSelectionCandidate("ViewModels/ShellViewModel.cs", "navigation-update", 0, true, "RestoreSelection(...)", 100, 120, "restore"),
                new SnippetSelectionCandidate("ViewModels/ShellViewModel.cs", "navigation-update", 1, true, "Select(...)", 123, 150, "select"),
                new SnippetSelectionCandidate("ViewModels/ShellViewModel.cs", "command-viewmodel", 2, false, "OpenSettings(...)", 220, 240, "open"),
                new SnippetSelectionCandidate("ViewModels/ShellViewModel.cs", "command-viewmodel", 3, false, "ImportSettings(...)", 260, 280, "import")
            ],
            FileCandidates:
            [
                new FileSelectionCandidate("ViewModels/ShellViewModel.cs", "navigation-update", -1, true)
            ],
            DecisionTraces: [],
            Unknowns: []);

        var pack = CreatePack(scanResult, ingredients, new Budget(MaxFiles: 8, MaxTotalLines: 1600, MaxSnippetsPerFile: 2));

        Assert.Equal(2, pack.SelectedSnippets.Count);
        Assert.Equal(100, pack.SelectedSnippets[0].StartLine);
        Assert.Equal(150, pack.SelectedSnippets[0].EndLine);
        Assert.Contains("RestoreSelection(...)", pack.SelectedSnippets[0].Anchor);
        Assert.Contains("Select(...)", pack.SelectedSnippets[0].Anchor);
        Assert.DoesNotContain(pack.SelectedSnippets, snippet => snippet.Anchor.Contains("ImportSettings", StringComparison.Ordinal));
    }

    private static WorkspaceScanResult CreateScanResult(params ScanFile[] files) =>
        new(
            "phase1.v1",
            "D:/workspace",
            DateTimeOffset.UtcNow,
            new ProjectSummary([], [], ["net8.0-windows"], true, [], []),
            files,
            [],
            [],
            [],
            [],
            [],
            [],
            []);

    private static ContextPack CreatePack(
        WorkspaceScanResult scanResult,
        PackIngredients ingredients,
        Budget budget) =>
        ContextPackFactory.Create(
            "Add a new page to the shell.",
            new Entry(EntryKind.File, "Views/ShellView.xaml"),
            new ResolvedEntry("file:Views/ShellView.xaml", ResolvedEntryKind.File, "Views/ShellView.xaml", null, ConfidenceLevel.High, []),
            ingredients,
            scanResult,
            budget);
}
