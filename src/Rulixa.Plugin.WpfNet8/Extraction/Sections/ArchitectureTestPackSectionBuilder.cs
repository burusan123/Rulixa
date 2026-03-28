using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class ArchitectureTestPackSectionBuilder
{
    private readonly IWorkspaceFileSystem workspaceFileSystem;

    internal ArchitectureTestPackSectionBuilder(IWorkspaceFileSystem workspaceFileSystem)
    {
        this.workspaceFileSystem = workspaceFileSystem ?? throw new ArgumentNullException(nameof(workspaceFileSystem));
    }

    internal async Task AddAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        RelevantPackContext relevantContext,
        ICollection<Contract> contracts,
        ICollection<IndexSection> indexes,
        ICollection<FileSelectionCandidate> fileCandidates,
        ICollection<Diagnostic> unknowns,
        CancellationToken cancellationToken)
    {
        var tests = await DiscoverTestsAsync(workspaceRoot, scanResult, cancellationToken).ConfigureAwait(false);
        if (tests.Count == 0)
        {
            if (relevantContext.GoalProfile.HasCategory("architecture"))
            {
                unknowns.Add(new Diagnostic(
                    "architecture-tests.unresolved",
                    "No architecture or regression-style tests were detected from the scanned workspace.",
                    null,
                    DiagnosticSeverity.Info,
                    []));
            }

            return;
        }

        indexes.Add(new IndexSection("Architecture Tests", tests.Select(static test => test.ToIndexLine()).ToArray()));
        contracts.Add(new Contract(
            ContractKind.DependencyInjection,
            "Architecture Tests",
            BuildSummary(tests),
            tests.Select(static test => test.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            []));

        foreach (var filePath in tests.Select(static test => test.FilePath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            fileCandidates.Add(new FileSelectionCandidate(filePath, "architecture-test", 32, false));
        }
    }

    private async Task<IReadOnlyList<ArchitectureTestSignal>> DiscoverTestsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        CancellationToken cancellationToken)
    {
        var candidates = scanResult.Files
            .Where(static file =>
                file.Path.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)
                || file.Path.Contains("/tests/", StringComparison.OrdinalIgnoreCase))
            .Where(static file => file.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var signals = new List<ArchitectureTestSignal>();

        foreach (var candidate in candidates)
        {
            var source = await ReadSourceAsync(workspaceRoot, candidate.Path, cancellationToken).ConfigureAwait(false);
            var descriptors = ExtractDescriptors(candidate.Path, source);
            if (descriptors.Count == 0)
            {
                continue;
            }

            signals.Add(new ArchitectureTestSignal(candidate.Path, descriptors));
        }

        return signals;
    }

    private static IReadOnlyList<string> ExtractDescriptors(string path, string source)
    {
        var descriptors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddIfContains(path, source, descriptors, "Architecture");
        AddIfContains(path, source, descriptors, "Golden");
        AddIfContains(path, source, descriptors, "Regression");
        AddIfContains(path, source, descriptors, "layer");
        AddIfContains(path, source, descriptors, "dependency");
        AddIfContains(path, source, descriptors, "Should");
        return descriptors.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void AddIfContains(string path, string source, ISet<string> descriptors, string token)
    {
        if (path.Contains(token, StringComparison.OrdinalIgnoreCase)
            || source.Contains(token, StringComparison.OrdinalIgnoreCase))
        {
            descriptors.Add(token);
        }
    }

    private static string BuildSummary(IReadOnlyList<ArchitectureTestSignal> tests)
    {
        var samples = tests.Take(3).Select(static test => test.ToIndexLine()).ToArray();
        return $"Discovered {tests.Count} architecture or regression test files. Examples: {string.Join(" / ", samples)}";
    }

    private async Task<string> ReadSourceAsync(
        string workspaceRoot,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return await workspaceFileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
    }

    private sealed record ArchitectureTestSignal(
        string FilePath,
        IReadOnlyList<string> Descriptors)
    {
        internal string ToIndexLine() => $"{Path.GetFileName(FilePath)} -> {string.Join(" / ", Descriptors)}";
    }
}
