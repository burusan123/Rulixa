using System.Text.Json;
using System.Text.Json.Serialization;
using Rulixa.Cli;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.Tests.Cli;

public sealed class CompareEvidenceBundleTests
{
    [Fact]
    public async Task Main_WithCompareEvidence_RendersDeterministicDiff()
    {
        var root = Path.Combine(Path.GetTempPath(), $"rulixa-compare-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var writer = new EvidenceBundleWriter(CreateJsonOptions());
            var budget = new Budget(MaxFiles: 8, MaxTotalLines: 1600, MaxSnippetsPerFile: 3);
            var entry = new Entry(EntryKind.Symbol, "Sample.App.ShellViewModel");

            var beforeScan = CreateScanResult(new DateTimeOffset(2026, 03, 28, 8, 30, 0, TimeSpan.Zero));
            var afterScan = CreateScanResult(new DateTimeOffset(2026, 03, 28, 8, 45, 0, TimeSpan.Zero));
            var resolvedEntry = new ResolvedEntry(
                "symbol:Sample.App.ShellViewModel",
                ResolvedEntryKind.Symbol,
                "src/Sample.App/ViewModels/ShellViewModel.cs",
                "Sample.App.ShellViewModel",
                ConfidenceLevel.High,
                []);
            var beforePack = new ContextPack(
                Goal: "project",
                Entry: entry,
                ResolvedEntry: resolvedEntry,
                Contracts:
                [
                    new Contract(
                        ContractKind.Startup,
                        "System Pack",
                        "ShellViewModel を起点に Shell / Settings の system map を束ねています。 中心状態は ProjectDocument です。",
                        ["src/Sample.App/ViewModels/ShellViewModel.cs"],
                        ["Sample.App.ShellViewModel"]),
                    new Contract(
                        ContractKind.Navigation,
                        "ViewModel 更新",
                        "Select(...) が CurrentPage を更新します。",
                        ["src/Sample.App/ViewModels/ShellViewModel.cs"],
                        ["Sample.App.ShellViewModel.Select"])
                ],
                Indexes: [],
                SelectedSnippets:
                [
                    new SelectedSnippet(
                        "src/Sample.App/Views/MainWindow.xaml.cs",
                        "root-binding-source",
                        -10,
                        true,
                        "ルート DataContext",
                        4,
                        12,
                        "DataContext = shellViewModel;")
                ],
                SelectedFiles:
                [
                    new SelectedFile("src/Sample.App/Views/ShellView.xaml", "conventional-view", 0, true, 20)
                ],
                DecisionTraces:
                [
                    new PackDecisionTrace(
                        "command-selection",
                        "OpenProjectCommand",
                        "selected-all",
                        "OpenProjectCommand は command 地図に含まれます。",
                        0,
                        1,
                        1,
                        ["project"],
                        [],
                        [])
                ],
                Unknowns: []);
            var afterPack = new ContextPack(
                Goal: "settings",
                Entry: entry,
                ResolvedEntry: resolvedEntry,
                Contracts:
                [
                    new Contract(
                        ContractKind.Startup,
                        "System Pack",
                        "ShellViewModel を起点に Shell / Settings / Report/Export の system map を束ねています。 中心状態は ProjectDocument です。",
                        ["src/Sample.App/ViewModels/ShellViewModel.cs"],
                        ["Sample.App.ShellViewModel"]),
                    new Contract(
                        ContractKind.Navigation,
                        "ViewModel 更新",
                        "Select(...) が CurrentPage と SelectedPageTitle を更新します。",
                        ["src/Sample.App/ViewModels/ShellViewModel.cs"],
                        ["Sample.App.ShellViewModel.Select"]),
                    new Contract(
                        ContractKind.Command,
                        "OpenSettingsCommand",
                        "OpenSettingsCommand が SettingsWindow を開きます。",
                        ["src/Sample.App/ViewModels/ShellViewModel.cs"],
                        ["Sample.App.ShellViewModel.OpenSettings"])
                ],
                Indexes: [],
                SelectedSnippets:
                [
                    new SelectedSnippet(
                        "src/Sample.App/Views/MainWindow.xaml.cs",
                        "root-binding-source",
                        -10,
                        true,
                        "ルート DataContext",
                        4,
                        14,
                        "DataContext = shellViewModel;\nTitle = shellViewModel.Title;")
                ],
                SelectedFiles:
                [
                    new SelectedFile("src/Sample.App/Views/ShellView.xaml", "conventional-view", 0, true, 24),
                    new SelectedFile("src/Sample.App/Views/SettingsWindow.xaml", "dialog-window", 10, true, 12)
                ],
                DecisionTraces:
                [
                    new PackDecisionTrace(
                        "command-selection",
                        "OpenSettingsCommand",
                        "selected-by-goal",
                        "OpenSettingsCommand は goal と一致するため選ばれました。",
                        5,
                        1,
                        7,
                        ["setting"],
                        ["setting"],
                        [new PackDecisionMatchedSource("property-name", ["setting"])]),
                    new PackDecisionTrace(
                        "workflow-selection",
                        "workflow.missing-downstream",
                        "unknown-raised",
                        "既知の範囲: DraftingWorkflowPort。停止点: algorithm / analyzer に到達する前に 2 hop で停止しました。次に見る候補: DiagramAnalyzer, WallAlgorithmRunner",
                        0,
                        0,
                        3,
                        ["drafting"],
                        ["drafting"],
                        [])
                ],
                Unknowns: []);

            var beforeDirectory = await writer.WriteAsync(root, beforeScan.WorkspaceRoot, budget, beforeScan, resolvedEntry, beforePack, "# before");
            var afterDirectory = await writer.WriteAsync(root, afterScan.WorkspaceRoot, budget, afterScan, resolvedEntry, afterPack, "# after");
            var diffPath = Path.Combine(root, "diff.md");

            var exitCode = await Program.Main(
                [
                    "compare-evidence",
                    "--base", beforeDirectory,
                    "--target", afterDirectory,
                    "--out", diffPath
                ]);

            var diff = await File.ReadAllTextAsync(diffPath);

            Assert.Equal(0, exitCode);
            Assert.Contains("## System Pack 差分", diff, StringComparison.Ordinal);
            Assert.Contains("Shell / Settings / Report/Export", diff, StringComparison.Ordinal);
            Assert.Contains("## Unknown Guidance 差分", diff, StringComparison.Ordinal);
            Assert.Contains("Drafting: 既知の範囲: DraftingWorkflowPort。停止点: algorithm / analyzer に到達する前に 2 hop で停止しました。次に見る候補: DiagramAnalyzer, WallAlgorithmRunner", diff, StringComparison.Ordinal);
            Assert.Contains("## メタデータ差分", diff, StringComparison.Ordinal);
            Assert.Contains("goal: `project` -> `settings`", diff, StringComparison.Ordinal);
            Assert.Contains("## 契約差分", diff, StringComparison.Ordinal);
            Assert.Contains("OpenSettingsCommand", diff, StringComparison.Ordinal);
            Assert.Contains("before: Select(...) が CurrentPage を更新します。", diff, StringComparison.Ordinal);
            Assert.Contains("after: Select(...) が CurrentPage と SelectedPageTitle を更新します。", diff, StringComparison.Ordinal);
            Assert.Contains("## 選択ファイル差分", diff, StringComparison.Ordinal);
            Assert.Contains("src/Sample.App/Views/SettingsWindow.xaml (reason: dialog-window, required: required, lines: 12)", diff, StringComparison.Ordinal);
            Assert.Contains("## 選択スニペット差分", diff, StringComparison.Ordinal);
            Assert.Contains("before: 4-12, reason=root-binding-source, required=required", diff, StringComparison.Ordinal);
            Assert.Contains("after: 4-14, reason=root-binding-source, required=required", diff, StringComparison.Ordinal);
            Assert.Contains("## 判断差分", diff, StringComparison.Ordinal);
            Assert.Contains("[command-selection] OpenSettingsCommand (selected-by-goal, score: 5, rank: 1, matched: setting)", diff, StringComparison.Ordinal);
            Assert.Contains("DiagramAnalyzer, WallAlgorithmRunner", diff, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static WorkspaceScanResult CreateScanResult(DateTimeOffset generatedAtUtc) =>
        new(
            "rulixa.phase1.scan.v1",
            @"D:\Workspace\Sample",
            generatedAtUtc,
            new ProjectSummary([], [], ["net8.0-windows"], true, [], ["Sample.App.ShellViewModel"]),
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
