using Rulixa.Application.UseCases;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;
using Rulixa.Infrastructure.FileSystem;
using Rulixa.Infrastructure.Rendering;
using Rulixa.Infrastructure.Resolution;
using Rulixa.Plugin.WpfNet8.Extraction;
using Rulixa.Plugin.WpfNet8.Scanning;

namespace Rulixa.Plugin.WpfNet8.Tests.Scanning;

public sealed class LegacyRealWorkspaceOptionalSmokeTests
{
    internal const string WorkspaceRootEnvironmentVariableName = "RULIXA_OPTIONAL_SMOKE_LEGACY_WORKSPACE";
    internal const string EntryPathEnvironmentVariableName = "RULIXA_OPTIONAL_SMOKE_LEGACY_ENTRY";

    [OptionalLegacyRealWorkspaceFact]
    public async Task BuildPack_ForConfiguredLegacyEntry_DoesNotCrashAndReturnsSignals()
    {
        var workspaceRoot = GetRequiredEnvironmentVariable(WorkspaceRootEnvironmentVariableName);
        var entryPath = GetRequiredEnvironmentVariable(EntryPathEnvironmentVariableName);

        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);
        var renderer = new MarkdownContextPackRenderer();
        var buildPackUseCase = new BuildContextPackUseCase(extractor);

        var entry = new Entry(EntryKind.File, entryPath);
        var scanResult = await scanner.ScanAsync(workspaceRoot);
        var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);
        var pack = await buildPackUseCase.ExecuteAsync(
            workspaceRoot,
            scanResult,
            entry,
            resolvedEntry,
            "legacy system",
            Budget.Default);
        var markdown = renderer.Render(pack);

        Assert.Equal(ResolvedEntryKind.File, resolvedEntry.ResolvedKind);
        Assert.Contains(scanResult.ViewModelBindings, binding =>
            binding.ViewPath == entryPath
            && binding.BindingKind == ViewModelBindingKind.RootDataContext);
        Assert.DoesNotContain(scanResult.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.True(
            pack.Contracts.Any(contract => contract.Title == "System Pack")
            || pack.Indexes.Any()
            || pack.Unknowns.Any());
        Assert.Contains(pack.Contracts, contract =>
            contract.Title == "System Pack"
            && contract.Summary.Contains("Settings", StringComparison.Ordinal)
            && contract.Summary.Contains("Report/Export", StringComparison.Ordinal));
        Assert.True(
            scanResult.WindowActivations.Any()
            || pack.Contracts.Any(contract => contract.Kind == ContractKind.DialogActivation)
            || pack.Unknowns.Any(unknown => unknown.Code.StartsWith("workflow.", StringComparison.Ordinal)));
        Assert.True(
            pack.Contracts.Any(contract => contract.Kind == ContractKind.DialogActivation)
            || pack.SelectedFiles.Any(file =>
                file.Path.Contains("Setting", StringComparison.OrdinalIgnoreCase)
                || file.Path.Contains("Report", StringComparison.OrdinalIgnoreCase))
            || pack.Unknowns.Any(unknown =>
                unknown.Code == "persistence.missing-owner"
                || unknown.Code.StartsWith("workflow.", StringComparison.Ordinal)));
        Assert.True(
            !pack.Unknowns.Any()
            || pack.Unknowns.Any(unknown =>
                unknown.Candidates.Count > 0
                && unknown.Candidates.All(candidate =>
                    !candidate.Contains("Overlay", StringComparison.OrdinalIgnoreCase)
                    && !candidate.Contains("Prompt", StringComparison.OrdinalIgnoreCase))));

        var entryFileName = Path.GetFileNameWithoutExtension(entryPath);
        if (!string.IsNullOrWhiteSpace(entryFileName))
        {
            Assert.Contains(entryFileName, markdown, StringComparison.Ordinal);
        }
    }

    internal static string? GetConfiguredWorkspaceRoot() =>
        Environment.GetEnvironmentVariable(WorkspaceRootEnvironmentVariableName);

    internal static string? GetConfiguredEntryPath() =>
        Environment.GetEnvironmentVariable(EntryPathEnvironmentVariableName);

    private static string GetRequiredEnvironmentVariable(string variableName) =>
        Environment.GetEnvironmentVariable(variableName)
        ?? throw new InvalidOperationException($"Environment variable '{variableName}' is required.");
}

public sealed class OptionalLegacyRealWorkspaceFactAttribute : FactAttribute
{
    public OptionalLegacyRealWorkspaceFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Optional legacy smoke test runs only on Windows.";
            return;
        }

        var flag = Environment.GetEnvironmentVariable(RealWorkspaceOptionalSmokeTests.EnableEnvironmentVariableName);
        if (!string.Equals(flag, "1", StringComparison.Ordinal))
        {
            Skip = $"{RealWorkspaceOptionalSmokeTests.EnableEnvironmentVariableName}=1 is required to run the optional smoke test.";
            return;
        }

        var workspaceRoot = LegacyRealWorkspaceOptionalSmokeTests.GetConfiguredWorkspaceRoot();
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            Skip = $"{LegacyRealWorkspaceOptionalSmokeTests.WorkspaceRootEnvironmentVariableName} is required.";
            return;
        }

        if (!Directory.Exists(workspaceRoot))
        {
            Skip = $"Configured legacy workspace was not found: {workspaceRoot}";
            return;
        }

        if (string.IsNullOrWhiteSpace(LegacyRealWorkspaceOptionalSmokeTests.GetConfiguredEntryPath()))
        {
            Skip = $"{LegacyRealWorkspaceOptionalSmokeTests.EntryPathEnvironmentVariableName} is required.";
        }
    }
}
