# Rulixa

`Rulixa` は、WPF / .NET ワークスペースから高密度な Context Pack と人間向け補助資料を生成するツールです。  
最初に `pack` で地図を取り、必要に応じて `render-human` で文章資料へ、`render-visual` で探索型 UI artifact へ展開します。

## できること

- `entry=file` と `entry=symbol` の両方で `pack` を生成する
- root ViewModel / root XAML から system map をまとめる
- `unknowns` と next candidates で次に読む候補を出す
- evidence bundle と quality artifact で根拠と比較を残す
- `render-human` で `review` / `audit` / `knowledge` の文章資料を出す
- `render-visual` で `index.html` / `app.css` / `app.js` の探索型 artifact を出す
- local quality gate から `release-review.md`、`human-outputs/`、`visual-outputs/` を補助資料として出す

## 主なコマンド

- `scan`
- `resolve-entry`
- `pack`
- `render-human`
- `render-visual`
- `compare-evidence`

## 例

### Context Pack を生成する

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "システム全体の地図を作る"
```

### Review Brief を生成する

```powershell
dotnet run --project src\Rulixa.Cli -- render-human `
  --workspace <target-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "project" `
  --mode review
```

### Visual Output を生成する

```powershell
dotnet run --project src\Rulixa.Cli -- render-visual `
  --workspace <target-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "project" `
  --out-dir artifacts\visual
```

### Evidence Bundle つきで Audit Snapshot を生成する

```powershell
dotnet run --project src\Rulixa.Cli -- render-human `
  --workspace <target-workspace> `
  --entry file:src/ReferenceWorkspace.Presentation.Wpf/Views/ShellView.xaml `
  --goal "legacy system" `
  --mode audit `
  --out artifacts\audit.md `
  --evidence-dir artifacts\evidence
```

## `render-human` の mode

- `review`
  概要、中心状態、主要 workflow、unknown / risk、次に読む file / symbol をまとめる
- `audit`
  root entry、observed facts、evidence source、degraded diagnostics、未確定事項をまとめる
- `knowledge`
  subsystem map、dependency seams、architectural constraints、known unknowns、将来変更時の注目点をまとめる

## `render-visual` の artifact

- `index.html`
  `Overview` / `Workflow` / `Evidence` / `Unknowns` / `Architecture` の 5 view を持つ探索 UI 本体
- `app.css`
  visual artifact の見た目を定義する
- `app.js`
  埋め込みデータを読み、検索、折りたたみ、inspector 更新を行う

## local quality gate の出力

`powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-LocalQualityGate.ps1` を実行すると、`artifacts/local-quality/latest/` に次が揃います。

- `gate.json`
- `kpi.json`
- `summary.md`
- `release-review.md`
- `human-outputs/`
- `visual-outputs/`

release review では `summary.md` を一次資料として読み、必要に応じて `release-review.md`、`human-outputs/`、`visual-outputs/` を補助資料として辿ります。  
`render-visual` は required gate の判定資料ではありませんが、文章資料を読んだ後に探索したいときの補助資料です。

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
- [Phase 9](docs/spec/phase9/README.md)
