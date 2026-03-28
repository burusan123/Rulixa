using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class CommandPackSectionBuilder
{
    private const int CommandSummaryThreshold = 6;

    internal static void AddContracts(
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        ICollection<Contract> contracts,
        ICollection<FileSelectionCandidate> fileCandidates)
    {
        var commands = FindCommands(scanResult, resolvedEntry);
        if (commands.Length > CommandSummaryThreshold)
        {
            var sampleNames = commands
                .Select(static command => command.PropertyName)
                .Take(3)
                .ToArray();
            var summary = $"{commands[0].ViewModelSymbol} には {commands.Length} 件のコマンド導線があります。例: {string.Join(", ", sampleNames)}。";
            contracts.Add(new Contract(
                ContractKind.Command,
                "コマンド導線の要約",
                summary,
                commands.SelectMany(static command => command.BoundViews).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                commands.Select(static command => command.ExecuteSymbol).ToArray()));
        }
        else
        {
            foreach (var command in commands)
            {
                contracts.Add(new Contract(
                    ContractKind.Command,
                    command.PropertyName,
                    $"{command.PropertyName} が {command.ExecuteSymbol} を実行します。",
                    command.BoundViews,
                    [command.ViewModelSymbol, command.ExecuteSymbol]));
            }
        }

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

        var delegateCommandFile = scanResult.Files.FirstOrDefault(file =>
            Path.GetFileName(file.Path).Equals("DelegateCommand.cs", StringComparison.OrdinalIgnoreCase));
        if (delegateCommandFile is not null && commands.Length > 0)
        {
            fileCandidates.Add(new FileSelectionCandidate(delegateCommandFile.Path, "command-support", 30, true));
        }
    }

    internal static IndexSection BuildIndex(WorkspaceScanResult scanResult, ResolvedEntry resolvedEntry)
    {
        var commands = FindCommands(scanResult, resolvedEntry);
        if (commands.Length > CommandSummaryThreshold)
        {
            var sampleNames = commands
                .Select(static command => command.PropertyName)
                .Take(3)
                .ToArray();
            return new IndexSection(
                "コマンド",
                [$"{commands.Length}件のコマンド導線 (例: {string.Join(", ", sampleNames)})"]);
        }

        return new IndexSection(
            "コマンド",
            commands.Select(command => $"{command.PropertyName} -> {command.ExecuteSymbol}").ToArray());
    }

    private static CommandBinding[] FindCommands(WorkspaceScanResult scanResult, ResolvedEntry resolvedEntry) =>
        scanResult.Commands
            .Where(command =>
                string.Equals(command.ViewModelSymbol, resolvedEntry.Symbol, StringComparison.OrdinalIgnoreCase)
                || command.BoundViews.Any(view => string.Equals(view, resolvedEntry.ResolvedPath, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(static command => command.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
