using Rulixa.Application.UseCases;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Infrastructure.FileSystem;
using Rulixa.Infrastructure.Rendering;
using Rulixa.Infrastructure.Resolution;
using Rulixa.Plugin.WpfNet8.Extraction;
using Rulixa.Plugin.WpfNet8.Scanning;

namespace Rulixa.Plugin.WpfNet8.Tests.Scanning;

public sealed class AssessMeisterOptionalSmokeTests
{
    internal const string WorkspaceRoot = @"D:\C#\AssessMeister";
    internal const string EnableEnvironmentVariableName = "RULIXA_RUN_ASSESSMEISTER_SMOKE";
    private const string ShellViewModelSymbol = "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel";

    [OptionalAssessMeisterFact]
    public async Task BuildPack_ForAssessMeisterShellViewModel_ContainsPhase1Signals()
    {
        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);
        var renderer = new MarkdownContextPackRenderer();

        var scanUseCase = new ScanWorkspaceUseCase(scanner);
        var resolveUseCase = new ResolveEntryUseCase(resolver);
        var buildPackUseCase = new BuildContextPackUseCase(extractor);

        var entry = new Entry(EntryKind.Symbol, ShellViewModelSymbol);
        var scanResult = await scanUseCase.ExecuteAsync(WorkspaceRoot);
        var resolvedEntry = await resolveUseCase.ExecuteAsync(entry, scanResult);
        var pack = await buildPackUseCase.ExecuteAsync(
            WorkspaceRoot,
            scanResult,
            entry,
            resolvedEntry,
            "\u8A2D\u5B9A\u753B\u9762\u3092\u958B\u304D\u305F\u3044",
            Budget.Default);
        var markdown = renderer.Render(pack);

        Assert.Equal(ResolvedEntryKind.Symbol, resolvedEntry.ResolvedKind);
        Assert.Equal(ShellViewModelSymbol, resolvedEntry.Symbol);
        Assert.Contains(pack.Contracts, contract =>
            contract.Kind == ContractKind.Navigation
            && contract.Summary.Contains("SelectedItem", StringComparison.Ordinal)
            && contract.Summary.Contains("CurrentPage", StringComparison.Ordinal));
        Assert.Contains(pack.Contracts, contract =>
            contract.Kind == ContractKind.DependencyInjection);
        Assert.Contains(pack.Contracts, contract =>
            contract.Kind == ContractKind.DialogActivation);
        Assert.Contains(pack.Contracts, contract =>
            contract.Kind == ContractKind.Command
            && contract.Summary.Contains("OpenSetting", StringComparison.Ordinal)
            && (contract.Summary.Contains("OpenSettingWindow", StringComparison.Ordinal)
                || contract.Summary.Contains("show-dialog", StringComparison.Ordinal)));
        Assert.Contains(pack.SelectedSnippets, snippet =>
            snippet.Reason is "root-binding-source" or "dependency-injection" or "navigation-update");
        Assert.Contains(pack.SelectedSnippets, snippet =>
            snippet.Reason == "command-impact" || snippet.Anchor.Contains("OpenSetting", StringComparison.Ordinal));
        Assert.Contains("SelectedItem", markdown, StringComparison.Ordinal);
        Assert.Contains("CurrentPage", markdown, StringComparison.Ordinal);
        Assert.Contains("show-dialog", markdown, StringComparison.Ordinal);
    }
}

public sealed class OptionalAssessMeisterFactAttribute : FactAttribute
{
    public OptionalAssessMeisterFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "AssessMeister smoke test runs only on Windows.";
            return;
        }

        if (!Directory.Exists(AssessMeisterOptionalSmokeTests.WorkspaceRoot))
        {
            Skip = $"AssessMeister workspace was not found: {AssessMeisterOptionalSmokeTests.WorkspaceRoot}";
            return;
        }

        var flag = Environment.GetEnvironmentVariable(AssessMeisterOptionalSmokeTests.EnableEnvironmentVariableName);
        if (!string.Equals(flag, "1", StringComparison.Ordinal))
        {
            Skip = $"{AssessMeisterOptionalSmokeTests.EnableEnvironmentVariableName}=1 is required to run the optional smoke test.";
        }
    }
}
