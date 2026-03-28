# Phase 1

このフォルダは `Rulixa` の Phase 1 仕様をまとめた入口です。

Phase 1 は `Windows` 上の `WPF + .NET 8` アプリケーションを対象に、AI が変更作業を始めるための最小 Context Pack をローカルで生成することを目的にします。

## 現在の実装範囲

- `Rulixa.Domain`、`Rulixa.Application`、`Rulixa.Infrastructure`、`Rulixa.Plugin.WpfNet8`、`Rulixa.Cli` の 5 プロジェクト構成
- CLI の `scan`、`resolve-entry`、`pack`
- `entry=file` と `entry=symbol` の両対応
- `AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel` を主対象にした Pack 生成
- `SelectedItem` / `CurrentPage` の binding と、ViewModel 側更新点の抽出

## 読む順番

- [scope.md](scope.md)
  Phase 1 の対象と非対象
- [architecture.md](architecture.md)
  Frontend / Core 分離とプロジェクト分割
- [ir.md](ir.md)
  Phase 1 の IR 定義
- [entry_resolution.md](entry_resolution.md)
  `entry=file/symbol/auto` の解決仕様
- [wpf_net8_extraction_targets.md](wpf_net8_extraction_targets.md)
  WPF 向け抽出対象
- [context_pack_rules.md](context_pack_rules.md)
  Context Pack の選定ルール
- [implementation_plan.md](implementation_plan.md)
  実装状況と改善バックログ
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
