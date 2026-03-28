# Phase 1

このフォルダは `Rulixa` の Phase 1 仕様をまとめた入口です。

この配下の文書は、Rulixa 全体の正本ではありません。
上位方針は [polaris.md](/D:/C#/Rulixa/docs/polaris.md) と [project_full_spec.md](/D:/C#/Rulixa/docs/project_full_spec.md) を参照してください。

Phase 1 は、その上位方針を `Windows` 上の `WPF + .NET 8` という具体攻略対象で実装するための仕様です。

## 現在の実装範囲

- `Rulixa.Domain`、`Rulixa.Application`、`Rulixa.Infrastructure`、`Rulixa.Plugin.WpfNet8`、`Rulixa.Cli` の 5 プロジェクト構成
- CLI の `scan`、`resolve-entry`、`pack`
- `entry=file` と `entry=symbol` の両対応
- `AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel` を主対象にした Pack 生成
- `SelectedItem` / `CurrentPage` の binding と、ViewModel 側更新点の抽出
- `file:.../ShellView.xaml` から `ShellViewModel`、`MainWindow`、起動経路、DI 登録を辿る Pack 生成
- `scan` 時に `publish/*` と `*_wpftmp.csproj` を除外

## 読む順番

- [scope.md](scope.md)
  Phase 1 の対象と非対象
- [architecture.md](architecture.md)
  Phase 1 用の Frontend / Core 分離とプロジェクト分割
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
