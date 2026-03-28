using Rulixa.Application.Ports;
using Rulixa.Application.UseCases;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.Tests.UseCases;

public sealed class BuildContextPackUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_BuildsPackFromExtractorResult()
    {
        var scanResult = new WorkspaceScanResult(
            "phase1.v1",
            "D:/workspace",
            DateTimeOffset.UtcNow,
            new ProjectSummary([], [], ["net8.0-windows"], true, [], []),
            [
                new ScanFile("Views/ShellView.xaml", ScanFileKind.Xaml, "App", "hash-1", 120, ["view"]),
                new ScanFile("ViewModels/ShellViewModel.cs", ScanFileKind.ViewModel, "App", "hash-2", 220, ["viewmodel"])
            ],
            [],
            [],
            [],
            [],
            [],
            [],
            []);

        var ingredients = new PackIngredients(
            Contracts:
            [
                new Contract(
                    ContractKind.ViewModelBinding,
                    "View と ViewModel の対応",
                    "ShellView は ShellViewModel と対応します。",
                    ["Views/ShellView.xaml"],
                    ["App.ShellViewModel"])
            ],
            Indexes:
            [
                new IndexSection("View-ViewModel", ["Views/ShellView.xaml <-> App.ShellViewModel"])
            ],
            SnippetCandidates: [],
            FileCandidates:
            [
                new FileSelectionCandidate("Views/ShellView.xaml", "entry", 0, true),
                new FileSelectionCandidate("ViewModels/ShellViewModel.cs", "viewmodel", 1, true)
            ],
            Unknowns:
            [
                new Diagnostic("sample", "sample", null, DiagnosticSeverity.Info, [])
            ]);

        var useCase = new BuildContextPackUseCase(new StubContractExtractor(ingredients));
        var pack = await useCase.ExecuteAsync(
            "D:/workspace",
            scanResult,
            new Entry(EntryKind.File, "Views/ShellView.xaml"),
            new ResolvedEntry("file:Views/ShellView.xaml", ResolvedEntryKind.File, "Views/ShellView.xaml", null, ConfidenceLevel.High, []),
            "Shell 画面に新しいページを追加したい",
            Budget.Default);

        Assert.Equal("Shell 画面に新しいページを追加したい", pack.Goal);
        Assert.Empty(pack.SelectedSnippets);
        Assert.Equal(2, pack.SelectedFiles.Count);
        Assert.Single(pack.Contracts);
        Assert.Single(pack.Indexes);
        Assert.Single(pack.Unknowns);
    }

    private sealed class StubContractExtractor : IContractExtractor
    {
        private readonly PackIngredients ingredients;

        public StubContractExtractor(PackIngredients ingredients)
        {
            this.ingredients = ingredients;
        }

        public Task<PackIngredients> ExtractAsync(
            string workspaceRoot,
            WorkspaceScanResult scanResult,
            ResolvedEntry resolvedEntry,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ingredients);
    }
}
