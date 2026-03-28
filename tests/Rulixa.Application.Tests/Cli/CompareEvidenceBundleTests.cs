using System.Text.Json;
using System.Text.Json.Serialization;
using Rulixa.Cli;
using Rulixa.Domain.Diagnostics;
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
                        ContractKind.Navigation,
                        "ViewModel 更新点",
                        "RestoreSelection(...) が SelectedItem を更新します。",
                        ["src/Sample.App/ViewModels/ShellViewModel.cs"],
                        ["Sample.App.ShellViewModel.RestoreSelection"]),
                    new Contract(
                        ContractKind.Navigation,
                        "ViewModel 更新点",
                        "Select(...) が CurrentPage を更新します。",
                        ["src/Sample.App/ViewModels/ShellViewModel.cs"],
                        ["Sample.App.ShellViewModel.Select"]),
                    new Contract(
                        ContractKind.Navigation,
                        "選択から表示への因果",
                        "SelectedItem が CurrentPage を更新します。",
                        ["src/Sample.App/ViewModels/ShellViewModel.cs"],
                        ["Sample.App.ShellViewModel"])
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
                Unknowns: []);
            var afterPack = new ContextPack(
                Goal: "settings",
                Entry: entry,
                ResolvedEntry: resolvedEntry,
                Contracts:
                [
                    new Contract(
                        ContractKind.Navigation,
                        "ViewModel 更新点",
                        "RestoreSelection(...) が SelectedItem を更新します。",
                        ["src/Sample.App/ViewModels/ShellViewModel.cs"],
                        ["Sample.App.ShellViewModel.RestoreSelection"]),
                    new Contract(
                        ContractKind.Navigation,
                        "ViewModel 更新点",
                        "Select(...) が CurrentPage を更新します。",
                        ["src/Sample.App/ViewModels/ShellViewModel.cs"],
                        ["Sample.App.ShellViewModel.Select"]),
                    new Contract(
                        ContractKind.Navigation,
                        "選択から表示への因果",
                        "SelectedItem が CurrentPage と SelectedPageTitle を更新します。",
                        ["src/Sample.App/ViewModels/ShellViewModel.cs"],
                        ["Sample.App.ShellViewModel"]),
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
            Assert.Contains("## メタデータ差分", diff, StringComparison.Ordinal);
            Assert.Contains("goal: `project` -> `settings`", diff, StringComparison.Ordinal);
            Assert.Contains("## 契約差分", diff, StringComparison.Ordinal);
            Assert.Contains("[command] OpenSettingsCommand: OpenSettingsCommand が SettingsWindow を開きます。", diff, StringComparison.Ordinal);
            Assert.Contains("before: SelectedItem が CurrentPage を更新します。", diff, StringComparison.Ordinal);
            Assert.Contains("after: SelectedItem が CurrentPage と SelectedPageTitle を更新します。", diff, StringComparison.Ordinal);
            Assert.Contains("## 選定ファイル差分", diff, StringComparison.Ordinal);
            Assert.Contains("src/Sample.App/Views/SettingsWindow.xaml (reason: dialog-window, required: required, lines: 12)", diff, StringComparison.Ordinal);
            Assert.Contains("## 選定スニペット差分", diff, StringComparison.Ordinal);
            Assert.Contains("before: 4-12, reason=root-binding-source, required=required", diff, StringComparison.Ordinal);
            Assert.Contains("after: 4-14, reason=root-binding-source, required=required", diff, StringComparison.Ordinal);
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
