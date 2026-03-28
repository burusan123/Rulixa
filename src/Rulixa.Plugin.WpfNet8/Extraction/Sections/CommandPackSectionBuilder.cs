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
        ["setting"] = ["setting", "settings", "設定"],
        ["drafting"] = ["drafting", "作図"],
        ["license"] = ["license", "ライセンス"],
        ["import"] = ["import", "インポート"],
        ["export"] = ["export", "エクスポート"],
        ["share"] = ["share", "共有"],
        ["result"] = ["result", "results", "結果", "出力"],
        ["project"] = ["project", "projects", "プロジェクト"],
        ["open"] = ["open", "起動", "開く"],
        ["save"] = ["save", "保存"],
        ["reset"] = ["reset", "初期化", "リセット"]
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
        CancellationToken cancellationToken)
    {
        var commands = FindCommands(scanResult, resolvedEntry);
        var impactAnalyzer = new CommandImpactAnalyzer(workspaceFileSystem);
        var commandDetails = await impactAnalyzer
            .AnalyzeAsync(workspaceRoot, scanResult, commands, cancellationToken)
            .ConfigureAwait(false);
        var detailedCommands = SelectDetailedCommands(commandDetails, goal);
        var summarized = commands.Length > CommandSummaryThreshold;

        if (summarized)
        {
            contracts.Add(BuildSummaryContract(commands));
        }

        foreach (var commandDetailsItem in summarized ? detailedCommands : commandDetails)
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
        var detailedCommands = SelectDetailedCommands(commandDetails, goal);

        if (commands.Length > CommandSummaryThreshold)
        {
            var sampleNames = commands
                .Select(static command => command.PropertyName)
                .Take(3)
                .ToArray();
            var lines = new List<string> { $"{commands.Length}件のコマンド導線 (例: {string.Join(", ", sampleNames)})" };
            lines.AddRange(detailedCommands.Select(BuildDetailedIndexLine));
            return new IndexSection("コマンド", lines);
        }

        return new IndexSection(
            "コマンド",
            commandDetails.Select(BuildDetailedIndexLine).ToArray());
    }

    private static Contract BuildSummaryContract(IReadOnlyList<CommandBinding> commands)
    {
        var sampleNames = commands
            .Select(static command => command.PropertyName)
            .Take(3)
            .ToArray();
        var summary = $"{commands[0].ViewModelSymbol} には {commands.Count} 件のコマンド導線があります。例: {string.Join(", ", sampleNames)}。";
        return new Contract(
            ContractKind.Command,
            "コマンド導線の要約",
            summary,
            commands.SelectMany(static command => command.BoundViews).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            commands.Select(static command => command.ExecuteSymbol).ToArray());
    }

    private static Contract BuildDetailedContract(CommandImpactDetails commandDetails)
    {
        var command = commandDetails.Command;
        var summary = $"{command.PropertyName} は {command.ExecuteSymbol}(...) を実行します。";
        if (commandDetails.DirectImpacts.Count == 0)
        {
            return new Contract(
                ContractKind.Command,
                command.PropertyName,
                summary,
                command.BoundViews,
                [command.ViewModelSymbol, command.ExecuteSymbol]);
        }

        var impactsSummary = string.Join("、", commandDetails.DirectImpacts.Select(FormatImpactSummary));
        return new Contract(
            ContractKind.Command,
            command.PropertyName,
            $"{summary} {impactsSummary}",
            command.BoundViews,
            BuildRelatedSymbols(commandDetails));
    }

    private static string[] BuildRelatedSymbols(CommandImpactDetails commandDetails) =>
        new[] { commandDetails.Command.ViewModelSymbol, commandDetails.Command.ExecuteSymbol }
            .Concat(commandDetails.DirectImpacts.Select(static impact => impact.DisplaySymbol))
            .Concat(commandDetails.DirectImpacts
                .Where(static impact => !string.IsNullOrWhiteSpace(impact.DialogWindowSymbol))
                .Select(static impact => impact.DialogWindowSymbol!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string FormatImpactSummary(DirectCommandImpact impact)
    {
        var summary = $"{impact.DisplaySymbol} を呼び出します。";
        if (string.IsNullOrWhiteSpace(impact.DialogWindowSymbol) || string.IsNullOrWhiteSpace(impact.ActivationKind))
        {
            return summary;
        }

        return $"{impact.DisplaySymbol} を呼び出し、最終的に {impact.DialogWindowSymbol} が {impact.ActivationKind} で起動されます。";
    }

    private static string BuildDetailedIndexLine(CommandImpactDetails commandDetails)
    {
        if (commandDetails.DirectImpacts.Count == 0)
        {
            return $"{commandDetails.Command.PropertyName} -> {commandDetails.Command.ExecuteSymbol}(...)";
        }

        var impacts = string.Join(" / ", commandDetails.DirectImpacts.Select(FormatImpactIndexPart));
        return $"{commandDetails.Command.PropertyName} -> {commandDetails.Command.ExecuteSymbol}(...) -> {impacts}";
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

            var methodName = details.Command.ExecuteSymbol.Split('.').Last();
            var executeSnippet = await snippetFactory
                .CreateMethodSnippetAsync(
                    workspaceRoot,
                    details.ViewModelFilePath,
                    methodName,
                    "command-viewmodel",
                    20,
                    false,
                    $"{methodName}(...)",
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
            if (executeSnippet is not null)
            {
                snippetCandidates.Add(executeSnippet);
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
                        21,
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

    private static CommandImpactDetails[] SelectDetailedCommands(
        IReadOnlyList<CommandImpactDetails> commandDetails,
        string goal)
    {
        if (commandDetails.Count <= CommandSummaryThreshold)
        {
            return commandDetails.ToArray();
        }

        return commandDetails
            .Select(details => new ScoredCommandImpact(details, ScoreGoalRelevance(goal, details)))
            .Where(static scored => scored.Score > 0)
            .OrderByDescending(static scored => scored.Score)
            .ThenBy(static scored => scored.Details.Command.PropertyName, StringComparer.OrdinalIgnoreCase)
            .Take(MaxDetailedCommandsWhenSummarized)
            .Select(static scored => scored.Details)
            .ToArray();
    }

    private static int ScoreGoalRelevance(string goal, CommandImpactDetails details)
    {
        var goalTerms = ExtractGoalTerms(goal);
        if (goalTerms.Count == 0)
        {
            return 0;
        }

        var score = 0;
        score += CountMatches(goalTerms, ExtractIdentifierTerms(details.Command.PropertyName)) * 3;
        score += CountMatches(goalTerms, ExtractIdentifierTerms(details.Command.ExecuteSymbol.Split('.').Last())) * 2;
        score += CountMatches(goalTerms, details.DirectImpacts.SelectMany(static impact => ExtractIdentifierTerms(impact.DisplaySymbol))) * 2;
        score += CountMatches(goalTerms, details.DirectImpacts
            .Where(static impact => !string.IsNullOrWhiteSpace(impact.DialogWindowSymbol))
            .SelectMany(static impact => ExtractIdentifierTerms(impact.DialogWindowSymbol!))) * 2;
        return score;
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

    private static CommandBinding[] FindCommands(WorkspaceScanResult scanResult, ResolvedEntry resolvedEntry) =>
        scanResult.Commands
            .Where(command =>
                string.Equals(command.ViewModelSymbol, resolvedEntry.Symbol, StringComparison.OrdinalIgnoreCase)
                || command.BoundViews.Any(view => string.Equals(view, resolvedEntry.ResolvedPath, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(static command => command.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private sealed record ScoredCommandImpact(CommandImpactDetails Details, int Score);
}
