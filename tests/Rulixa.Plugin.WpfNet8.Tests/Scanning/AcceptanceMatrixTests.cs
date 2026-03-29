using Rulixa.Application.UseCases;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;
using Rulixa.Infrastructure.FileSystem;
using Rulixa.Infrastructure.Resolution;
using Rulixa.Plugin.WpfNet8.Extraction;
using Rulixa.Plugin.WpfNet8.Scanning;

namespace Rulixa.Plugin.WpfNet8.Tests.Scanning;

public sealed class AcceptanceMatrixTests
{
    [Theory]
    [MemberData(nameof(RootCases))]
    public async Task BuildPack_ForCorpusRootEntry_ReturnsExpectedSystemMap(
        string workspaceRoot,
        Entry entry,
        string goal,
        string[] expectedFamilies,
        bool expectCenterState)
    {
        var (scanResult, pack) = await BuildPackAsync(workspaceRoot, entry, goal);

        Assert.DoesNotContain(scanResult.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var systemPack = Assert.Single(pack.Contracts, contract => contract.Title == "System Pack");
        foreach (var expectedFamily in expectedFamilies)
        {
            Assert.Contains(expectedFamily, systemPack.Summary, StringComparison.Ordinal);
        }

        if (expectCenterState)
        {
            Assert.Contains("ProjectDocument", systemPack.Summary, StringComparison.Ordinal);
        }

        AssertUniqueUnknowns(pack);
    }

    [Theory]
    [MemberData(nameof(NonRootCases))]
    public async Task BuildPack_ForCorpusNonRootEntry_DoesNotPromoteSystemPack(string workspaceRoot, Entry entry, string goal)
    {
        var (_, pack) = await BuildPackAsync(workspaceRoot, entry, goal);

        Assert.DoesNotContain(pack.Contracts, contract => contract.Title == "System Pack");
    }

    [Fact]
    public async Task ScanAsync_ForTemplateHeavyFixture_ReturnsPartialBindingAndDegradedDiagnostic()
    {
        var scanner = new WpfNet8WorkspaceScanner(new WorkspaceFileSystem());
        var result = await scanner.ScanAsync(TemplateHeavyResourcesRoot);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(result.ViewModelBindings, binding =>
            binding.ViewPath == "Themes/SharedTemplates.xaml"
            && binding.BindingKind == ViewModelBindingKind.DataTemplate
            && binding.ViewModelSymbol == "TemplateHeavyResources.ViewModels.ShellViewModel");
        Assert.Contains(result.ViewModelBindings, binding =>
            binding.ViewPath == "ShellWindow.xaml"
            && binding.BindingKind == ViewModelBindingKind.RootDataContext
            && binding.ViewModelSymbol == "TemplateHeavyResources.ShellWindow");
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == "xaml.parse-degraded"
            && diagnostic.FilePath == "Themes/SharedTemplates.xaml");
    }

    [Theory]
    [InlineData("LegacyDialogHeavy", true)]
    [InlineData("LegacyServiceLocator", true)]
    [InlineData("ModernSiblingRoot", false)]
    public async Task BuildPack_ForCorpusRootEntry_AggregatesUnknownsByFamily(string fixtureName, bool expectUnknowns)
    {
        var workspaceRoot = fixtureName switch
        {
            "LegacyDialogHeavy" => LegacyDialogHeavyRoot,
            "LegacyServiceLocator" => LegacyServiceLocatorRoot,
            "ModernSiblingRoot" => ModernSiblingRootRoot,
            _ => throw new ArgumentOutOfRangeException(nameof(fixtureName), fixtureName, null)
        };
        var entry = fixtureName == "ModernSiblingRoot"
            ? new Entry(EntryKind.Symbol, "ModernSiblingRoot.ViewModels.ShellViewModel")
            : new Entry(EntryKind.File, "ShellWindow.xaml");
        var goal = fixtureName == "ModernSiblingRoot" ? "system" : "legacy system";

        var (_, pack) = await BuildPackAsync(workspaceRoot, entry, goal);

        if (expectUnknowns)
        {
            Assert.NotEmpty(pack.Unknowns);
        }

        AssertUniqueUnknowns(pack);
    }

