# Phase 1

Phase 1 は、`Rulixa` の最初の土台を定義するフェーズです。  
目的は `scan -> resolve-entry -> pack` の最小経路を成立させ、WPF + .NET 8 ワークスペースから Context Pack を生成できるようにすることでした。

上位仕様は [polaris.md](../../polaris.md) と [project_full_spec.md](../../project_full_spec.md) を参照してください。

## このフェーズで入れるもの

- プロジェクト構成
  - `Rulixa.Domain`
  - `Rulixa.Application`
  - `Rulixa.Infrastructure`
  - `Rulixa.Plugin.WpfNet8`
  - `Rulixa.Cli`
- CLI
  - `scan`
  - `resolve-entry`
  - `pack`
  - `compare-evidence`
- `entry=file` / `entry=symbol` / `entry=auto`
- root binding / view binding / `DataTemplate` 抽出
- command の 直接呼び出しと helper 1 hop の追跡
- evidence bundle の出力と比較

## 実行例

### file entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <modern-real-workspace> `
  --entry file:src/ReferenceWorkspace.Presentation.Wpf/Views/ShellView.xaml `
  --goal "Shell 画面に新しいページを追加したい"
```

### symbol entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <modern-real-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "設計全体を把握したい"
```

### evidence bundle

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <modern-real-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "project" `
  --evidence-dir artifacts\evidence
```

## ドキュメント構成

- [scope.md](scope.md)
- [architecture.md](architecture.md)
- [ir.md](ir.md)
- [entry_resolution.md](entry_resolution.md)
- [wpf_net8_extraction_targets.md](wpf_net8_extraction_targets.md)
- [context_pack_rules.md](context_pack_rules.md)
- [implementation_plan.md](implementation_plan.md)
- [examples/sample_shell_pack_example.md](examples/sample_shell_pack_example.md)
