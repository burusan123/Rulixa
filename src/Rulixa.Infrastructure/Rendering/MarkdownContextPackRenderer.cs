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
        foreach (var contract in contextPack.Contracts.OrderBy(GetContractPriority).ThenBy(static contract => contract.Title, StringComparer.Ordinal))
        {
            builder.AppendLine($"- [{ToDisplayText(contract.Kind)}] {contract.Title}: {contract.Summary}");
        }

        builder.AppendLine();
        builder.AppendLine("## 影響範囲 / インデックス");
        foreach (var index in contextPack.Indexes.OrderBy(GetIndexPriority).ThenBy(static index => index.Title, StringComparer.Ordinal))
        {
            builder.AppendLine($"### {index.Title}");
            foreach (var line in index.Lines)
            {
                builder.AppendLine($"- {line}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## 選定ファイル");
        foreach (var selectedFile in contextPack.SelectedFiles)
        {
            builder.AppendLine(
                $"- `{NormalizePath(selectedFile.Path)}` (理由: {ToDisplayText(selectedFile.Reason)}, 行数: {selectedFile.LineCount}, 種別: {(selectedFile.IsRequired ? "必須" : "任意")})");
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
                builder.AppendLine($"- [{ToDisplayText(unknown.Severity)}] {unknown.Code}: {unknown.Message}");
            }
        }

        return builder.ToString();
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').TrimStart('.').TrimStart('/');

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
        ContractKind.DependencyInjection => "依存関係の構成",
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
        "data-template" => "DataTemplate による二次文脈",
        "data-template-source" => "DataTemplate の定義元",
        "conventional-view" => "規約ベースの対応 View",
        "code-behind" => "対応する code-behind",
        "command-viewmodel" => "コマンド定義元 ViewModel",
        "command-bound-view" => "コマンドが使われる View",
        "command-support" => "コマンド基盤",
        "dialog-service" => "ダイアログ起動サービス",
        "navigation-view" => "ナビゲーション View",
        "navigation-update" => "ナビゲーション更新点",
        _ => reason
    };

    private static int GetContractPriority(Contract contract) => contract.Kind switch
    {
        ContractKind.ViewModelBinding when !contract.Title.Contains("DataTemplate", StringComparison.Ordinal) => 0,
        ContractKind.Startup => 10,
        ContractKind.DependencyInjection => 20,
        ContractKind.Navigation when contract.Title == "選択から表示への因果" => 25,
        ContractKind.Navigation => 30,
        ContractKind.ViewModelBinding => 40,
        ContractKind.Command => 50,
        ContractKind.DialogActivation => 60,
        _ => 100
    };

    private static int GetIndexPriority(IndexSection index) => index.Title switch
    {
        "ナビゲーション" => 0,
        "選択から表示への因果" => 10,
        "ナビゲーション更新点" => 20,
        "View-ViewModel" => 30,
        "起動経路" => 40,
        "コマンド" => 50,
        _ => 100
    };
}
