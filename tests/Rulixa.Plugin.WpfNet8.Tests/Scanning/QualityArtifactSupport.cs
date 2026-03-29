using System.Text.Json;
using Rulixa.Application.UseCases;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;
using Rulixa.Infrastructure.FileSystem;
using Rulixa.Infrastructure.Quality;
using Rulixa.Infrastructure.Resolution;
using Rulixa.Plugin.WpfNet8.Extraction;
using Rulixa.Plugin.WpfNet8.Scanning;

namespace Rulixa.Plugin.WpfNet8.Tests.Scanning;

internal static class QualityArtifactSupport
{
    internal static readonly string RepositoryRoot = GetRepositoryRoot();
    internal static readonly string LegacyDialogHeavyRoot = GetFixtureRoot("LegacyDialogHeavy");
    internal static readonly string LegacyServiceLocatorRoot = GetFixtureRoot("LegacyServiceLocator");
    internal static readonly string ModernSiblingRootRoot = GetFixtureRoot("ModernSiblingRoot");
    internal static readonly string TemplateHeavyResourcesRoot = GetFixtureRoot("TemplateHeavyResources");

    internal static IReadOnlyList<QualityCaseDefinition> CreateSyntheticCaseDefinitions() =>
    [
        new(
            CaseId: "legacy-dialog-root",
            CorpusName: "LegacyDialogHeavy",
            WorkspaceType: "synthetic-legacy",
            WorkspaceRoot: LegacyDialogHeavyRoot,
            Entry: new Entry(EntryKind.File, "ShellWindow.xaml"),
            Goal: "legacy system",
            Tags: ["root-case", "deterministic"],
            RequiredFamilies: ["Settings", "Report/Export"],
            RequireCenterState: true,
            ExpectUnknownGuidance: false,
            DisallowedRepresentativeSections: []),
        new(
            CaseId: "legacy-dialog-non-root",
            CorpusName: "LegacyDialogHeavy",
            WorkspaceType: "synthetic-legacy",
            WorkspaceRoot: LegacyDialogHeavyRoot,
            Entry: new Entry(EntryKind.Symbol, "LegacyDialogHeavy.SettingsWindowAdapter"),
            Goal: "legacy system",
            Tags: [],
            RequiredFamilies: [],
            RequireCenterState: false,
            ExpectUnknownGuidance: false,
            DisallowedRepresentativeSections: []),
        new(
            CaseId: "legacy-service-locator-root",
            CorpusName: "LegacyServiceLocator",
            WorkspaceType: "synthetic-legacy",
            WorkspaceRoot: LegacyServiceLocatorRoot,
            Entry: new Entry(EntryKind.File, "ShellWindow.xaml"),
            Goal: "legacy system",
            Tags: ["root-case", "deterministic"],
            RequiredFamilies: ["Drafting", "Report/Export"],
            RequireCenterState: true,
            ExpectUnknownGuidance: true,
            DisallowedRepresentativeSections: []),
        new(
            CaseId: "modern-sibling-root",
            CorpusName: "ModernSiblingRoot",
            WorkspaceType: "synthetic-modern",
            WorkspaceRoot: ModernSiblingRootRoot,
            Entry: new Entry(EntryKind.Symbol, "ModernSiblingRoot.ViewModels.ShellViewModel"),
            Goal: "system",
            Tags: ["root-case", "deterministic"],
            RequiredFamilies: ["3D", "Settings", "Report/Export"],
            RequireCenterState: true,
            ExpectUnknownGuidance: false,
            DisallowedRepresentativeSections: []),
        new(
            CaseId: "template-heavy-root",
            CorpusName: "TemplateHeavyResources",
            WorkspaceType: "synthetic-legacy",
            WorkspaceRoot: TemplateHeavyResourcesRoot,
            Entry: new Entry(EntryKind.File, "ShellWindow.xaml"),
            Goal: "legacy system",
            Tags: ["root-case"],
            RequiredFamilies: [],
            RequireCenterState: false,
            ExpectUnknownGuidance: true,
            DisallowedRepresentativeSections: ["Persistence", "Architecture Tests"]),
        new(
            CaseId: "template-heavy-weak-signal",
            CorpusName: "TemplateHeavyResources",
            WorkspaceType: "synthetic-legacy",
            WorkspaceRoot: TemplateHeavyResourcesRoot,
            Entry: new Entry(EntryKind.Symbol, "TemplateHeavyResources.ViewModels.ShellViewModel"),
            Goal: "legacy system",
            Tags: ["weak-signal"],
            RequiredFamilies: [],
            RequireCenterState: false,
            ExpectUnknownGuidance: true,
            DisallowedRepresentativeSections: ["Persistence", "Architecture Tests", "Workflow"])
    ];

