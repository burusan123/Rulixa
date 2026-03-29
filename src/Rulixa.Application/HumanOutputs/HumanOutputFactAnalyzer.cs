using System.Text.RegularExpressions;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.HumanOutputs;

internal sealed partial class HumanOutputFactAnalyzer
{
    private static readonly string[] UiNoiseTokens =
    [
        "Overlay",
        "Prompt",
        "Renderer"
    ];

    private static readonly string[] WorkflowSectionTitles =
    [
        "Workflow",
        "Navigation"
    ];

    public HumanOutputFactSet Analyze(
        ContextPack contextPack,
        WorkspaceScanResult scanResult,
        HumanOutputRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(contextPack);
        ArgumentNullException.ThrowIfNull(scanResult);
        ArgumentNullException.ThrowIfNull(options);

        var systemSummary = contextPack.Contracts.FirstOrDefault(static contract =>
            string.Equals(contract.Title, "System Pack", StringComparison.Ordinal))?.Summary;
        var centerStates = ExtractCenterStates(contextPack, systemSummary);
        var workflowLines = ExtractWorkflowLines(contextPack);
        var persistenceLines = ExtractPersistenceLines(contextPack);
        var externalAssetLines = ExtractExternalAssetLines(contextPack);
        var evidenceSources = ExtractEvidenceSources(contextPack);
        var nextCandidates = ExtractNextCandidates(contextPack.Unknowns, contextPack.SelectedFiles);
        var observedFacts = ExtractObservedFacts(contextPack, scanResult);
        var dependencySeams = ExtractDependencySeams(contextPack);
        var architecturalConstraints = ExtractArchitecturalConstraints(contextPack);
        var knownUnknowns = ExtractKnownUnknowns(contextPack.Unknowns);
        var riskLines = ExtractRiskLines(contextPack, scanResult, nextCandidates);

        return new HumanOutputFactSet(
            Entry: contextPack.Entry.ToString(),
            ResolvedKind: contextPack.ResolvedEntry.ResolvedKind.ToString(),
            ResolvedPath: NormalizePath(contextPack.ResolvedEntry.ResolvedPath),
            ResolvedSymbol: contextPack.ResolvedEntry.Symbol,
            Goal: contextPack.Goal,
            SystemSummary: systemSummary,
            CenterStates: centerStates,
            WorkflowLines: workflowLines,
            PersistenceLines: persistenceLines,
            ExternalAssetLines: externalAssetLines,
            ObservedFacts: observedFacts,
            EvidenceSources: evidenceSources,
            DependencySeams: dependencySeams,
            ArchitecturalConstraints: architecturalConstraints,
            KnownUnknowns: knownUnknowns,
            RiskLines: riskLines,
            NextCandidates: nextCandidates,
            DegradedDiagnosticCount: scanResult.Diagnostics.Count(static diagnostic => diagnostic.Severity != DiagnosticSeverity.Error),
            RepresentativeChainCount: contextPack.Indexes.Sum(static index => index.Lines.Count),
            EvidenceDirectory: options.EvidenceDirectory,
            CompareEvidenceReference: options.CompareEvidenceReference);
    }

    private static IReadOnlyList<string> ExtractCenterStates(ContextPack contextPack, string? systemSummary)
    {
        var texts = BuildFactSourceTexts(contextPack, systemSummary);
        var states = new HashSet<string>(StringComparer.Ordinal);

        foreach (var text in texts)
        {
            foreach (Match match in CandidateSymbolPattern().Matches(text))
            {
                var value = match.Value;
                if (value.EndsWith("ViewModel", StringComparison.Ordinal)
                    || value.EndsWith("Window", StringComparison.Ordinal)
                    || string.Equals(value, "DataContext", StringComparison.Ordinal))
                {
                    continue;
                }

                states.Add(value);
            }
        }

        return states.OrderBy(static item => item, StringComparer.Ordinal).Take(5).ToArray();
    }

    private static IReadOnlyList<string> ExtractWorkflowLines(ContextPack contextPack)
    {
        var workflowSections = contextPack.Indexes
            .Where(index => WorkflowSectionTitles.Contains(index.Title, StringComparer.Ordinal)
                || index.Title.Contains("Workflow", StringComparison.OrdinalIgnoreCase))
            .SelectMany(static index => index.Lines)
            .Distinct(StringComparer.Ordinal)
            .Take(6)
            .ToArray();

        if (workflowSections.Length > 0)
        {
            return workflowSections;
        }

        return contextPack.Contracts
            .Where(static contract => string.Equals(contract.Title, "Workflow", StringComparison.Ordinal))
            .Select(static contract => contract.Summary)
            .Distinct(StringComparer.Ordinal)
            .Take(4)
            .ToArray();
    }

