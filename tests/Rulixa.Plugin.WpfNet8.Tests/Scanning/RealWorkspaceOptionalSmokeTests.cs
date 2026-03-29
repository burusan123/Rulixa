using Rulixa.Application.UseCases;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Infrastructure.FileSystem;
using Rulixa.Infrastructure.Rendering;
using Rulixa.Infrastructure.Resolution;
using Rulixa.Plugin.WpfNet8.Extraction;
using Rulixa.Plugin.WpfNet8.Scanning;

namespace Rulixa.Plugin.WpfNet8.Tests.Scanning;

public sealed class RealWorkspaceOptionalSmokeTests
{
    internal const string EnableEnvironmentVariableName = "RULIXA_RUN_OPTIONAL_SMOKE";
    internal const string WorkspaceRootEnvironmentVariableName = "RULIXA_OPTIONAL_SMOKE_WORKSPACE";
    internal const string RootSymbolEnvironmentVariableName = "RULIXA_OPTIONAL_SMOKE_ROOT_SYMBOL";
    internal const string SecondarySymbolEnvironmentVariableName = "RULIXA_OPTIONAL_SMOKE_SECONDARY_SYMBOL";

    [OptionalRealWorkspaceFact]
    public async Task BuildPack_ForConfiguredRootSymbol_ContainsSystemSignals()
    {
        var workspaceRoot = GetRequiredEnvironmentVariable(WorkspaceRootEnvironmentVariableName);
        var rootSymbol = GetRequiredEnvironmentVariable(RootSymbolEnvironmentVariableName);

        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);
        var renderer = new MarkdownContextPackRenderer();

        var scanUseCase = new ScanWorkspaceUseCase(scanner);
        var resolveUseCase = new ResolveEntryUseCase(resolver);
        var buildPackUseCase = new BuildContextPackUseCase(extractor);

        var entry = new Entry(EntryKind.Symbol, rootSymbol);
        var scanResult = await scanUseCase.ExecuteAsync(workspaceRoot);
        var resolvedEntry = await resolveUseCase.ExecuteAsync(entry, scanResult);
        var pack = await buildPackUseCase.ExecuteAsync(
            workspaceRoot,
            scanResult,
            entry,
            resolvedEntry,
            "project",
            Budget.Default);
        var markdown = renderer.Render(pack);