    internal static IReadOnlyList<QualityCaseDefinition> CreateOptionalSmokeCaseDefinitions() =>
    [
        new(
            CaseId: "assessmeister-modern-shell",
            CorpusName: "AssessMeister",
            WorkspaceType: "modern-real",
            WorkspaceRoot: AssessMeisterOptionalSmokeTests.WorkspaceRoot,
            Entry: new Entry(EntryKind.Symbol, "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel"),
            Goal: "project",
            Tags: ["root-case", "optional-smoke"],
            RequiredFamilies: ["Drafting", "Settings", "Report/Export", "Architecture"],
            RequireCenterState: true,
            ExpectUnknownGuidance: false,
            DisallowedRepresentativeSections: []),
        new(
            CaseId: "assessmeister-modern-drafting",
            CorpusName: "AssessMeister",
            WorkspaceType: "modern-real",
            WorkspaceRoot: AssessMeisterOptionalSmokeTests.WorkspaceRoot,
            Entry: new Entry(EntryKind.Symbol, "AssessMeister.Presentation.Wpf.ViewModels.Drafting.DraftingWindowViewModel"),
            Goal: "drafting ai analyze",
            Tags: ["optional-smoke"],
            RequiredFamilies: [],
            RequireCenterState: false,
            ExpectUnknownGuidance: true,
            DisallowedRepresentativeSections: []),
        new(
            CaseId: "assessmeister-legacy-root",
            CorpusName: "AssessMeister_20260204",
            WorkspaceType: "legacy-real",
            WorkspaceRoot: AssessMeisterLegacyOptionalSmokeTests.WorkspaceRoot,
            Entry: new Entry(EntryKind.File, "AssessMeister/Predict3DWindow.xaml"),
            Goal: "legacy system",
            Tags: ["root-case", "optional-smoke"],
            RequiredFamilies: ["Settings", "Report/Export"],
            RequireCenterState: false,
            ExpectUnknownGuidance: true,
            DisallowedRepresentativeSections: [])
    ];

