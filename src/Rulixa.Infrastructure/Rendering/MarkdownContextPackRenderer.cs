using System.Text;
using Rulixa.Application.Ports;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;

namespace Rulixa.Infrastructure.Rendering;

public sealed class MarkdownContextPackRenderer : IContextPackRenderer
{
    public string Render(ContextPack contextPack)
    {
        ArgumentNullException.ThrowIfNull(contextPack);

        var builder = new StringBuilder();
        var systemPackContract = contextPack.Contracts.FirstOrDefault(static contract =>
            string.Equals(contract.Title, "System Pack", StringComparison.Ordinal));
        var displayContracts = contextPack.Contracts
            .Where(static contract => !string.Equals(contract.Title, "System Pack", StringComparison.Ordinal))
            .ToArray();

        builder.AppendLine("# コンテキストパック");
        builder.AppendLine();
        builder.AppendLine("## 目的");
        builder.AppendLine(contextPack.Goal);
        builder.AppendLine();
        builder.AppendLine("## エントリ");
        builder.AppendLine($"- 入力: `{contextPack.Entry}`");
        builder.AppendLine($"- 解決種別: `{ToDisplayText(contextPack.ResolvedEntry.ResolvedKind)}`");
        if (!string.IsNullOrWhiteSpace(contextPack.ResolvedEntry.ResolvedPath))
        {
            builder.AppendLine($"- パス: `{NormalizePath(contextPack.ResolvedEntry.ResolvedPath)}`");
        }

        if (!string.IsNullOrWhiteSpace(contextPack.ResolvedEntry.Symbol))
        {
            builder.AppendLine($"- シンボル: `{contextPack.ResolvedEntry.Symbol}`");
        }

        if (systemPackContract is not null)
        {
            builder.AppendLine();
            builder.AppendLine("## システム地図");
            builder.AppendLine($"- {systemPackContract.Summary}");
        }

        builder.AppendLine();
        builder.AppendLine("## 契約");
        if (displayContracts.Length == 0)
        {
            builder.AppendLine("- なし");
        }
        else
        {
            foreach (var contract in displayContracts.OrderBy(GetContractPriority).ThenBy(static contract => contract.Title, StringComparer.Ordinal))
            {
                builder.AppendLine($"- [{ToDisplayText(contract.Kind)}] {contract.Title}: {contract.Summary}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## ガイド / インデックス");
        if (contextPack.Indexes.Count == 0)
        {
            builder.AppendLine("- なし");
        }
        else
        {
            foreach (var index in contextPack.Indexes.OrderBy(GetIndexPriority).ThenBy(static index => index.Title, StringComparer.Ordinal))
            {
                builder.AppendLine($"### {index.Title}");
                foreach (var line in index.Lines)
                {
                    builder.AppendLine($"- {line}");
                }
            }
        }

        builder.AppendLine();
        builder.AppendLine("## 選択スニペット");
        if (contextPack.SelectedSnippets.Count == 0)
        {
            builder.AppendLine("- なし");
        }
        else
        {
            foreach (var snippet in contextPack.SelectedSnippets
                         .OrderBy(static snippet => snippet.Priority)
                         .ThenBy(static snippet => snippet.Path, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(static snippet => snippet.StartLine))
            {
                builder.AppendLine($"### {NormalizePath(snippet.Path)}:{snippet.StartLine}-{snippet.EndLine}");
                builder.AppendLine($"- 理由: {ToDisplayText(snippet.Reason)}, アンカー: `{snippet.Anchor}`");
                builder.AppendLine($"```{GetCodeFenceLanguage(snippet.Path)}");
                builder.AppendLine(snippet.Content);
                builder.AppendLine("```");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## 選択ファイル");
        if (contextPack.SelectedFiles.Count == 0)
        {
            builder.AppendLine("- なし");
        }
        else
        {
            foreach (var selectedFile in contextPack.SelectedFiles)
            {
                builder.AppendLine(
                    $"- `{NormalizePath(selectedFile.Path)}` (理由: {ToDisplayText(selectedFile.Reason)}, 行数: {selectedFile.LineCount}, 必須: {(selectedFile.IsRequired ? "required" : "optional")})");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## 未解決事項");
        if (contextPack.Unknowns.Count == 0)
        {
            builder.AppendLine("- なし");
        }
        else
        {
            foreach (var unknown in contextPack.Unknowns)
            {
                builder.AppendLine($"- [{ToDisplayText(unknown.Severity)}] {ToDiagnosticLabel(unknown.Code)}");
                builder.AppendLine($"  既知の範囲: {FormatUnknownMessage(unknown.Message)}");
                builder.AppendLine($"  次に見る候補: {FormatCandidates(unknown.Candidates)}");
            }
        }

        return builder.ToString();
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').TrimStart('.').TrimStart('/');

    private static string GetCodeFenceLanguage(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".cs" => "csharp",
        ".xaml" => "xml",
        _ => string.Empty
    };

    private static string ToDisplayText(ResolvedEntryKind resolvedEntryKind) => resolvedEntryKind switch
    {
        ResolvedEntryKind.File => "ファイル",
        ResolvedEntryKind.Symbol => "シンボル",
        ResolvedEntryKind.Unresolved => "未解決",
        _ => resolvedEntryKind.ToString()
    };

    private static string ToDisplayText(ContractKind contractKind) => contractKind switch
    {
        ContractKind.Startup => "起動経路",
        ContractKind.DependencyInjection => "DI 登録",
        ContractKind.ViewModelBinding => "View と ViewModel の対応",
        ContractKind.Navigation => "ナビゲーション",
        ContractKind.Command => "コマンド",
        ContractKind.DialogActivation => "ダイアログ起動",
        _ => contractKind.ToString()
    };

    private static string ToDisplayText(DiagnosticSeverity diagnosticSeverity) => diagnosticSeverity switch
    {
        DiagnosticSeverity.Info => "情報",
        DiagnosticSeverity.Warning => "警告",
        DiagnosticSeverity.Error => "エラー",
        _ => diagnosticSeverity.ToString()
    };

    private static string ToDisplayText(string reason) => reason switch
    {
        "entry" => "入口",
        "startup" => "起動経路",
        "dependency-injection" => "DI 登録",
        "root-binding" => "ルート DataContext",
        "root-binding-source" => "ルート DataContext の設定元",
        "view-binding" => "View DataContext",
        "view-binding-source" => "View DataContext の設定元",
        "data-template" => "DataTemplate 対応",
        "data-template-source" => "DataTemplate の設定元",
        "conventional-view" => "慣例対応 View",
        "code-behind" => "code-behind",
        "command-viewmodel" => "コマンド関連 ViewModel",
        "command-impact" => "コマンド影響先",
        "command-bound-view" => "コマンド利用 View",
        "command-support" => "コマンド補助実装",
        "dialog-service" => "ダイアログ起動サービス",
        "dialog-window" => "ダイアログ Window",
        "dialog-viewmodel" => "ダイアログ ViewModel",
        "navigation-view" => "ナビゲーション View",
        "navigation-xaml-binding" => "XAML ナビゲーション binding",
        "navigation-update" => "ナビゲーション更新",
        "workflow" => "ワークフロー代表",
        "persistence" => "永続化代表",
        "hub-object" => "共有状態代表",
        "external-asset" => "外部資産代表",
        "architecture-test" => "アーキテクチャテスト代表",
        "system-pack" => "System Pack 代表",
        _ => reason
    };

    private static string ToDiagnosticLabel(string code) => code switch
    {
        "entry.unresolved" => "入口を解決できませんでした",
        "workflow.missing-downstream" => "Workflow の下流が不足しています",
        "workflow.ambiguous-target" => "Workflow の候補が競合しています",
        "persistence.missing-owner" => "Persistence の owner を確定できません",
        "hub-object.weak-signal" => "Hub Object の根拠が弱いです",
        "external-asset.unresolved-source" => "外部資産の読込経路を確定できません",
        "architecture-tests.not-found" => "Architecture Tests を見つけられませんでした",
        _ => code
    };

    private static string FormatCandidates(IReadOnlyList<string> candidates) =>
        candidates.Count == 0
            ? "なし"
            : string.Join(", ", candidates.Select(candidate => NormalizePath(candidate)));

    private static string FormatUnknownMessage(string message)
    {
        const string knownPrefix = "既知の範囲: ";
        if (message.StartsWith(knownPrefix, StringComparison.Ordinal))
        {
            return message[knownPrefix.Length..];
        }

        const string legacyPrefix = "既知の範囲: ";
        return message.StartsWith(legacyPrefix, StringComparison.Ordinal)
            ? message[legacyPrefix.Length..]
            : message;
    }

    private static int GetContractPriority(Contract contract) => contract switch
    {
        { Title: "System Pack" } => -10,
        { Kind: ContractKind.ViewModelBinding } when !contract.Title.Contains("DataTemplate", StringComparison.Ordinal) => 0,
        { Kind: ContractKind.Startup } => 10,
        { Kind: ContractKind.DependencyInjection, Title: "Workflow" } => 24,
        { Kind: ContractKind.DependencyInjection, Title: "Persistence" } => 25,
        { Kind: ContractKind.DependencyInjection, Title: "Hub Objects" } => 26,
        { Kind: ContractKind.DependencyInjection, Title: "External Assets" } => 27,
        { Kind: ContractKind.DependencyInjection, Title: "Architecture Tests" } => 28,
        { Kind: ContractKind.DependencyInjection } => 22,
        { Kind: ContractKind.Navigation } => 30,
        { Kind: ContractKind.ViewModelBinding } => 40,
        { Kind: ContractKind.Command, Title: "Workflow" } => 45,
        { Kind: ContractKind.Command } => 50,
        { Kind: ContractKind.DialogActivation } => 60,
        _ => 100
    };

    private static int GetIndexPriority(IndexSection index) => index.Title switch
    {
        "ナビゲーション" => 0,
        "選択から表示への反映" => 10,
        "ナビゲーション更新" => 20,
        "View-ViewModel" => 30,
        "起動経路" => 40,
        "DI" => 45,
        "Workflow" => 50,
        "Persistence" => 55,
        "Hub Objects" => 60,
        "External Assets" => 65,
        "コマンド" => 70,
        "Architecture Tests" => 80,
        _ => 100
    };
}
