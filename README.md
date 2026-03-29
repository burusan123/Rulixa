# Rulixa

`Rulixa` は、WPF / .NET ワークスペースから高密度な Context Pack を生成するツールです。  
目的は全文検索の代替ではなく、LLM や人間が短いコンテキストで正しく理解を始められる system map を返すことです。

## できること

- `entry=file` と `entry=symbol` の 2 方式で pack を生成
- root ViewModel や root XAML から system map を圧縮して返す
- `unknowns` と next candidates で次に掘る候補を案内
- evidence bundle と quality gate artifact を生成
- Codex plugin から `pack` を呼び出し可能

## 主なコマンド

- `scan`
- `resolve-entry`
- `pack`
- `compare-evidence`

## 例

### file entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry file:src/ReferenceWorkspace.Presentation.Wpf/Views/ShellView.xaml `
  --goal "Shell 画面の workflow と persistence map を理解する"
```

### symbol entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "システム全体の構造を理解する"
```

### evidence bundle を保存

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "project" `
  --evidence-dir artifacts\evidence
```

### evidence bundle を比較

```powershell
dotnet run --project src\Rulixa.Cli -- compare-evidence `
  --base artifacts\evidence\<base-bundle> `
  --target artifacts\evidence\<target-bundle>
```

## リポジトリ構成

- `src/Rulixa.Domain`
- `src/Rulixa.Application`
- `src/Rulixa.Infrastructure`
- `src/Rulixa.Plugin.WpfNet8`
- `src/Rulixa.Cli`
- `plugins/rulixa`

## ドキュメント

- [全体仕様](/D:/C%23/Rulixa/docs/project_full_spec.md)
- [Product Readiness](/D:/C%23/Rulixa/docs/product-readiness.md)
- [Phase 1](/D:/C%23/Rulixa/docs/spec/phase1/README.md)
- [Phase 2](/D:/C%23/Rulixa/docs/spec/phase2/README.md)
- [Phase 3](/D:/C%23/Rulixa/docs/spec/phase3/README.md)
- [Phase 4](/D:/C%23/Rulixa/docs/spec/phase4/README.md)
- [Phase 5](/D:/C%23/Rulixa/docs/spec/phase5/README.md)
- [Phase 6](/D:/C%23/Rulixa/docs/spec/phase6/README.md)
