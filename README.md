# Rulixa

`Rulixa` は、WPF / .NET ワークスペースから高密度な Context Pack と人間向けの要約文書を生成するツールです。  
全文検索の代替ではなく、`どこから理解し始めるべきか` を短いコンテキストで返すことを目的にしています。

## できること

- `entry=file` と `entry=symbol` の 2 方式で `pack` を生成する
- root ViewModel や root XAML から system map をまとめて返す
- `unknowns` と next candidates で次に読む候補を案内する
- evidence bundle と quality artifact で比較・監査を支える
- `render-human` で人間向けの review / audit / knowledge 文書を出す
- local quality gate から `release-review.md` と `human-outputs/` をまとめて出す
- Codex plugin から `pack` skill を利用できる

## 主なコマンド

- `scan`
- `resolve-entry`
- `pack`
- `render-human`
- `compare-evidence`

## 例

### Context Pack を生成する

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "システム全体の地図を確認する"
```

### 人間向けの review brief を生成する

```powershell
dotnet run --project src\Rulixa.Cli -- render-human `
  --workspace <target-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "project" `
  --mode review
```

### evidence bundle と一緒に audit snapshot を保存する

```powershell
dotnet run --project src\Rulixa.Cli -- render-human `
  --workspace <target-workspace> `
  --entry file:src/ReferenceWorkspace.Presentation.Wpf/Views/ShellView.xaml `
  --goal "legacy system" `
  --mode audit `
  --out artifacts\audit.md `
  --evidence-dir artifacts\evidence
```

### evidence bundle を比較する

```powershell
dotnet run --project src\Rulixa.Cli -- compare-evidence `
  --base artifacts\evidence\<base-bundle> `
  --target artifacts\evidence\<target-bundle>
```

## `render-human` の mode

- `review`
  システム概要、中心状態、主要 workflow、unknown / risk、次に読む候補をまとめます。
- `audit`
  root entry、observed facts、evidence source、degraded diagnostics、未確定事項をまとめます。
- `knowledge`
  subsystem map、dependency seams、architectural constraints、known unknowns、将来変更時の注目点をまとめます。

## local quality gate の成果物

`powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-LocalQualityGate.ps1` を実行すると、`artifacts/local-quality/latest/` に次が揃います。

- `gate.json`
- `kpi.json`
- `summary.md`
- `release-review.md`
- `human-outputs/review-brief.md`
- `human-outputs/audit-snapshot.md`
- `human-outputs/design-knowledge-snapshot.md`

release review では `summary.md` を一次確認に使い、必要に応じて `release-review.md` と `human-outputs/` を読む運用を想定しています。

## リポジトリ構成

- `src/Rulixa.Domain`
- `src/Rulixa.Application`
- `src/Rulixa.Infrastructure`
- `src/Rulixa.Plugin.WpfNet8`
- `src/Rulixa.Cli`
- `plugins/rulixa`

## ドキュメント

- [全体仕様](docs/project_full_spec.md)
- [Product Readiness](docs/product-readiness.md)
- [Phase 1](docs/spec/phase1/README.md)
- [Phase 2](docs/spec/phase2/README.md)
- [Phase 3](docs/spec/phase3/README.md)
- [Phase 4](docs/spec/phase4/README.md)
- [Phase 5](docs/spec/phase5/README.md)
- [Phase 6](docs/spec/phase6/README.md)
- [Phase 7](docs/spec/phase7/README.md)
- [Phase 8](docs/spec/phase8/README.md)
