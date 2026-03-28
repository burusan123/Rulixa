# Phase 1

このフォルダは、`Rulixa` の Phase 1 実装仕様をまとめたものです。  
現時点の Phase 1 は、`Windows` 上の `WPF + .NET 8` アプリケーションを対象に、AI 用の最小 Context Pack を生成することを目的にしています。

## 現在の到達点

- `Rulixa.Domain`、`Rulixa.Application`、`Rulixa.Infrastructure`、`Rulixa.Plugin.WpfNet8`、`Rulixa.Cli` の 5 プロジェクト構成で実装済みです。
- `scan`、`resolve-entry`、`pack` の CLI が動作します。
- `entry=file` と `entry=symbol` の両方に対応しています。
- `AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel` を入口にした Pack 生成を主要な受け入れケースとして扱っています。

## 主要ドキュメント

- [scope.md](D:/C#/Rulixa/docs/spec/phase1/scope.md)
  Phase 1 の対象範囲と非対象
- [architecture.md](D:/C#/Rulixa/docs/spec/phase1/architecture.md)
  Frontend / Core 分離とプロジェクト構成
- [ir.md](D:/C#/Rulixa/docs/spec/phase1/ir.md)
  Phase 1 の IR 定義
- [entry_resolution.md](D:/C#/Rulixa/docs/spec/phase1/entry_resolution.md)
  `entry=file/symbol/auto` の解決仕様
- [wpf_net8_extraction_targets.md](D:/C#/Rulixa/docs/spec/phase1/wpf_net8_extraction_targets.md)
  WPF 解析対象
- [context_pack_rules.md](D:/C#/Rulixa/docs/spec/phase1/context_pack_rules.md)
  Context Pack の選定ルール
- [implementation_plan.md](D:/C#/Rulixa/docs/spec/phase1/implementation_plan.md)
  Phase 1 の実装方針
- [examples/assessmeister_shell_pack_example.md](D:/C#/Rulixa/docs/spec/phase1/examples/assessmeister_shell_pack_example.md)
  `AssessMeister` を題材にした Pack 例

## 代表的な実行例

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