    private static IReadOnlyList<string> ExtractPersistenceLines(ContextPack contextPack)
    {
        var fromIndexes = contextPack.Indexes
            .Where(static index => string.Equals(index.Title, "Persistence", StringComparison.Ordinal))
            .SelectMany(static index => index.Lines)
            .Distinct(StringComparer.Ordinal)
            .Take(5)
            .ToArray();

        if (fromIndexes.Length > 0)
        {
            return fromIndexes;
        }

        return contextPack.Contracts
            .Where(contract =>
                string.Equals(contract.Title, "Persistence", StringComparison.Ordinal)
                || contract.Summary.Contains("Repository", StringComparison.Ordinal)
                || contract.Summary.Contains("Query", StringComparison.Ordinal)
                || contract.Summary.Contains("Store", StringComparison.Ordinal))
            .Select(static contract => contract.Summary)
            .Distinct(StringComparer.Ordinal)
            .Take(4)
            .ToArray();
    }

    private static IReadOnlyList<string> ExtractExternalAssetLines(ContextPack contextPack)
    {
        var fromIndexes = contextPack.Indexes
            .Where(static index => string.Equals(index.Title, "External Assets", StringComparison.Ordinal))
            .SelectMany(static index => index.Lines)
            .Distinct(StringComparer.Ordinal)
            .Take(5)
            .ToArray();

        if (fromIndexes.Length > 0)
        {
            return fromIndexes;
        }

        return contextPack.Contracts
            .Where(contract =>
                string.Equals(contract.Title, "External Assets", StringComparison.Ordinal)
                || contract.Summary.Contains("Excel", StringComparison.OrdinalIgnoreCase)
                || contract.Summary.Contains("Template", StringComparison.OrdinalIgnoreCase)
                || contract.Summary.Contains("Asset", StringComparison.OrdinalIgnoreCase))
            .Select(static contract => contract.Summary)
            .Distinct(StringComparer.Ordinal)
            .Take(4)
            .ToArray();
    }

    private static IReadOnlyList<string> ExtractObservedFacts(ContextPack contextPack, WorkspaceScanResult scanResult)
    {
        var facts = new List<string>
        {
            $"断定: entry は `{contextPack.Entry}` です。",
            $"断定: 解決種別は `{contextPack.ResolvedEntry.ResolvedKind}` です。"
        };

        if (!string.IsNullOrWhiteSpace(contextPack.ResolvedEntry.Symbol))
        {
            facts.Add($"断定: 解決 symbol は `{contextPack.ResolvedEntry.Symbol}` です。");
        }

        if (!string.IsNullOrWhiteSpace(contextPack.ResolvedEntry.ResolvedPath))
        {
            facts.Add($"断定: 解決 path は `{NormalizePath(contextPack.ResolvedEntry.ResolvedPath)}` です。");
        }

        foreach (var contract in contextPack.Contracts
                     .Where(static contract => !string.Equals(contract.Title, "System Pack", StringComparison.Ordinal))
                     .Take(4))
        {
            facts.Add($"断定: {contract.Title}: {contract.Summary}");
        }

        if (scanResult.Diagnostics.Count > 0)
        {
            facts.Add($"推定: scan diagnostics は `{scanResult.Diagnostics.Count}` 件あります。");
        }

        return facts;
    }

