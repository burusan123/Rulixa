using Rulixa.Application.Ports;
using Rulixa.Application.UseCases;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Scanning;
using Rulixa.Infrastructure.Resolution;

namespace Rulixa.Application.Tests.UseCases;

public sealed class ResolveEntryUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_UsesResolverResult()
    {
        var expected = new ResolvedEntry(
            "file:Views/ShellView.xaml",
            ResolvedEntryKind.File,
            "Views/ShellView.xaml",
            null,
            ConfidenceLevel.High,
            []);

        var useCase = new ResolveEntryUseCase(new StubEntryResolver(expected));
        var scanResult = EmptyScanResult();

        var actual = await useCase.ExecuteAsync(new Entry(EntryKind.File, "Views/ShellView.xaml"), scanResult);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task ExecuteAsync_WithExactSymbol_ReturnsExactMatch()
    {
        var useCase = new ResolveEntryUseCase(new ScanBackedEntryResolver());
        var scanResult = new WorkspaceScanResult(
            "phase1.v1",
            "D:/workspace",
            DateTimeOffset.UtcNow,
            new ProjectSummary([], [], ["net8.0-windows"], true, [], []),
            [new ScanFile("ViewModels/ShellViewModel.cs", ScanFileKind.ViewModel, "App", "hash-1", 120, ["viewmodel"])],
            [new ScanSymbol("sym-0001", SymbolKind.Class, "App.ViewModels.ShellViewModel", "ShellViewModel", "ViewModels/ShellViewModel.cs", 1, 120, [])],
            [],
            [],
            [],
            [],
            []);

        var actual = await useCase.ExecuteAsync(
            new Entry(EntryKind.Symbol, "App.ViewModels.ShellViewModel"),
            scanResult);

        Assert.Equal(ResolvedEntryKind.Symbol, actual.ResolvedKind);
        Assert.Equal("ViewModels/ShellViewModel.cs", actual.ResolvedPath);
        Assert.Equal("App.ViewModels.ShellViewModel", actual.Symbol);
    }

    private static WorkspaceScanResult EmptyScanResult() =>
        new(
            "phase1.v1",
            "D:/workspace",
            DateTimeOffset.UtcNow,
            new ProjectSummary([], [], ["net8.0-windows"], true, [], []),
            [],
            [],
            [],
            [],
            [],
            [],
            []);

    private sealed class StubEntryResolver : IEntryResolver
    {
        private readonly ResolvedEntry resolvedEntry;

        public StubEntryResolver(ResolvedEntry resolvedEntry)
        {
            this.resolvedEntry = resolvedEntry;
        }

        public Task<ResolvedEntry> ResolveAsync(Entry entry, WorkspaceScanResult scanResult, CancellationToken cancellationToken = default) =>
            Task.FromResult(resolvedEntry);
    }
}
