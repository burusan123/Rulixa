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

        builder.AppendLine();
        builder.AppendLine("## 契約");
        if (contextPack.Contracts.Count == 0)
        {
            builder.AppendLine("- なし");
        }
        else
        {
            foreach (var contract in contextPack.Contracts.OrderBy(GetContractPriority).ThenBy(static contract => contract.Title, StringComparer.Ordinal))
            {
                builder.AppendLine($"- [{ToDisplayText(contract.Kind)}] {contract.Title}: {contract.Summary}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## 参照ガイド / インデックス");
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
        builder.AppendLine("## 抜粋スニペット");
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
        "data-template-source" => "DataTemplate 設定元",
        "conventional-view" => "規約対応 View",
        "code-behind" => "code-behind",
        "command-viewmodel" => "コマンド関連 ViewModel",
        "command-impact" => "コマンド影響先",
        "command-bound-view" => "コマンドが使われる View",
        "command-support" => "コマンド補助実装",
        "dialog-service" => "ダイアログ起動サービス",
        "navigation-view" => "ナビゲーション View",
        "navigation-xaml-binding" => "XAML ナビゲーション binding",
        "navigation-update" => "ナビゲーション更新処理",
        "workflow" => "ワークフロー候補",
        "persistence" => "永続化候補",
        "hub-object" => "共有状態候補",
        "external-asset" => "外部資産候補",
        "architecture-test" => "アーキテクチャテスト候補",
        _ => reason
    };

    private static string ToDiagnosticLabel(string code) => code switch
    {
        "entry.unresolved" => "入口の解決失敗",
        "workflow.missing-downstream" => "Workflow の探索ガイド",
        "workflow.ambiguous-target" => "Workflow の候補が競合",
        "persistence.missing-owner" => "Persistence の探索ガイド",
        "hub-object.weak-signal" => "Hub Object の探索ガイド",
        "external-asset.unresolved-source" => "外部資産の探索ガイド",
        "architecture-tests.not-found" => "Architecture Test の探索ガイド",
        _ => code
    };

    private static string FormatCandidates(IReadOnlyList<string> candidates) =>
        candidates.Count == 0
            ? "なし"
            : string.Join(", ", candidates.Select(candidate => NormalizePath(candidate)));

    private static string FormatUnknownMessage(string message)
    {
        const string knownPrefix = "追跡できた範囲: ";
        if (!message.StartsWith(knownPrefix, StringComparison.Ordinal))
        {
            return message;
        }

        return message[knownPrefix.Length..];
    }

    private static int GetContractPriority(Contract contract) => contract.Kind switch
    {
        ContractKind.ViewModelBinding when !contract.Title.Contains("DataTemplate", StringComparison.Ordinal) => 0,
        ContractKind.Startup => 10,
        ContractKind.DependencyInjection when contract.Title == "Workflow" => 24,
        ContractKind.DependencyInjection when contract.Title == "Persistence" => 25,
        ContractKind.DependencyInjection when contract.Title == "Hub Objects" => 26,
        ContractKind.DependencyInjection when contract.Title == "External Assets" => 27,
        ContractKind.DependencyInjection when contract.Title == "Architecture Tests" => 28,
        ContractKind.DependencyInjection => 22,
        ContractKind.Navigation => 30,
        ContractKind.ViewModelBinding => 40,
        ContractKind.Command when contract.Title == "Workflow" => 45,
        ContractKind.Command => 50,
        ContractKind.DialogActivation => 60,
        _ => 100
    };

    private static int GetIndexPriority(IndexSection index) => index.Title switch
    {
        "ナビゲーション" => 0,
        "選択から表示への因果" => 10,
        "ナビゲーション更新処理" => 20,
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
