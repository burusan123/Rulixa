# Phase 1

このフォルダは、`Rulixa` の Phase 1 仕様をまとめた入口です。  
現在の Phase 1 は `Windows` 上の `WPF + .NET 8` アプリケーションを対象に、AI 用の最小 Context Pack を決定的に生成することを目的にしています。

## 現在の到達点

- `Rulixa.Domain`、`Rulixa.Application`、`Rulixa.Infrastructure`、`Rulixa.Plugin.WpfNet8`、`Rulixa.Cli` の 5 プロジェクト構成で実装済みです。
- CLI として `scan`、`resolve-entry`、`pack` を提供しています。
- `entry=file` と `entry=symbol` の両方に対応しています。
- `AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel` を起点にした Pack 生成で、Shell 周辺の必須ファイルだけを優先して選定できます。

## 主なドキュメント

- [scope.md](scope.md)
  Phase 1 の対象範囲と非対象範囲
- [architecture.md](architecture.md)
  Frontend / Core 分離とプロジェクト構成
- [ir.md](ir.md)
  Phase 1 の IR 定義
- [entry_resolution.md](entry_resolution.md)
  `entry=file/symbol/auto` の解決仕様
- [wpf_net8_extraction_targets.md](wpf_net8_extraction_targets.md)
  WPF 向け抽出対象
- [context_pack_rules.md](context_pack_rules.md)
  Context Pack の選定ルール
- [implementation_plan.md](implementation_plan.md)
  Phase 1 の実装順序
- [examples/assessmeister_shell_pack_example.md](examples/assessmeister_shell_pack_example.md)
  `AssessMeister` を題材にした Pack 例

## 実行例

### file entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace D:\C#\AssessMeister `
  --entry file:src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml `
  --goal "Shell 画面に新しいページを追加したい"
```

### symbol entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace D:\C#\AssessMeister `
  --entry symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "Shell 画面に新しいページを追加したい"
```
