# Rulixa

`Rulixa` は、大規模で暗黙仕様の多いコードベースを AI が安全に扱える最小の作業文脈へ圧縮するためのローカル Context Pack 生成器です。  
現在の Phase 1 は `Windows` 上の `WPF + .NET 8` アプリケーションを対象に、`Contracts`、`Index`、`Context Pack` を組み立てるところまでを実装しています。

## 現在の実装

- `entry=file` と `entry=symbol` の両方で Context Pack を生成できます。
- 主対象は `AssessMeister` の Shell 導線です。
- `symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel` から、`ShellView.xaml` 周辺の必須ファイルを既定 budget 内で選定できます。
- `DataTemplate` 由来の PageViewModel 群は、既定では二次文脈として扱います。

## プロジェクト構成

- `src/Rulixa.Domain`
  ドメインモデルと Context Pack 選定ルール
- `src/Rulixa.Application`
  ユースケースとポート
- `src/Rulixa.Infrastructure`
  ファイルシステム、entry 解決、Markdown 出力
- `src/Rulixa.Plugin.WpfNet8`
  `WPF + .NET 8` 向け解析
- `src/Rulixa.Cli`
  `scan`、`resolve-entry`、`pack` を提供する CLI

## CLI

現在の主コマンドは次の 3 つです。

- `scan`
- `resolve-entry`
- `pack`

### 例: file entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace D:\C#\AssessMeister `
  --entry file:src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml `
  --goal "Shell 画面に新しいページを追加したい"
```

### 例: symbol entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace D:\C#\AssessMeister `
  --entry symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "Shell 画面に新しいページを追加したい"
```

## Codex Plugin

repo-local の Codex Plugin を `plugins/rulixa` に追加しています。  
この plugin は `Rulixa.Cli` の `pack` を最短導線として扱い、Codex から Context Pack 生成手順を呼び出せるようにするためのものです。

- plugin root: `plugins/rulixa`
- marketplace: `.agents/plugins/marketplace.json`
- 主 skill: `plugins/rulixa/skills/pack/SKILL.md`

## 関連ドキュメント

- [全体仕様](docs/project_full_spec.md)
- [背景整理](docs/polaris.md)
- [Phase 1 仕様入口](docs/spec/phase1/README.md)
