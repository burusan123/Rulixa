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
    private const string DraftingWindowViewModelSymbol = "AssessMeister.Presentation.Wpf.ViewModels.Drafting.DraftingWindowViewModel";

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
            "project",
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
            contract.Title == "System Pack"
            && contract.Summary.Contains("Drafting", StringComparison.Ordinal)
            && contract.Summary.Contains("Architecture", StringComparison.Ordinal));
        Assert.Contains(pack.Contracts, contract =>
            contract.Kind == ContractKind.DialogActivation);
        Assert.Contains(pack.Contracts, contract =>
            contract.Kind == ContractKind.Command
            && (contract.Summary.Contains("LoadPagesFromProjectDocument(...)", StringComparison.Ordinal)
                || contract.Summary.Contains("ApplyInsulationDefaultsAfterDraftingOk(...)", StringComparison.Ordinal)));
        Assert.Contains(pack.SelectedSnippets, snippet =>
            snippet.Reason is "root-binding-source" or "dependency-injection" or "navigation-update");
        Assert.Contains(pack.SelectedSnippets, snippet =>
            snippet.Reason == "command-impact"
            || snippet.Anchor.Contains("LoadPagesFromProjectDocument(...)", StringComparison.Ordinal)
            || snippet.Anchor.Contains("ApplyInsulationDefaultsAfterDraftingOk(...)", StringComparison.Ordinal));
        Assert.Contains("SelectedItem", markdown, StringComparison.Ordinal);
        Assert.Contains("CurrentPage", markdown, StringComparison.Ordinal);
        Assert.Contains("project", markdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("システム地図", markdown, StringComparison.Ordinal);
        Assert.InRange(AssessMeisterSmokeAssertions.GetIndexLineCount(pack, "Persistence"), 1, 6);
        Assert.InRange(AssessMeisterSmokeAssertions.GetIndexLineCount(pack, "Hub Objects"), 1, 3);
        Assert.Contains(pack.Indexes.Where(index => index.Title == "Hub Objects").SelectMany(static index => index.Lines),
            line => line.Contains("ProjectDocument", StringComparison.Ordinal));
    }

    [OptionalAssessMeisterFact]
    public async Task BuildPack_ForAssessMeisterDraftingViewModel_ContainsWorkflowOrUnknownSignals()
    {
        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);

        var scanResult = await scanner.ScanAsync(WorkspaceRoot);
        var resolvedEntry = await resolver.ResolveAsync(new Entry(EntryKind.Symbol, DraftingWindowViewModelSymbol), scanResult);
        var ingredients = await extractor.ExtractAsync(
            WorkspaceRoot,
            scanResult,
            resolvedEntry,
            "drafting ai analyze");

        Assert.Equal(ResolvedEntryKind.Symbol, resolvedEntry.ResolvedKind);
        Assert.True(
            ingredients.Indexes.Any(index => index.Title == "Workflow")
            || ingredients.Unknowns.Any(unknown => unknown.Code.StartsWith("workflow.", StringComparison.Ordinal))
            || ingredients.Indexes.SelectMany(static index => index.Lines).Any(line =>
                line.Contains("WallAlgorithm", StringComparison.Ordinal)
                || line.Contains("Algorithm", StringComparison.Ordinal)));
        Assert.True(
            ingredients.Indexes.Where(index => index.Title == "Workflow").SelectMany(static index => index.Lines).Any(line =>
                line.Contains("Algorithm", StringComparison.Ordinal)
                || line.Contains("Analyzer", StringComparison.Ordinal))
            || ingredients.Unknowns.Any(unknown =>
                unknown.Code == "workflow.missing-downstream"
                && unknown.Candidates.Any(candidate =>
                    candidate.Contains("Algorithm", StringComparison.Ordinal)
                    || candidate.Contains("Analyzer", StringComparison.Ordinal))));
        Assert.InRange(AssessMeisterSmokeAssertions.GetIndexLineCount(ingredients, "Workflow"), 0, 6);
        Assert.InRange(AssessMeisterSmokeAssertions.GetIndexLineCount(ingredients, "Persistence"), 0, 6);
        Assert.InRange(AssessMeisterSmokeAssertions.GetIndexLineCount(ingredients, "Architecture Tests"), 0, 4);
        Assert.DoesNotContain(
            ingredients.Indexes.Where(index => index.Title is "Workflow" or "Persistence").SelectMany(static index => index.Lines),
            line => line.Contains("IFileDialogService", StringComparison.Ordinal)
                || line.Contains("IUserPromptService", StringComparison.Ordinal)
                || line.Contains("FloorSettingsWindow", StringComparison.Ordinal)
                || line.Contains("IDraftingOverlayRenderer", StringComparison.Ordinal));
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

file static class AssessMeisterSmokeAssertions
{
    internal static int GetIndexLineCount(ContextPack contextPack, string title) =>
        contextPack.Indexes
            .Where(index => index.Title == title)
            .SelectMany(static index => index.Lines)
            .Count();

    internal static int GetIndexLineCount(PackIngredients ingredients, string title) =>
        ingredients.Indexes
            .Where(index => index.Title == title)
            .SelectMany(static index => index.Lines)
            .Count();
}
