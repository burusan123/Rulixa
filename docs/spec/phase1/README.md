# Phase 1

このフォルダは `Rulixa` の Phase 1 仕様をまとめた入口です。  
`Rulixa` は、設計知の成果物を継続生成し、PR レビュー、監査、差分確認、ドリフト検知に使える状態を保つための基盤です。  
Context Pack はその中で AI 変更開始に使う重要な成果物ですが、製品全体の主役ではありません。  
Phase 1 は、その全体構想の最初の具体攻略対象として `Windows` 上の `WPF + .NET 8` を扱い、`scan -> resolve-entry -> pack` を安定化する段階です。

上位方針は [polaris.md](/D:/C#/Rulixa/docs/polaris.md) と [project_full_spec.md](/D:/C#/Rulixa/docs/project_full_spec.md) を正本とし、この配下は Phase 1 の具体仕様を扱います。

## 現在の実装範囲

- プロジェクト構成は `Rulixa.Domain`、`Rulixa.Application`、`Rulixa.Infrastructure`、`Rulixa.Plugin.WpfNet8`、`Rulixa.Cli`
- CLI は `scan`、`resolve-entry`、`pack`
- `entry=file`、`entry=symbol`、`entry=auto` を処理
- `SelectedItem -> CurrentPage` の navigation 導線を抽出
- root binding、view binding、`DataTemplate` 要約、DI、dialog activation を Pack に反映
- `auto` entry は `root-data-context` / `view-data-context` を優先し、`data-template` 由来候補を劣後
- command は少数件なら全件詳細化し、多数件なら summary を維持しつつ `goal` に近い command だけを詳細化
- command 詳細化では `execute -> direct service/dialog` に加えて、同一 ViewModel 内の `private helper 1 hop` を `execute -> helper -> service/dialog` として表示
- dialog 抽出は invocation 単位で `show` / `show-dialog` / `owner kind` を判定
- 大きい `*.cs` は全文ではなく snippet 優先で Pack に入れる
- optional smoke として `D:\C#\AssessMeister` を使う実ワークスペース検証を用意

## 読む順番

- [scope.md](scope.md)
  Phase 1 の対象と非対象
- [architecture.md](architecture.md)
  Domain / Application / Plugin / Infrastructure の責務分割
- [ir.md](ir.md)
  Phase 1 の IR
- [entry_resolution.md](entry_resolution.md)
  `entry=file/symbol/auto` の解決規則
- [wpf_net8_extraction_targets.md](wpf_net8_extraction_targets.md)
  WPF 固有の抽出対象
- [context_pack_rules.md](context_pack_rules.md)
  Pack の構成と snippet / file 選定規則
- [implementation_plan.md](implementation_plan.md)
  現在の実装状態、出口条件、次の backlog
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
  --goal "設定画面を開きたい"
```

### command helper 導線を確認したい場合

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace D:\C#\AssessMeister `
  --entry symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "project"
```

この場合、Pack の `Command` 契約と index で `execute -> helper -> service/dialog` の経路が詳細化されます。
