using System.Text.Json;
using Rulixa.Cli;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.Tests.Rendering;

public sealed class CliJsonModelsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [Fact]
    public void FromScanResult_SerializesPhase1WireContract()
    {
        var scanResult = new WorkspaceScanResult(
            "phase1.v1",
            "D:/workspace",
            new DateTimeOffset(2026, 03, 28, 6, 30, 0, TimeSpan.Zero),
            new ProjectSummary(
                ["Sample.sln"],
                ["src/Sample/Sample.csproj"],
                ["net8.0-windows"],
                true,
                ["src/Sample/App.xaml.cs"],
                ["Sample.ViewModels.ShellViewModel"]),
            [
                new ScanFile("src/Sample/Views/ShellView.xaml", ScanFileKind.Xaml, "Sample", "abc123", 42, ["view"])
            ],
            [
                new ScanSymbol("sym-0001", SymbolKind.Command, "Sample.ViewModels.ShellViewModel.OpenSettingsCommand", "OpenSettingsCommand", "src/Sample/ViewModels/ShellViewModel.cs", 12, 12, ["command"])
            ],
            [
                new ViewModelBinding(
                    "src/Sample/Views/ShellView.xaml",
                    "Sample.Views.ShellView",
                    "Sample.ViewModels.ShellViewModel",
                    ViewModelBindingKind.RootDataContext,
                    "src/Sample/Views/ShellView.xaml.cs",
                    new SourceSpan(8, 8),
                    ConfidenceLevel.High,
                    [])
            ],
            [
                new NavigationTransition(
                    "Sample.ViewModels.ShellViewModel",
                    "src/Sample/ViewModels/ShellViewModel.cs",
                    "Select",
                    "SelectedItem",
                    "CurrentPage",
                    "CurrentPage = item.PageViewModel",
                    new SourceSpan(18, 18))
            ],
            [
                new CommandBinding(
                    "Sample.ViewModels.ShellViewModel",
                    "OpenSettingsCommand",
                    "DelegateCommand",
                    "Sample.ViewModels.ShellViewModel.OpenSettings",
                    null,
                    ["src/Sample/Views/ShellView.xaml"])
            ],
            [
                new WindowActivation(
                    "Sample.Services.SettingWindowService.Show",
                    "Sample.Services.SettingWindowService",
                    "Sample.Views.SettingWindow",
                    null,
                    "show-dialog",
                    "main-window")
            ],
            [
                new ServiceRegistration(
                    "src/Sample/ServiceRegistration.cs",
                    "Sample.Services.ISettingWindowService",
                    "Sample.Services.SettingWindowService",
                    ServiceRegistrationLifetime.Singleton,
                    new SourceSpan(5, 5))
            ],
            [
                new Diagnostic("binding.viewmodel.ambiguous", "ambiguous", "src/Sample/Views/ShellView.xaml", DiagnosticSeverity.Warning, ["A", "B"])
            ]);

        var json = JsonSerializer.Serialize(CliJsonModels.FromScanResult(scanResult), JsonOptions);

        Assert.Contains("\"generatedAtUtc\": \"2026-03-28T06:30:00.0000000Z\"", json);
        Assert.Contains("\"kind\": \"xaml\"", json);
        Assert.Contains("\"bindingKind\": \"root-data-context\"", json);
        Assert.Contains("\"lifetime\": \"singleton\"", json);
        Assert.Contains("\"hash\": \"sha256:abc123\"", json);
        Assert.DoesNotContain("\"GeneratedAtUtc\"", json);
    }

    [Fact]
    public void FromResolvedEntry_SerializesLowerCamelAndKebabTokens()
    {
        var resolvedEntry = new ResolvedEntry(
            "symbol:Sample.ViewModels.ShellViewModel.OpenSettings",
            ResolvedEntryKind.Symbol,
            "src/Sample/ViewModels/ShellViewModel.cs",
            "Sample.ViewModels.ShellViewModel.OpenSettings",
            ConfidenceLevel.Medium,
            [
                new ResolvedCandidate(CandidateKind.ViewModel, null, "Sample.ViewModels.ShellViewModel", "auto-viewmodel-convention-match")
            ]);

        var json = JsonSerializer.Serialize(CliJsonModels.FromResolvedEntry(resolvedEntry), JsonOptions);

        Assert.Contains("\"resolvedKind\": \"symbol\"", json);
        Assert.Contains("\"confidence\": \"medium\"", json);
        Assert.Contains("\"kind\": \"view-model\"", json);
        Assert.DoesNotContain("\"ResolvedKind\"", json);
    }
}