    internal static async Task<QualityCaseArtifact> ExecuteCaseAsync(QualityCaseDefinition definition, CancellationToken cancellationToken = default)
    {
        var skipReason = GetSkipReason(definition);
        if (!string.IsNullOrWhiteSpace(skipReason))
        {
            return new QualityCaseArtifact(
                CaseId: definition.CaseId,
                CorpusName: definition.CorpusName,
                WorkspaceType: definition.WorkspaceType,
                Entry: definition.Entry.ToString(),
                Goal: definition.Goal,
                Status: "skipped",
                Tags: definition.Tags,
                PackSuccess: null,
                CrashFree: null,
                PartialPack: null,
                HasUnknownGuidance: null,
                FalseConfidenceDetected: null,
                Deterministic: null,
                DurationMilliseconds: 0,
                FirstUsefulMapTimeMs: null,
                FailureReason: null,
                SkipReason: skipReason,
                DegradedDiagnosticCount: 0,
                RepresentativeChainCount: 0,
                DegradedReasonCount: 0,
                UnknownGuidance: []);
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var firstRun = await BuildPackAsync(definition, cancellationToken).ConfigureAwait(false);
            var deterministic = definition.Tags.Contains("deterministic", StringComparer.OrdinalIgnoreCase)
                ? await IsDeterministicAsync(definition, firstRun, cancellationToken).ConfigureAwait(false)
                : true;
            stopwatch.Stop();

            var packSuccess = firstRun.Pack is not null;
            var partialPack = firstRun.ScanResult.Diagnostics.Any(static diagnostic => diagnostic.Severity != DiagnosticSeverity.Error)
                || firstRun.Pack!.Unknowns.Count > 0;
            var hasUnknownGuidance = firstRun.Pack!.Unknowns.Any(static unknown => unknown.Candidates.Count > 0);
            var falseConfidenceDetected = DetectFalseConfidence(definition, firstRun.Pack);
            var degradedDiagnosticCount = firstRun.ScanResult.Diagnostics.Count(static diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
            var representativeChainCount = CountRepresentativeChains(firstRun.Pack);
            var degradedReasonCount = CountDegradedReasons(firstRun.ScanResult, firstRun.Pack);

            return new QualityCaseArtifact(
                CaseId: definition.CaseId,
                CorpusName: definition.CorpusName,
                WorkspaceType: definition.WorkspaceType,
                Entry: definition.Entry.ToString(),
                Goal: definition.Goal,
                Status: "passed",
                Tags: definition.Tags,
                PackSuccess: packSuccess,
                CrashFree: true,
                PartialPack: partialPack,
                HasUnknownGuidance: hasUnknownGuidance,
                FalseConfidenceDetected: falseConfidenceDetected,
                Deterministic: deterministic,
                DurationMilliseconds: stopwatch.ElapsedMilliseconds,
                FirstUsefulMapTimeMs: packSuccess ? stopwatch.ElapsedMilliseconds : null,
                FailureReason: null,
                SkipReason: null,
                DegradedDiagnosticCount: degradedDiagnosticCount,
                RepresentativeChainCount: representativeChainCount,
                DegradedReasonCount: degradedReasonCount,
                UnknownGuidance: firstRun.Pack.Unknowns.Select(ToUnknownGuidance).ToArray());
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return new QualityCaseArtifact(
                CaseId: definition.CaseId,
                CorpusName: definition.CorpusName,
                WorkspaceType: definition.WorkspaceType,
                Entry: definition.Entry.ToString(),
                Goal: definition.Goal,
                Status: "failed",
                Tags: definition.Tags,
                PackSuccess: false,
                CrashFree: false,
                PartialPack: false,
                HasUnknownGuidance: false,
                FalseConfidenceDetected: false,
                Deterministic: false,
                DurationMilliseconds: stopwatch.ElapsedMilliseconds,
                FirstUsefulMapTimeMs: null,
                FailureReason: exception.Message,
                SkipReason: null,
                DegradedDiagnosticCount: 0,
                RepresentativeChainCount: 0,
                DegradedReasonCount: 0,
                UnknownGuidance: []);
        }
    }

    internal static string CreateArtifactOutputRoot() =>
        Path.Combine(Path.GetTempPath(), "rulixa-local-quality-artifacts", Guid.NewGuid().ToString("N"));

    internal static string CreateRepositoryArtifactOutputRoot() =>
        QualityArtifactConventions.BuildDefaultOutputDirectory(RepositoryRoot);

    internal static async Task<IReadOnlyList<QualityCaseArtifact>> ExecuteDefinitionsAsync(
        IReadOnlyList<QualityCaseDefinition> definitions,
        CancellationToken cancellationToken = default)
    {
        var results = new List<QualityCaseArtifact>(definitions.Count);
        foreach (var definition in definitions)
        {
            results.Add(await ExecuteCaseAsync(definition, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    internal static string GetFixtureRoot(string fixtureName) =>
        Path.GetFullPath(Path.Combine(RepositoryRoot, "tests", "Rulixa.Plugin.WpfNet8.Tests", "Fixtures", fixtureName));

    internal static bool IsRootCase(QualityCaseDefinition definition) =>
        definition.Tags.Contains("root-case", StringComparer.OrdinalIgnoreCase);

    internal static string GetRepositoryRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static async Task<(WorkspaceScanResult ScanResult, ContextPack Pack)> BuildPackAsync(
        QualityCaseDefinition definition,
        CancellationToken cancellationToken)
    {
        var fileSystem = new WorkspaceFileSystem();
        var scanner = new WpfNet8WorkspaceScanner(fileSystem);
        var resolver = new ScanBackedEntryResolver();
        var extractor = new WpfNet8ContractExtractor(fileSystem);
        var buildPackUseCase = new BuildContextPackUseCase(extractor);

        var scanResult = await scanner.ScanAsync(definition.WorkspaceRoot).ConfigureAwait(false);
        var resolvedEntry = await resolver.ResolveAsync(definition.Entry, scanResult).ConfigureAwait(false);
        var pack = await buildPackUseCase.ExecuteAsync(
                definition.WorkspaceRoot,
                scanResult,
                definition.Entry,
                resolvedEntry,
                definition.Goal,
                Budget.Default)
            .ConfigureAwait(false);
        return (scanResult, pack);
    }

    private static async Task<bool> IsDeterministicAsync(
        QualityCaseDefinition definition,
        (WorkspaceScanResult ScanResult, ContextPack Pack) firstRun,
        CancellationToken cancellationToken)
    {
        var secondRun = await BuildPackAsync(definition, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(new
            {
                Contracts = firstRun.Pack.Contracts.Select(static contract => new { contract.Title, contract.Summary }).ToArray(),
                Indexes = firstRun.Pack.Indexes.Select(static index => new { index.Title, index.Lines }).ToArray(),
                Unknowns = firstRun.Pack.Unknowns.Select(static unknown => new { unknown.Code, unknown.Candidates }).ToArray()
            })
            == JsonSerializer.Serialize(new
            {
                Contracts = secondRun.Pack.Contracts.Select(static contract => new { contract.Title, contract.Summary }).ToArray(),
                Indexes = secondRun.Pack.Indexes.Select(static index => new { index.Title, index.Lines }).ToArray(),
                Unknowns = secondRun.Pack.Unknowns.Select(static unknown => new { unknown.Code, unknown.Candidates }).ToArray()
            });
    }

    private static bool DetectFalseConfidence(QualityCaseDefinition definition, ContextPack pack)
    {
        if (definition.ExpectUnknownGuidance && !pack.Unknowns.Any(static unknown => unknown.Candidates.Count > 0))
        {
            return true;
        }

        foreach (var disallowedTitle in definition.DisallowedRepresentativeSections)
        {
            if (pack.Indexes.Any(index => index.Title == disallowedTitle && index.Lines.Count > 0))
            {
                return true;
            }
        }

        if (IsRootCase(definition))
        {
            var systemPack = pack.Contracts.FirstOrDefault(static contract => contract.Title == "System Pack");
            if (systemPack is null)
            {
                return true;
            }

            foreach (var family in definition.RequiredFamilies)
            {
                if (!systemPack.Summary.Contains(family, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            if (definition.RequireCenterState && !systemPack.Summary.Contains("ProjectDocument", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetSkipReason(QualityCaseDefinition definition)
    {
        if (!definition.Tags.Contains("optional-smoke", StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!OperatingSystem.IsWindows())
        {
            return "windows-only";
        }

        if (!Directory.Exists(definition.WorkspaceRoot))
        {
            return "workspace-missing";
        }

        var flag = Environment.GetEnvironmentVariable(AssessMeisterOptionalSmokeTests.EnableEnvironmentVariableName);
        if (!string.Equals(flag, "1", StringComparison.Ordinal))
        {
            return "smoke-env-disabled";
        }

        return null;
    }

    private static UnknownGuidanceArtifact ToUnknownGuidance(Diagnostic diagnostic)
    {
        var firstCandidate = diagnostic.Candidates.FirstOrDefault();
        return new UnknownGuidanceArtifact(
            Code: diagnostic.Code,
            Family: InferFamily(diagnostic),
            CandidateCount: diagnostic.Candidates.Count,
            FirstCandidate: firstCandidate);
    }

    private static string InferFamily(Diagnostic diagnostic)
    {
        if (diagnostic.Candidates.Any(static candidate =>
                candidate.Contains("Algorithm", StringComparison.OrdinalIgnoreCase)
                || candidate.Contains("Runner", StringComparison.OrdinalIgnoreCase)
                || candidate.Contains("Pipeline", StringComparison.OrdinalIgnoreCase)))
        {
            return "Algorithm";
        }

        if (diagnostic.Candidates.Any(static candidate =>
                candidate.Contains("Analyzer", StringComparison.OrdinalIgnoreCase)))
        {
            return "Analyzer";
        }

        if (diagnostic.Candidates.Any(static candidate => candidate.Contains("Drafting", StringComparison.OrdinalIgnoreCase)))
        {
            return "Drafting";
        }

        if (diagnostic.Candidates.Any(static candidate => candidate.Contains("Setting", StringComparison.OrdinalIgnoreCase)))
        {
            return "Settings";
        }

        if (diagnostic.Candidates.Any(static candidate =>
                candidate.Contains("Report", StringComparison.OrdinalIgnoreCase)
                || candidate.Contains("Export", StringComparison.OrdinalIgnoreCase)))
        {
            return "Report/Export";
        }

        if (diagnostic.Candidates.Any(static candidate =>
                candidate.Contains("ThreeD", StringComparison.OrdinalIgnoreCase)
                || candidate.Contains("3D", StringComparison.OrdinalIgnoreCase)))
        {
            return "3D";
        }

        if (diagnostic.Code.Contains("architecture", StringComparison.OrdinalIgnoreCase))
        {
            return "Architecture";
        }

        if (diagnostic.Candidates.Any(static candidate =>
                candidate.Contains("Repository", StringComparison.OrdinalIgnoreCase)
                || candidate.Contains("Query", StringComparison.OrdinalIgnoreCase)
                || candidate.Contains("Store", StringComparison.OrdinalIgnoreCase)
                || candidate.Contains("Saver", StringComparison.OrdinalIgnoreCase)))
        {
            return "Persistence";
        }

        return "Shell";
    }

    private static int CountRepresentativeChains(ContextPack pack) =>
        pack.Indexes.Sum(static index => index.Lines.Count)
        + pack.Contracts.Count(static contract => contract.Title != "System Pack");

    private static int CountDegradedReasons(WorkspaceScanResult scanResult, ContextPack pack) =>
        scanResult.Diagnostics.Count(static diagnostic => diagnostic.Severity != DiagnosticSeverity.Error)
        + pack.Unknowns.Count;
}

internal sealed record QualityCaseDefinition(
    string CaseId,
    string CorpusName,
    string WorkspaceType,
    string WorkspaceRoot,
    Entry Entry,
    string Goal,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> RequiredFamilies,
    bool RequireCenterState,
    bool ExpectUnknownGuidance,
    IReadOnlyList<string> DisallowedRepresentativeSections);