    private static IReadOnlyList<string> ExtractEvidenceSources(ContextPack contextPack)
    {
        var sources = new List<string>();

        foreach (var file in contextPack.SelectedFiles.Take(5))
        {
            sources.Add($"file: `{NormalizePath(file.Path)}` ({file.Reason})");
        }

        foreach (var snippet in contextPack.SelectedSnippets.Take(5))
        {
            sources.Add($"snippet: `{NormalizePath(snippet.Path)}:{snippet.StartLine}-{snippet.EndLine}` ({snippet.Reason})");
        }

        return sources
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> ExtractDependencySeams(ContextPack contextPack)
    {
        var seamReasons = new[]
        {
            "dependency-injection",
            "root-binding-source",
            "navigation-update",
            "dialog-service",
            "dialog-window"
        };

        var seams = contextPack.SelectedSnippets
            .Where(snippet => seamReasons.Contains(snippet.Reason, StringComparer.Ordinal))
            .Select(snippet => $"断定: `{NormalizePath(snippet.Path)}:{snippet.StartLine}-{snippet.EndLine}` は `{snippet.Reason}` の seam です。")
            .Distinct(StringComparer.Ordinal)
            .Take(5)
            .ToArray();

        if (seams.Length > 0)
        {
            return seams;
        }

        return contextPack.Contracts
            .Where(contract => contract.Kind is ContractKind.DependencyInjection or ContractKind.DialogActivation or ContractKind.Navigation)
            .Select(static contract => $"断定: {contract.Title}: {contract.Summary}")
            .Distinct(StringComparer.Ordinal)
            .Take(4)
            .ToArray();
    }

    private static IReadOnlyList<string> ExtractArchitecturalConstraints(ContextPack contextPack)
    {
        var fromIndexes = contextPack.Indexes
            .Where(static index => string.Equals(index.Title, "Architecture Tests", StringComparison.Ordinal))
            .SelectMany(static index => index.Lines)
            .Distinct(StringComparer.Ordinal)
            .Take(5)
            .ToArray();

        if (fromIndexes.Length > 0)
        {
            return fromIndexes.Select(static line => $"断定: {line}").ToArray();
        }

        var fromContracts = contextPack.Contracts
            .Where(static contract => string.Equals(contract.Title, "Architecture Tests", StringComparison.Ordinal))
            .Select(static contract => $"断定: {contract.Summary}")
            .Distinct(StringComparer.Ordinal)
            .Take(4)
            .ToArray();

        return fromContracts.Length > 0
            ? fromContracts
            : ["unknown: Architecture constraints は pack だけでは確定できません。"];
    }

    private static IReadOnlyList<string> ExtractKnownUnknowns(IReadOnlyList<Diagnostic> unknowns) =>
        unknowns.Count == 0
            ? ["断定: 既知の unknown はありません。"]
            : unknowns.Select(diagnostic => $"unknown: `{diagnostic.Code}` {TrimKnownPrefix(diagnostic.Message)}")
                .Take(6)
                .ToArray();

    private static IReadOnlyList<string> ExtractRiskLines(
        ContextPack contextPack,
        WorkspaceScanResult scanResult,
        IReadOnlyList<string> nextCandidates)
    {
        var risks = new List<string>();
        var degradedDiagnostics = scanResult.Diagnostics.Count(static diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
        if (degradedDiagnostics > 0)
        {
            risks.Add($"推定: degraded diagnostics は `{degradedDiagnostics}` 件です。");
        }

        if (contextPack.Unknowns.Count > 0)
        {
            risks.Add($"unknown: pack に未確定事項が `{contextPack.Unknowns.Count}` 件あります。");
        }

        if (nextCandidates.Count == 0 && contextPack.Unknowns.Count > 0)
        {
            risks.Add("unknown: 次に読む候補を十分に特定できていません。");
        }

        return risks.Count == 0
            ? ["断定: 現状では追加のリスク signal は見えていません。"]
            : risks;
    }

    private static IReadOnlyList<string> ExtractNextCandidates(
        IReadOnlyList<Diagnostic> unknowns,
        IReadOnlyList<SelectedFile> selectedFiles)
    {
        var candidates = unknowns
            .SelectMany(static diagnostic => diagnostic.Candidates)
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
            .Where(candidate => !UiNoiseTokens.Any(token => candidate.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.Ordinal)
            .Take(5)
            .ToList();

        if (candidates.Count > 0)
        {
            return candidates;
        }

        return selectedFiles
            .OrderByDescending(static file => file.IsRequired)
            .ThenBy(static file => file.Priority)
            .Select(static file => NormalizePath(file.Path))
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .Take(5)
            .ToArray();
    }

    private static IReadOnlyList<string> BuildFactSourceTexts(ContextPack contextPack, string? systemSummary)
    {
        var texts = new List<string>();
        if (!string.IsNullOrWhiteSpace(systemSummary))
        {
            texts.Add(systemSummary);
        }

        texts.AddRange(contextPack.Contracts.Select(static contract => contract.Summary));
        texts.AddRange(contextPack.Indexes.SelectMany(static index => index.Lines));
        texts.AddRange(contextPack.SelectedSnippets.Select(static snippet => snippet.Content));
        return texts;
    }

    private static string? NormalizePath(string? path) =>
        path?.Replace('\\', '/').TrimStart('.').TrimStart('/');

    private static string TrimKnownPrefix(string message)
    {
        const string prefix = "既知の限界: ";
        return message.StartsWith(prefix, StringComparison.Ordinal)
            ? message[prefix.Length..]
            : message;
    }

    [GeneratedRegex(@"\b[A-Z][A-Za-z0-9]+(?:Document|State|Info|Context|Model)\b", RegexOptions.CultureInvariant)]
    private static partial Regex CandidateSymbolPattern();
}
