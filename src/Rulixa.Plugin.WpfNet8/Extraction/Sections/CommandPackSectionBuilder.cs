using System.Text.RegularExpressions;
using Rulixa.Application.Ports;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class CommandPackSectionBuilder
{
    private const int CommandSummaryThreshold = 6;
    private const int MaxDetailedCommandsWhenSummarized = 3;

    private static readonly IReadOnlyDictionary<string, string[]> GoalAliases = new Dictionary<string, string[]>
    {
        ["setting"] = ["setting", "settings", "\u8A2D\u5B9A"],
        ["drafting"] = ["drafting", "\u4F5C\u56F3"],
        ["license"] = ["license", "\u30E9\u30A4\u30BB\u30F3\u30B9"],
        ["import"] = ["import", "\u30A4\u30F3\u30DD\u30FC\u30C8"],
        ["export"] = ["export", "\u30A8\u30AF\u30B9\u30DD\u30FC\u30C8"],
        ["share"] = ["share", "\u5171\u6709"],
        ["result"] = ["result", "results", "output", "\u7D50\u679C", "\u51FA\u529B"],
        ["project"] = ["project", "projects", "\u30D7\u30ED\u30B8\u30A7\u30AF\u30C8"],
        ["open"] = ["open", "\u958B\u304F", "\u8D77\u52D5"],
        ["save"] = ["save", "\u4FDD\u5B58"],
        ["reset"] = ["reset", "\u521D\u671F\u5316", "\u30EA\u30BB\u30C3\u30C8"],
        ["new"] = ["new", "\u65B0\u898F"]
    };

    internal static async Task AddContractsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        string goal,
        IWorkspaceFileSystem workspaceFileSystem,
        CSharpSnippetCandidateFactory snippetFactory,
        ICollection<Contract> contracts,
        ICollection<SnippetSelectionCandidate> snippetCandidates,
        ICollection<FileSelectionCandidate> fileCandidates,
        ICollection<PackDecisionTrace> decisionTraces,
        CancellationToken cancellationToken)
    {
        var commands = FindCommands(scanResult, resolvedEntry);
        var impactAnalyzer = new CommandImpactAnalyzer(workspaceFileSystem);
        var commandDetails = await impactAnalyzer
            .AnalyzeAsync(workspaceRoot, scanResult, commands, cancellationToken)
            .ConfigureAwait(false);
        var summarized = commands.Length > CommandSummaryThreshold;
        var selectionResult = AnalyzeCommandSelection(commandDetails, goal);
        var detailedCommands = summarized ? selectionResult.SelectedCommandDetails : commandDetails.ToArray();

        if (summarized)
        {
            contracts.Add(BuildSummaryContract(commands));
        }

        foreach (var decisionTrace in selectionResult.DecisionTraces)
        {
            decisionTraces.Add(decisionTrace);
        }

        foreach (var commandDetailsItem in detailedCommands)
        {
            contracts.Add(BuildDetailedContract(commandDetailsItem));
        }

        await AddCommandSnippetsAsync(
                workspaceRoot,
                scanResult,
                summarized ? detailedCommands : commandDetails,
                snippetFactory,
                snippetCandidates,
                cancellationToken)
            .ConfigureAwait(false);

        foreach (var command in commands)
        {
            var viewModelFile = scanResult.Symbols.FirstOrDefault(symbol =>
                string.Equals(symbol.QualifiedName, command.ViewModelSymbol, StringComparison.OrdinalIgnoreCase))?.FilePath;
            if (!string.IsNullOrWhiteSpace(viewModelFile))
            {
                fileCandidates.Add(new FileSelectionCandidate(viewModelFile, "command-viewmodel", 6, true));
            }

            foreach (var boundView in command.BoundViews)
            {
                fileCandidates.Add(new FileSelectionCandidate(boundView, "command-bound-view", 4, true));
                PackExtractionConventions.AddCodeBehindIfPresent(scanResult, boundView, fileCandidates, 5, true);
            }
        }

        foreach (var impact in (summarized ? detailedCommands : commandDetails).SelectMany(static details => details.DirectImpacts))
        {
            if (!string.IsNullOrWhiteSpace(impact.SourceFilePath))
            {
                fileCandidates.Add(new FileSelectionCandidate(impact.SourceFilePath, "command-impact", 18, false));
            }
        }

        var delegateCommandFile = scanResult.Files.FirstOrDefault(file =>
            Path.GetFileName(file.Path).Equals("DelegateCommand.cs", StringComparison.OrdinalIgnoreCase));
        if (delegateCommandFile is not null && commands.Length > 0)
        {
            fileCandidates.Add(new FileSelectionCandidate(delegateCommandFile.Path, "command-support", 30, true));
        }
    }

    internal static async Task<IndexSection> BuildIndexAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        string goal,
        IWorkspaceFileSystem workspaceFileSystem,
        CancellationToken cancellationToken)
    {
        var commands = FindCommands(scanResult, resolvedEntry);
        var impactAnalyzer = new CommandImpactAnalyzer(workspaceFileSystem);
        var commandDetails = await impactAnalyzer
            .AnalyzeAsync(workspaceRoot, scanResult, commands, cancellationToken)
            .ConfigureAwait(false);
        var detailedCommands = commandDetails.Count > CommandSummaryThreshold
            ? AnalyzeCommandSelection(commandDetails, goal).SelectedCommandDetails
            : commandDetails.ToArray();

        if (commands.Length > CommandSummaryThreshold)
        {
            var sampleNames = commands
                .Select(static command => command.PropertyName)
                .Take(3)
                .ToArray();
            var lines = new List<string>
            {
                $"{commands.Length} \u4EF6\u306E\u30B3\u30DE\u30F3\u30C9\u5B9A\u7FA9 (\u4F8B: {string.Join(", ", sampleNames)})"
            };
            lines.AddRange(detailedCommands.Select(BuildDetailedIndexLine));
            return new IndexSection("\u30B3\u30DE\u30F3\u30C9", lines);
        }

        return new IndexSection(
            "\u30B3\u30DE\u30F3\u30C9",
            commandDetails.Select(BuildDetailedIndexLine).ToArray());
    }

    private static Contract BuildSummaryContract(IReadOnlyList<CommandBinding> commands)
    {
        var sampleNames = commands
            .Select(static command => command.PropertyName)
            .Take(3)
            .ToArray();
        var summary = $"{commands[0].ViewModelSymbol} \u306B\u306F {commands.Count} \u4EF6\u306E\u30B3\u30DE\u30F3\u30C9\u5B9A\u7FA9\u304C\u3042\u308A\u307E\u3059\u3002\u4F8B: {string.Join(", ", sampleNames)}\u3002";
        return new Contract(
            ContractKind.Command,
            "\u30B3\u30DE\u30F3\u30C9\u8981\u7D04",
            summary,
            commands.SelectMany(static command => command.BoundViews).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            commands.Select(static command => command.ExecuteSymbol).ToArray());
    }

    private static Contract BuildDetailedContract(CommandImpactDetails commandDetails)
    {
        var command = commandDetails.Command;
        var summaryParts = new List<string>
        {
            $"{command.PropertyName} \u306F {BuildExecuteDisplay(commandDetails)} \u3092\u5B9F\u884C\u3057\u307E\u3059\u3002"
        };

        var routeSummaries = BuildRouteSummaries(commandDetails);
        if (routeSummaries.Count > 0)
        {
            summaryParts.AddRange(routeSummaries);
        }

        return new Contract(
            ContractKind.Command,
            command.PropertyName,
            string.Join(" ", summaryParts),
            command.BoundViews,
            BuildRelatedSymbols(commandDetails));
    }

    private static string[] BuildRelatedSymbols(CommandImpactDetails commandDetails) =>
        new[] { commandDetails.Command.ViewModelSymbol, commandDetails.Command.ExecuteSymbol }
            .Concat(commandDetails.HelperInvocations.Select(helper =>
                $"{commandDetails.Command.ViewModelSymbol}.{helper.HelperMethodName}"))
            .Concat(commandDetails.DirectImpacts.Select(static impact => impact.DisplaySymbol))
            .Concat(commandDetails.DirectImpacts
                .Where(static impact => !string.IsNullOrWhiteSpace(impact.DialogWindowSymbol))
                .Select(static impact => impact.DialogWindowSymbol!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IReadOnlyList<string> BuildRouteSummaries(CommandImpactDetails commandDetails)
    {
        var executeMethodName = GetExecuteMethodName(commandDetails.Command);
        var routeSummaries = new List<string>();

        var directRouteImpacts = GetRouteImpacts(commandDetails, executeMethodName);
        if (directRouteImpacts.Count > 0)
        {
            routeSummaries.Add(BuildRouteSummary(null, directRouteImpacts, commandDetails.Command.ViewModelSymbol));
        }

        foreach (var helperInvocation in commandDetails.HelperInvocations.OrderBy(static helper => helper.HelperMethodName, StringComparer.OrdinalIgnoreCase))
        {
            var helperImpacts = GetRouteImpacts(commandDetails, helperInvocation.HelperMethodName);
            if (helperImpacts.Count == 0)
            {
                continue;
            }

            routeSummaries.Add(BuildRouteSummary(helperInvocation.HelperMethodName, helperImpacts, commandDetails.Command.ViewModelSymbol));
        }

        return routeSummaries;
    }

    private static string BuildRouteSummary(
        string? helperMethodName,
        IReadOnlyList<DirectCommandImpact> impacts,
        string viewModelSymbol)
    {
        var callSummary = $"{string.Join(" / ", impacts.Select(static impact => impact.DisplaySymbol))} \u3092\u547C\u3073\u51FA\u3057\u307E\u3059\u3002";
        var prefix = string.IsNullOrWhiteSpace(helperMethodName)
            ? string.Empty
            : $"{PackExtractionConventions.GetSimpleTypeName(viewModelSymbol)}.{helperMethodName}(...) \u3092\u7D4C\u7531\u3057\u3066 ";
        var dialogSummary = BuildDialogSummary(impacts);
        return string.IsNullOrWhiteSpace(dialogSummary)
            ? $"{prefix}{callSummary}"
            : $"{prefix}{callSummary} {dialogSummary}";
    }

    private static string BuildDialogSummary(IReadOnlyList<DirectCommandImpact> impacts)
    {
        var dialogs = impacts
            .Where(static impact => !string.IsNullOrWhiteSpace(impact.DialogWindowSymbol) && !string.IsNullOrWhiteSpace(impact.ActivationKind))
            .Select(static impact => $"{impact.DialogWindowSymbol}({impact.ActivationKind})")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return dialogs.Length == 0
            ? string.Empty
            : $"\u6700\u7D42\u7684\u306B {string.Join(" / ", dialogs)} \u304C\u8D77\u52D5\u3055\u308C\u307E\u3059\u3002";
    }

    private static string BuildDetailedIndexLine(CommandImpactDetails commandDetails)
    {
        var executeDisplay = BuildExecuteDisplay(commandDetails);
        var routeSegments = new List<string>();
        var executeMethodName = GetExecuteMethodName(commandDetails.Command);

        var directRouteImpacts = GetRouteImpacts(commandDetails, executeMethodName);
        if (directRouteImpacts.Count > 0)
        {
            routeSegments.Add($"{executeDisplay} -> {string.Join(" / ", directRouteImpacts.Select(FormatImpactIndexPart))}");
        }

        foreach (var helperInvocation in commandDetails.HelperInvocations.OrderBy(static helper => helper.HelperMethodName, StringComparer.OrdinalIgnoreCase))
        {
            var helperImpacts = GetRouteImpacts(commandDetails, helperInvocation.HelperMethodName);
            if (helperImpacts.Count == 0)
            {
                continue;
            }

            routeSegments.Add(
                $"{executeDisplay} -> {PackExtractionConventions.GetSimpleTypeName(commandDetails.Command.ViewModelSymbol)}.{helperInvocation.HelperMethodName}(...) -> {string.Join(" / ", helperImpacts.Select(FormatImpactIndexPart))}");
        }

        if (routeSegments.Count == 0)
        {
            routeSegments.Add(executeDisplay);
        }

        return $"{commandDetails.Command.PropertyName} -> {string.Join(" / ", routeSegments)}";
    }

    private static string FormatImpactIndexPart(DirectCommandImpact impact) =>
        string.IsNullOrWhiteSpace(impact.DialogWindowSymbol) || string.IsNullOrWhiteSpace(impact.ActivationKind)
            ? impact.DisplaySymbol
            : $"{impact.DisplaySymbol} -> {impact.DialogWindowSymbol} ({impact.ActivationKind})";

    private static async Task AddCommandSnippetsAsync(
        string workspaceRoot,
        WorkspaceScanResult scanResult,
        IReadOnlyList<CommandImpactDetails> commandDetails,
        CSharpSnippetCandidateFactory snippetFactory,
        ICollection<SnippetSelectionCandidate> snippetCandidates,
        CancellationToken cancellationToken)
    {
        foreach (var details in commandDetails)
        {
            if (string.IsNullOrWhiteSpace(details.ViewModelFilePath)
                || !PackExtractionConventions.ShouldCreateSnippet(scanResult, details.ViewModelFilePath))
            {
                continue;
            }

            var executeMethodName = GetExecuteMethodName(details.Command);
            var executeSnippet = await snippetFactory
                .CreateMethodSnippetAsync(
                    workspaceRoot,
                    details.ViewModelFilePath,
                    executeMethodName,
                    "command-viewmodel",
                    20,
                    false,
                    $"{executeMethodName}(...)",
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
            if (executeSnippet is not null)
            {
                snippetCandidates.Add(executeSnippet);
            }

            foreach (var helperInvocation in details.HelperInvocations
                         .DistinctBy(static helper => helper.HelperMethodName, StringComparer.OrdinalIgnoreCase))
            {
                var helperSnippet = await snippetFactory
                    .CreateMethodSnippetAsync(
                        workspaceRoot,
                        helperInvocation.SourceFilePath,
                        helperInvocation.HelperMethodName,
                        "command-viewmodel",
                        21,
                        false,
                        $"{helperInvocation.HelperMethodName}(...)",
                        helperInvocation.BodyRange,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (helperSnippet is not null)
                {
                    snippetCandidates.Add(helperSnippet);
                }
            }

            foreach (var impact in details.DirectImpacts
                         .Where(static impact => !string.IsNullOrWhiteSpace(impact.SourceFilePath))
                         .DistinctBy(static impact => $"{impact.SourceFilePath}:{impact.MethodName}", StringComparer.OrdinalIgnoreCase))
            {
                if (!PackExtractionConventions.ShouldCreateSnippet(scanResult, impact.SourceFilePath!))
                {
                    continue;
                }

                var impactSnippet = await snippetFactory
                    .CreateMethodSnippetAsync(
                        workspaceRoot,
                        impact.SourceFilePath!,
                        impact.MethodName,
                        "command-impact",
                        22,
                        false,
                        $"{impact.MethodName}(...)",
                        null,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (impactSnippet is not null)
                {
                    snippetCandidates.Add(impactSnippet);
                }
            }
        }
    }

    private static CommandSelectionResult AnalyzeCommandSelection(
        IReadOnlyList<CommandImpactDetails> commandDetails,
        string goal)
    {
        var goalTerms = ExtractGoalTerms(goal)
            .OrderBy(static term => term, StringComparer.Ordinal)
            .ToArray();
        var analyses = commandDetails
            .Select(details => BuildSelectionAnalysis(details, goalTerms, commandDetails.Count))
            .OrderByDescending(static analysis => analysis.Score)
            .ThenBy(static analysis => analysis.Details.Command.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        for (var index = 0; index < analyses.Length; index++)
        {
            analyses[index] = analyses[index] with { Rank = index + 1 };
        }

        if (commandDetails.Count <= CommandSummaryThreshold)
        {
            return new CommandSelectionResult(
                analyses.Select(BuildSelectedAllTrace).ToArray(),
                analyses.Select(static analysis => analysis.Details).ToArray());
        }

        var selectedAnalyses = analyses
            .Where(static analysis => analysis.Score > 0)
            .Take(MaxDetailedCommandsWhenSummarized)
            .ToArray();
        var selectedKeys = selectedAnalyses
            .Select(static analysis => analysis.Details.Command.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var traces = analyses
            .Select(analysis => BuildSummarizedTrace(
                analysis,
                selectedKeys.Contains(analysis.Details.Command.PropertyName)))
            .ToArray();

        return new CommandSelectionResult(
            traces,
            selectedAnalyses.Select(static analysis => analysis.Details).ToArray());
    }

    private static CommandSelectionAnalysis BuildSelectionAnalysis(
        CommandImpactDetails details,
        IReadOnlyList<string> goalTerms,
        int candidateCount)
    {
        var sourceTerms = new[]
        {
            new WeightedTerms("property-name", 3, ExtractIdentifierTerms(details.Command.PropertyName).ToArray()),
            new WeightedTerms("execute-method", 2, ExtractIdentifierTerms(GetExecuteMethodName(details.Command)).ToArray()),
            new WeightedTerms("helper-method", 2, details.HelperInvocations.SelectMany(static helper => ExtractIdentifierTerms(helper.HelperMethodName)).ToArray()),
            new WeightedTerms("impact-symbol", 2, details.DirectImpacts.SelectMany(static impact => ExtractIdentifierTerms(impact.DisplaySymbol)).ToArray()),
            new WeightedTerms("dialog-window", 2, details.DirectImpacts
                .Where(static impact => !string.IsNullOrWhiteSpace(impact.DialogWindowSymbol))
                .SelectMany(static impact => ExtractIdentifierTerms(impact.DialogWindowSymbol!))
                .ToArray())
        };

        var score = sourceTerms.Sum(source => CountMatches(goalTerms, source.Terms) * source.Weight);
        var matchedSources = sourceTerms
            .Select(source => new PackDecisionMatchedSource(
                source.Source,
                source.Terms
                    .Where(goalTerms.Contains)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static term => term, StringComparer.Ordinal)
                    .ToArray()))
            .Where(static source => source.Terms.Count > 0)
            .OrderBy(static source => source.Source, StringComparer.Ordinal)
            .ToArray();
        var matchedTerms = matchedSources
            .SelectMany(static source => source.Terms)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static term => term, StringComparer.Ordinal)
            .ToArray();

        return new CommandSelectionAnalysis(
            details,
            score,
            0,
            candidateCount,
            goalTerms,
            matchedTerms,
            matchedSources);
    }

    private static PackDecisionTrace BuildSelectedAllTrace(CommandSelectionAnalysis analysis) =>
        new(
            Category: "command-selection",
            ItemKey: analysis.Details.Command.PropertyName,
            DecisionKind: "selected-all",
            Summary: $"{analysis.Details.Command.PropertyName} は command 数が閾値以下のため詳細化対象として採用されます。",
            Score: analysis.Score,
            Rank: analysis.Rank,
            CandidateCount: analysis.CandidateCount,
            GoalTerms: analysis.GoalTerms,
            MatchedTerms: analysis.MatchedTerms,
            MatchedSources: analysis.MatchedSources);

    private static PackDecisionTrace BuildSummarizedTrace(CommandSelectionAnalysis analysis, bool selected)
    {
        var decisionKind = selected
            ? "selected-by-goal"
            : analysis.Score == 0
                ? "omitted-low-score"
                : "omitted-rank";
        var summary = decisionKind switch
        {
            "selected-by-goal" => $"{analysis.Details.Command.PropertyName} は goal と一致する term により詳細化対象として採用されます。",
            "omitted-low-score" => $"{analysis.Details.Command.PropertyName} は goal 一致 term がないため詳細化対象から外れます。",
            _ => $"{analysis.Details.Command.PropertyName} は goal 一致 term はありますが上位件数外のため詳細化対象から外れます。"
        };

        return new PackDecisionTrace(
            Category: "command-selection",
            ItemKey: analysis.Details.Command.PropertyName,
            DecisionKind: decisionKind,
            Summary: summary,
            Score: analysis.Score,
            Rank: analysis.Rank,
            CandidateCount: analysis.CandidateCount,
            GoalTerms: analysis.GoalTerms,
            MatchedTerms: analysis.MatchedTerms,
            MatchedSources: analysis.MatchedSources);
    }

    private static HashSet<string> ExtractGoalTerms(string goal)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (canonical, aliases) in GoalAliases)
        {
            if (aliases.Any(alias => goal.Contains(alias, StringComparison.OrdinalIgnoreCase)))
            {
                terms.Add(canonical);
            }
        }

        foreach (Match match in Regex.Matches(goal, "[A-Za-z0-9]+"))
        {
            terms.Add(Canonicalize(match.Value));
        }

        return terms;
    }

    private static IEnumerable<string> ExtractIdentifierTerms(string raw)
    {
        foreach (var segment in raw.Split(['.', '_'], StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (Match match in Regex.Matches(segment, "[A-Z]?[a-z]+|[A-Z]+(?![a-z])|[0-9]+"))
            {
                yield return Canonicalize(match.Value);
            }
        }
    }

    private static string Canonicalize(string value)
    {
        foreach (var (canonical, aliases) in GoalAliases)
        {
            if (aliases.Any(alias => string.Equals(alias, value, StringComparison.OrdinalIgnoreCase)))
            {
                return canonical;
            }
        }

        return value.ToLowerInvariant();
    }

    private static int CountMatches(IEnumerable<string> goalTerms, IEnumerable<string> candidateTerms)
    {
        var goalTermSet = goalTerms.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return candidateTerms.Count(goalTermSet.Contains);
    }

    private static IReadOnlyList<DirectCommandImpact> GetRouteImpacts(
        CommandImpactDetails details,
        string originMethodName) =>
        details.DirectImpacts
            .Where(impact => string.Equals(impact.OriginMethodName, originMethodName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

    private static string BuildExecuteDisplay(CommandImpactDetails details) =>
        $"{PackExtractionConventions.GetSimpleTypeName(details.Command.ViewModelSymbol)}.{GetExecuteMethodName(details.Command)}(...)";

    private static string GetExecuteMethodName(CommandBinding command) =>
        command.ExecuteSymbol.Split('.').Last();

    private static CommandBinding[] FindCommands(WorkspaceScanResult scanResult, ResolvedEntry resolvedEntry) =>
        scanResult.Commands
            .Where(command =>
                string.Equals(command.ViewModelSymbol, resolvedEntry.Symbol, StringComparison.OrdinalIgnoreCase)
                || command.BoundViews.Any(view => string.Equals(view, resolvedEntry.ResolvedPath, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(static command => command.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private sealed record WeightedTerms(string Source, int Weight, IReadOnlyList<string> Terms);

    private sealed record CommandSelectionAnalysis(
        CommandImpactDetails Details,
        int Score,
        int Rank,
        int CandidateCount,
        IReadOnlyList<string> GoalTerms,
        IReadOnlyList<string> MatchedTerms,
        IReadOnlyList<PackDecisionMatchedSource> MatchedSources);

    private sealed record CommandSelectionResult(
        IReadOnlyList<PackDecisionTrace> DecisionTraces,
        IReadOnlyList<CommandImpactDetails> SelectedCommandDetails);
}