        Assert.Equal(ResolvedEntryKind.Symbol, resolvedEntry.ResolvedKind);
        Assert.Equal(rootSymbol, resolvedEntry.Symbol);
        Assert.Contains(pack.Contracts, contract =>
            contract.Kind == ContractKind.Navigation
            && contract.Summary.Contains("SelectedItem", StringComparison.Ordinal)
            && contract.Summary.Contains("CurrentPage", StringComparison.Ordinal));
        Assert.Contains(pack.Contracts, contract => contract.Kind == ContractKind.DependencyInjection);
        Assert.Contains(pack.Contracts, contract =>
            contract.Title == "System Pack"
            && contract.Summary.Contains("Drafting", StringComparison.Ordinal)
            && contract.Summary.Contains("Settings", StringComparison.Ordinal)
            && contract.Summary.Contains("Report/Export", StringComparison.Ordinal)
            && contract.Summary.Contains("Architecture", StringComparison.Ordinal));
        Assert.Contains(pack.Contracts, contract => contract.Kind == ContractKind.DialogActivation);
        Assert.Contains(pack.SelectedSnippets, snippet =>
            snippet.Reason is "root-binding-source" or "dependency-injection" or "navigation-update");
        Assert.Contains("SelectedItem", markdown, StringComparison.Ordinal);
        Assert.Contains("CurrentPage", markdown, StringComparison.Ordinal);
        Assert.Contains("project", markdown, StringComparison.OrdinalIgnoreCase);
        Assert.InRange(RealWorkspaceSmokeAssertions.GetIndexLineCount(pack, "Persistence"), 1, 6);
        Assert.InRange(RealWorkspaceSmokeAssertions.GetIndexLineCount(pack, "Hub Objects"), 1, 3);
        Assert.Contains(
            pack.Indexes.Where(index => index.Title == "Hub Objects").SelectMany(static index => index.Lines),
            line => line.Contains("ProjectDocument", StringComparison.Ordinal));
    }

    [OptionalRealWorkspaceFact]
    public async Task BuildPack_ForConfiguredSecondarySymbol_ContainsWorkflowOrUnknownSignals()
    {
        var workspaceRoot = GetRequiredEnvironmentVariable(WorkspaceRootEnvironmentVariableName);
        var secondarySymbol = GetRequiredEnvironmentVariable(SecondarySymbolEnvironmentVariableName);

        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);

        var scanResult = await scanner.ScanAsync(workspaceRoot);
        var resolvedEntry = await resolver.ResolveAsync(new Entry(EntryKind.Symbol, secondarySymbol), scanResult);
        var ingredients = await extractor.ExtractAsync(
            workspaceRoot,
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
        Assert.InRange(RealWorkspaceSmokeAssertions.GetIndexLineCount(ingredients, "Workflow"), 0, 6);
        Assert.InRange(RealWorkspaceSmokeAssertions.GetIndexLineCount(ingredients, "Persistence"), 0, 6);
        Assert.InRange(RealWorkspaceSmokeAssertions.GetIndexLineCount(ingredients, "Architecture Tests"), 0, 4);
        Assert.DoesNotContain(
            ingredients.Indexes.Where(index => index.Title is "Workflow" or "Persistence").SelectMany(static index => index.Lines),
            line => line.Contains("IFileDialogService", StringComparison.Ordinal)
                || line.Contains("IUserPromptService", StringComparison.Ordinal)
                || line.Contains("FloorSettingsWindow", StringComparison.Ordinal)
                || line.Contains("IDraftingOverlayRenderer", StringComparison.Ordinal));
    }

    internal static string? GetConfiguredWorkspaceRoot() =>
        Environment.GetEnvironmentVariable(WorkspaceRootEnvironmentVariableName);

    internal static string? GetConfiguredRootSymbol() =>
        Environment.GetEnvironmentVariable(RootSymbolEnvironmentVariableName);

    internal static string? GetConfiguredSecondarySymbol() =>
        Environment.GetEnvironmentVariable(SecondarySymbolEnvironmentVariableName);

    private static string GetRequiredEnvironmentVariable(string variableName) =>
        Environment.GetEnvironmentVariable(variableName)
        ?? throw new InvalidOperationException($"Environment variable '{variableName}' is required.");
}

public sealed class OptionalRealWorkspaceFactAttribute : FactAttribute
{
    public OptionalRealWorkspaceFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Optional smoke test runs only on Windows.";
            return;
        }

        var flag = Environment.GetEnvironmentVariable(RealWorkspaceOptionalSmokeTests.EnableEnvironmentVariableName);
        if (!string.Equals(flag, "1", StringComparison.Ordinal))
        {
            Skip = $"{RealWorkspaceOptionalSmokeTests.EnableEnvironmentVariableName}=1 is required to run the optional smoke test.";
            return;
        }

        var workspaceRoot = RealWorkspaceOptionalSmokeTests.GetConfiguredWorkspaceRoot();
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            Skip = $"{RealWorkspaceOptionalSmokeTests.WorkspaceRootEnvironmentVariableName} is required.";
            return;
        }

        if (!Directory.Exists(workspaceRoot))
        {
            Skip = $"Configured workspace was not found: {workspaceRoot}";
            return;
        }

        if (string.IsNullOrWhiteSpace(RealWorkspaceOptionalSmokeTests.GetConfiguredRootSymbol()))
        {
            Skip = $"{RealWorkspaceOptionalSmokeTests.RootSymbolEnvironmentVariableName} is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RealWorkspaceOptionalSmokeTests.GetConfiguredSecondarySymbol()))
        {
            Skip = $"{RealWorkspaceOptionalSmokeTests.SecondarySymbolEnvironmentVariableName} is required.";
        }
    }
}

file static class RealWorkspaceSmokeAssertions
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