    [Fact]
    public async Task BuildPack_ForWeakSignalRoot_ReturnsGuidedUnknownWithoutOverPromotingPersistence()
    {
        var (_, pack) = await BuildPackAsync(
            TemplateHeavyResourcesRoot,
            new Entry(EntryKind.File, "ShellWindow.xaml"),
            "legacy system");

        Assert.NotEmpty(pack.Unknowns);
        Assert.DoesNotContain(pack.Indexes, static index =>
            (index.Title == "Persistence" || index.Title == "Architecture Tests")
            && index.Lines.Count > 0);
        AssertUniqueUnknowns(pack);
    }

    public static IEnumerable<object[]> RootCases()
    {
        yield return
        [
            LegacyDialogHeavyRoot,
            new Entry(EntryKind.File, "ShellWindow.xaml"),
            "legacy system",
            new[] { "Settings", "Report/Export" },
            true
        ];
        yield return
        [
            LegacyServiceLocatorRoot,
            new Entry(EntryKind.File, "ShellWindow.xaml"),
            "legacy system",
            new[] { "Drafting", "Report/Export" },
            true
        ];
        yield return
        [
            ModernSiblingRootRoot,
            new Entry(EntryKind.Symbol, "ModernSiblingRoot.ViewModels.ShellViewModel"),
            "system",
            new[] { "3D", "Settings", "Report/Export" },
            true
        ];
    }

    public static IEnumerable<object[]> NonRootCases()
    {
        yield return [LegacyDialogHeavyRoot, new Entry(EntryKind.Symbol, "LegacyDialogHeavy.SettingsWindowAdapter"), "legacy system"];
        yield return [LegacyServiceLocatorRoot, new Entry(EntryKind.Symbol, "LegacyServiceLocator.ReportWindowService"), "legacy system"];
        yield return [ModernSiblingRootRoot, new Entry(EntryKind.Symbol, "ModernSiblingRoot.ViewModels.SettingsViewModel"), "system"];
        yield return [TemplateHeavyResourcesRoot, new Entry(EntryKind.Symbol, "TemplateHeavyResources.ViewModels.ShellViewModel"), "legacy system"];
    }

    private static readonly string LegacyDialogHeavyRoot = GetFixtureRoot("LegacyDialogHeavy");
    private static readonly string LegacyServiceLocatorRoot = GetFixtureRoot("LegacyServiceLocator");
    private static readonly string ModernSiblingRootRoot = GetFixtureRoot("ModernSiblingRoot");
    private static readonly string TemplateHeavyResourcesRoot = GetFixtureRoot("TemplateHeavyResources");

    private static async Task<(WorkspaceScanResult ScanResult, ContextPack Pack)> BuildPackAsync(
        string workspaceRoot,
        Entry entry,
        string goal)
    {
        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);
        var buildPackUseCase = new BuildContextPackUseCase(extractor);

        var scanResult = await scanner.ScanAsync(workspaceRoot);
        var resolvedEntry = await resolver.ResolveAsync(entry, scanResult);
        var pack = await buildPackUseCase.ExecuteAsync(
            workspaceRoot,
            scanResult,
            entry,
            resolvedEntry,
            goal,
            Budget.Default);

        return (scanResult, pack);
    }

    private static string GetFixtureRoot(string fixtureName) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", fixtureName));

    private static void AssertUniqueUnknowns(ContextPack pack)
    {
        var duplicateGroup = pack.Unknowns
            .GroupBy(static unknown =>
                $"{unknown.Code}|{ExtractFamily(unknown)}|{ExtractRoot(unknown)}",
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1);

        Assert.True(duplicateGroup is null, $"Unknowns were not aggregated: {duplicateGroup?.Key}");
    }

    private static string ExtractFamily(Diagnostic diagnostic) =>
        diagnostic.Candidates.FirstOrDefault(static candidate =>
            candidate.Contains("Drafting", StringComparison.OrdinalIgnoreCase)
            || candidate.Contains("Settings", StringComparison.OrdinalIgnoreCase)
            || candidate.Contains("Report", StringComparison.OrdinalIgnoreCase)
            || candidate.Contains("ThreeD", StringComparison.OrdinalIgnoreCase)
            || candidate.Contains("3D", StringComparison.OrdinalIgnoreCase))
        ?? string.Empty;

    private static string ExtractRoot(Diagnostic diagnostic) =>
        diagnostic.Candidates.FirstOrDefault(static candidate =>
            candidate.Contains("Shell", StringComparison.OrdinalIgnoreCase)
            || candidate.Contains("Window", StringComparison.OrdinalIgnoreCase))
        ?? string.Empty;
}
