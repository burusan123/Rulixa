using Rulixa.Application.HumanOutputs;
using Rulixa.Application.Ports;
using Rulixa.Application.UseCases;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.Tests.UseCases;

public sealed class RenderVisualOutputUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_BuildsFiveViewsAndStructuredInspectorPayload()
    {
        var renderer = new CapturingVisualOutputRenderer();
        var useCase = new RenderVisualOutputUseCase(renderer);

        var result = await useCase.ExecuteAsync(
            CreateContextPack(),
            CreateScanResult(),
            Path.Combine(Path.GetTempPath(), "rulixa-visual-test"),
            new VisualOutputRenderOptions(@"artifacts\evidence\bundle-1"));

        Assert.Equal("index.html", result.IndexPath);
        Assert.NotNull(renderer.Document);
        Assert.Equal(5, renderer.Document!.Views.Count);
        Assert.Contains(renderer.Document.Views, static view => view.Id == "overview");
        Assert.Contains(renderer.Document.Views, static view => view.Id == "workflow");
        Assert.Contains(renderer.Document.Views, static view => view.Id == "evidence");
        Assert.Contains(renderer.Document.Views, static view => view.Id == "unknowns");
        Assert.Contains(renderer.Document.Views, static view => view.Id == "architecture");

        var overview = renderer.Document.Views.Single(static view => view.Id == "overview");
        Assert.Contains(overview.Sections.SelectMany(static section => section.Items), static item =>
            item.Title.Contains("ShellViewModel", StringComparison.Ordinal));
        Assert.Contains(overview.Sections.SelectMany(static section => section.Items), static item =>
            item.Title.Contains("ProjectDocument", StringComparison.Ordinal));

        var workflow = renderer.Document.Views.Single(static view => view.Id == "workflow");
        Assert.NotNull(workflow.Sections.Single(static section => section.Id == "workflow-routes").Graph);

        var evidence = renderer.Document.Views.Single(static view => view.Id == "evidence");
        Assert.Contains(evidence.Sections.SelectMany(static section => section.Items), static item =>
            item.Title.Contains("ShellViewModel.cs", StringComparison.Ordinal));

        var unknowns = renderer.Document.Views.Single(static view => view.Id == "unknowns");
        Assert.Contains(unknowns.Sections.SelectMany(static section => section.Items), static item =>
            item.Title.Contains("workflow.missing-downstream", StringComparison.Ordinal));

        var architecture = renderer.Document.Views.Single(static view => view.Id == "architecture");
        Assert.Contains(architecture.Sections, static section => section.Title == "Constraints");
        Assert.Contains(architecture.Sections, static section => section.Title == "Known Unknowns");

        Assert.Contains(renderer.Document.InspectorItems.Keys, static key => key == "root-entry");
        Assert.Contains(renderer.Document.InspectorItems.Keys, static key => key == "unknown-0");
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
                    "ShellViewModel が Shell / Drafting / Settings / Report/Export を束ねます。中心状態は ProjectDocument です。",
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
                    "AsmProjectRepository と ExcelSettingsQuery を利用します。",
                    ["src/App/ServiceRegistration.cs"],
                    ["App.ProjectWorkflowService"]),
                new Contract(
                    ContractKind.DependencyInjection,
                    "Architecture Tests",
                    "LayerGuardTests が layer 制約を守ります。",
                    ["tests/App.Architecture.Tests/LayerGuardTests.cs"],
                    ["App.Architecture.Tests.LayerGuardTests"])
            ],
            Indexes:
            [
                new IndexSection("Workflow", ["ShellViewModel -> ProjectWorkflowService -> ProjectDocument"]),
                new IndexSection("Persistence", ["ProjectWorkflowService -> AsmProjectRepository"]),
                new IndexSection("Architecture Tests", ["LayerGuardTests"])
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
                    "既知の限界: Drafting downstream は 2 hop 以内では確定できません。",
                    null,
                    DiagnosticSeverity.Info,
                    ["App.Services.DraftingAnalyzer", "App.Services.DraftingPersistenceAdapter"])
            ]);

    private static WorkspaceScanResult CreateScanResult() =>
        new(
            "phase9.test",
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
                new Diagnostic(
                    "xaml.parse-degraded",
                    "既知の限界: commented block を無視して scan を継続しました。",
                    null,
                    DiagnosticSeverity.Warning,
                    ["ShellWindow.xaml"])
            ]);

    private sealed class CapturingVisualOutputRenderer : IVisualOutputRenderer
    {
        public VisualOutputDocument? Document { get; private set; }

        public Task<VisualOutputRenderResult> RenderAsync(
            VisualOutputDocument document,
            string outputDirectory,
            CancellationToken cancellationToken = default)
        {
            Document = document;
            return Task.FromResult(new VisualOutputRenderResult("index.html", "app.css", "app.js"));
        }
    }
}
