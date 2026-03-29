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

public sealed class AssessMeisterLegacyOptionalSmokeTests
{
    internal const string WorkspaceRoot = @"D:\C#\AssessMeister_20260204";
    private const string EntryPath = "AssessMeister/Predict3DWindow.xaml";

    [OptionalLegacyAssessMeisterFact]
    public async Task BuildPack_ForLegacyAssessMeisterStartupWindow_DoesNotCrashAndReturnsSignals()
    {
        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);
        var renderer = new MarkdownContextPackRenderer();
        var buildPackUseCase = new BuildContextPackUseCase(extractor);

        var entry = new Entry(EntryKind.File, EntryPath);
        var scanResult = await scanner.ScanAsync(WorkspaceRoot);
        var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);
        var pack = await buildPackUseCase.ExecuteAsync(
            WorkspaceRoot,
            scanResult,
            entry,
            resolvedEntry,
            "legacy system",
            Budget.Default);
        var markdown = renderer.Render(pack);

        Assert.Equal(ResolvedEntryKind.File, resolvedEntry.ResolvedKind);
        Assert.Contains(scanResult.ViewModelBindings, binding =>
            binding.ViewPath == EntryPath
            && binding.BindingKind == ViewModelBindingKind.RootDataContext
            && binding.ViewModelSymbol.Contains("Predict3DWindow", StringComparison.Ordinal));
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
        Assert.Contains("Predict3DWindow", markdown, StringComparison.Ordinal);
    }
}

public sealed class OptionalLegacyAssessMeisterFactAttribute : FactAttribute
{
    public OptionalLegacyAssessMeisterFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Legacy AssessMeister smoke test runs only on Windows.";
            return;
        }

        if (!Directory.Exists(AssessMeisterLegacyOptionalSmokeTests.WorkspaceRoot))
        {
            Skip = $"Legacy AssessMeister workspace was not found: {AssessMeisterLegacyOptionalSmokeTests.WorkspaceRoot}";
            return;
        }

        var flag = Environment.GetEnvironmentVariable(AssessMeisterOptionalSmokeTests.EnableEnvironmentVariableName);
        if (!string.Equals(flag, "1", StringComparison.Ordinal))
        {
            Skip = $"{AssessMeisterOptionalSmokeTests.EnableEnvironmentVariableName}=1 is required to run the optional smoke test.";
        }
    }
}
