# Rulixa

`Rulixa` は、設計知・依存・制約・安全要件を継続生成し、AI 入力を正規化し、PR レビューや監査に使える成果物へ変換するためのローカル基盤です。

現在の Phase 1 では、その具体攻略対象として `Windows` 上の `WPF + .NET 8` アプリケーションを扱い、`Contracts`、`Index`、`Selected Files` を含む Context Pack を `CLI` と `Codex Plugin` から生成できます。

## 製品の中心価値

- AI に渡す入力を決定的に正規化する
- PR レビューに必要な契約と依存を差分で読めるようにする
- 監査証跡として成果物を継続生成する
- その一部として Context Pack を生成する

## Phase 1 の到達点

- `entry=file` と `entry=symbol` の両方で Context Pack を生成できます。
- 主対象は `AssessMeister` の Shell 導線です。
- `symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel` から、Shell 導線の必須ファイルを既定 budget 内で選定できます。
- `Pages/*` の `DataTemplate` 由来 ViewModel は、`symbol` 起点の既定 Pack では二次文脈として落とします。
- `SelectedItem` と `CurrentPage` の binding だけでなく、`CurrentPage = item.PageViewModel` や `SelectedItem = match` のような ViewModel 側更新点も Pack に含めます。

## プロジェクト構成

- `src/Rulixa.Domain`
  ドメイン型、Pack 選定ルール、IR の中核型
- `src/Rulixa.Application`
  ユースケースとポート
- `src/Rulixa.Infrastructure`
  ファイルシステム、レンダリング、entry 解決支援
- `src/Rulixa.Plugin.WpfNet8`
  `WPF + .NET 8` 固有の走査と契約抽出
- `src/Rulixa.Cli`
  `scan`、`resolve-entry`、`pack` を提供する CLI

## CLI

現在の主要コマンドは次の 3 つです。

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

repo-local の Codex Plugin を `plugins/rulixa` に配置しています。
この plugin は `Rulixa.Cli` の `pack` を最短導線で呼び出すためのもので、Codex から Context Pack を作る入口として使います。

- plugin root: `plugins/rulixa`
- marketplace: `.agents/plugins/marketplace.json`
- 主 skill: `plugins/rulixa/skills/pack/SKILL.md`

## ドキュメント

- [全体仕様](/D:/C#/Rulixa/docs/project_full_spec.md)
- [背景整理](/D:/C#/Rulixa/docs/polaris.md)
- [Phase 1 仕様](/D:/C#/Rulixa/docs/spec/phase1/README.md)
