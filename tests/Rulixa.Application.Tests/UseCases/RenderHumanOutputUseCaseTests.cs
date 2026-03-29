using Rulixa.Application.HumanOutputs;
using Rulixa.Application.Ports;
using Rulixa.Application.UseCases;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.Tests.UseCases;

public sealed class RenderHumanOutputUseCaseTests
{
    [Fact]
    public void Execute_ForReview_BuildsCenterStateWorkflowAndNextCandidates()
    {
        var renderer = new CapturingHumanOutputRenderer();
        var useCase = new RenderHumanOutputUseCase(renderer);

        var markdown = useCase.Execute(
            CreateContextPack(),
            CreateScanResult(),
            HumanOutputMode.Review,
            new HumanOutputRenderOptions(@"artifacts\evidence\bundle-1"));

        Assert.Equal("rendered", markdown);
        Assert.NotNull(renderer.Document);
        Assert.Equal(HumanOutputMode.Review, renderer.Document!.Mode);
        Assert.Contains(renderer.Document.Sections, static section => section.Title == "中心状態"
            && section.BulletPoints.Any(line => line.Contains("ProjectDocument", StringComparison.Ordinal)));
        Assert.Contains(renderer.Document.Sections, static section => section.Title == "主要 workflow"
            && section.BulletPoints.Any(line => line.Contains("ShellViewModel", StringComparison.Ordinal)));
        Assert.Contains(renderer.Document.Sections, static section => section.Title == "Unknown / Risk"
            && section.BulletPoints.Any(line => line.StartsWith("unknown:", StringComparison.Ordinal)));
        Assert.Contains(renderer.Document.Sections, static section => section.Title == "次に読む file / symbol"
            && section.BulletPoints.Any(line => line.Contains("DraftingAnalyzer", StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_ForAudit_BuildsObservedFactsEvidenceAndDiagnostics()
    {
        var renderer = new CapturingHumanOutputRenderer();
        var useCase = new RenderHumanOutputUseCase(renderer);

        useCase.Execute(
            CreateContextPack(),
            CreateScanResult(),
            HumanOutputMode.Audit,
            new HumanOutputRenderOptions(@"artifacts\evidence\bundle-1"));

        Assert.NotNull(renderer.Document);
        Assert.Equal(HumanOutputMode.Audit, renderer.Document!.Mode);
        Assert.Contains(renderer.Document.Sections, static section => section.Title == "Root Entry"
            && section.BulletPoints.Any(line => line.Contains("symbol:App.ViewModels.ShellViewModel", StringComparison.Ordinal)));
        Assert.Contains(renderer.Document.Sections, static section => section.Title == "Observed Facts"
            && section.BulletPoints.Any(line => line.StartsWith("断定:", StringComparison.Ordinal)));
        Assert.Contains(renderer.Document.Sections, static section => section.Title == "Evidence Source"
            && section.BulletPoints.Any(line => line.Contains("evidence bundle", StringComparison.Ordinal)));
        Assert.Contains(renderer.Document.Sections, static section => section.Title == "Degraded Diagnostics"
            && section.BulletPoints.Any(line => line.Contains("degraded diagnostics", StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_ForKnowledge_OnWeakSignalCase_DoesNotHideUnknown()
    {
        var renderer = new CapturingHumanOutputRenderer();
        var useCase = new RenderHumanOutputUseCase(renderer);

        useCase.Execute(
            CreateWeakSignalContextPack(),
            CreateScanResult(),
            HumanOutputMode.Knowledge,
            new HumanOutputRenderOptions(null));

        Assert.NotNull(renderer.Document);
        Assert.Equal(HumanOutputMode.Knowledge, renderer.Document!.Mode);
        Assert.Contains(renderer.Document.Sections, static section => section.Title == "Dependency Seams");
        Assert.Contains(renderer.Document.Sections, static section => section.Title == "Architectural Constraints"
            && section.BulletPoints.Any(line => line.StartsWith("unknown:", StringComparison.Ordinal)));
        Assert.Contains(renderer.Document.Sections, static section => section.Title == "Known Unknowns"
            && section.BulletPoints.Any(line => line.StartsWith("unknown:", StringComparison.Ordinal)));
        Assert.DoesNotContain(renderer.Document.Sections.SelectMany(static section => section.BulletPoints), static line =>
            line.Contains("exception", StringComparison.OrdinalIgnoreCase));
    }

    private static ContextPack CreateContextPack() =>
        new(
            Goal: "project",
            Entry: new Entry(EntryKind.Symbol, "App.ViewModels.ShellViewModel"),
            ResolvedEntry: new ResolvedEntry(
                "symbol:App.ViewModels.ShellViewModel",
                ResolvedEntryKind.Symbol,
                @".\src\App\ViewModels\ShellViewModel.cs",
                "App.ViewModels.ShellViewModel",
                ConfidenceLevel.High,
                []),
            Contracts:
            [
                new Contract(
                    ContractKind.Navigation,
                    "System Pack",
                    "ShellViewModel から Shell / Drafting / Settings / Report/Export を束ねます。中心状態は ProjectDocument です。",
                    ["src/App/ViewModels/ShellViewModel.cs"],
                    ["App.ViewModels.ShellViewModel"]),
                new Contract(
                    ContractKind.Command,
                    "Workflow",
                    "ShellViewModel -> ProjectWorkflowService -> ProjectDocument",
                    ["src/App/ViewModels/ShellViewModel.cs"],
                    ["App.ViewModels.ShellViewModel"]),
                new Contract(
                    ContractKind.DependencyInjection,
                    "Persistence",
                    "AsmProjectRepository と ExcelSettingsQuery を経由します。",
                    ["src/App/ServiceRegistration.cs"],
                    ["App.ProjectWorkflowService"]),
                new Contract(
                    ContractKind.DependencyInjection,
                    "External Assets",
                    "Excel template と settings asset を読み込みます。",
                    ["src/App/Exports/ExportService.cs"],
                    ["App.ExportService"])
            ],
            Indexes:
            [
                new IndexSection("Workflow", ["ShellViewModel -> ProjectWorkflowService -> ProjectDocument"]),
                new IndexSection("Persistence", ["ProjectWorkflowService -> AsmProjectRepository"]),
                new IndexSection("Hub Objects", ["ProjectDocument"])
            ],
            SelectedSnippets:
            [
                new SelectedSnippet(
                    @".\src\App\ViewModels\ShellViewModel.cs",
                    "root-binding-source",
                    0,
                    true,
                    "ShellViewModel",
                    10,
                    18,
                    "public sealed class ShellViewModel\n{\n    private readonly ProjectWorkflowService workflow;\n}"),
                new SelectedSnippet(
                    @".\src\App\ServiceRegistration.cs",
                    "dependency-injection",
                    1,
                    true,
                    "ServiceRegistration",
                    20,
                    28,
                    "services.AddSingleton<ShellViewModel>();")
            ],
            SelectedFiles:
            [
                new SelectedFile(@".\src\App\ViewModels\ShellViewModel.cs", "entry", 0, true, 120),
                new SelectedFile(@".\src\App\Services\DraftingAnalyzer.cs", "workflow", 1, false, 80)
            ],
            DecisionTraces: [],
            Unknowns:
            [
                new Diagnostic(
                    "workflow.missing-downstream",
                    "既知の状況: Drafting downstream は 2 hop 内で確定できませんでした。",
                    null,
                    DiagnosticSeverity.Info,
                    ["App.Services.DraftingAnalyzer", "App.Services.DraftingPersistenceAdapter"])
            ]);

    private static ContextPack CreateWeakSignalContextPack() =>
        new(
            Goal: "legacy system",
            Entry: new Entry(EntryKind.File, "ShellWindow.xaml"),
            ResolvedEntry: new ResolvedEntry(
                "file:ShellWindow.xaml",
                ResolvedEntryKind.File,
                @"ShellWindow.xaml",
                null,
                ConfidenceLevel.High,
                []),
            Contracts: [],
            Indexes: [],
            SelectedSnippets: [],
            SelectedFiles:
            [
                new SelectedFile("ShellWindow.xaml", "entry", 0, true, 40)
            ],
            DecisionTraces: [],
            Unknowns:
            [
                new Diagnostic(
                    "workflow.missing-downstream",
                    "既知の状況: helper 経由の route は確認できたが target family は未確定です。",
                    null,
                    DiagnosticSeverity.Info,
                    ["LegacyApp.SettingsWindowAdapter"])
            ]);

    private static WorkspaceScanResult CreateScanResult() =>
        new(
            "phase8.test",
            "D:/workspace",
            new DateTimeOffset(2026, 03, 29, 8, 0, 0, TimeSpan.Zero),
            new ProjectSummary([], [], ["net9.0"], true, [], ["App.ViewModels.ShellViewModel"]),
            [
                new ScanFile("src/App/ViewModels/ShellViewModel.cs", ScanFileKind.ViewModel, "App", "hash-1", 120, []),
                new ScanFile("src/App/ViewModels/ProjectWorkflowService.cs", ScanFileKind.CSharp, "App", "hash-2", 90, [])
            ],
            [],
            [],
            [],
            [],
            [],
            [],
            [
                new Diagnostic("xaml.parse-degraded", "既知の状況: commented block を無視して scan を継続しました。", null, DiagnosticSeverity.Warning, ["ShellWindow.xaml"])
            ]);

    private sealed class CapturingHumanOutputRenderer : IHumanOutputRenderer
    {
        public HumanOutputDocument? Document { get; private set; }

        public string Render(HumanOutputDocument document)
        {
            Document = document;
            return "rendered";
        }
    }
}
