# Rulixa

`Rulixa` は、大規模コードベースや複雑な設定を AI が扱いやすい最小文脈に圧縮し、再現可能な作業単位として取り出すためのローカルツールです。  
Phase 1 では `Windows` 上の `WPF + .NET 8` アプリケーションを対象に、`Contracts`、`Index`、`Context Pack` を生成することを目的にしています。

## 現在の実装状況

- `entry=file` と `entry=symbol` の両方で Context Pack を生成できます。
- 主対象は `AssessMeister` の Shell 導線です。
- `symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel` から、`ShellView.xaml` 周辺の必須文脈を既定 budget 内で選定できます。
- `DataTemplate` 配下の PageViewModel は、既定では二次文脈として扱います。

## プロジェクト構成

- `src/Rulixa.Domain`
  ドメインモデル、IR、Context Pack 選定ルール
- `src/Rulixa.Application`
  ユースケースとポート
- `src/Rulixa.Infrastructure`
  ファイルシステム、entry 解決、Markdown 出力
- `src/Rulixa.Plugin.WpfNet8`
  `WPF + .NET 8` 向けの解析実装
- `src/Rulixa.Cli`
  `scan`、`resolve-entry`、`pack` を提供する CLI

## CLI

現在実装済みのコマンドは次の 3 つです。

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

## 関連ドキュメント

- [全体仕様](D:/C#/Rulixa/docs/project_full_spec.md)
- [課題整理](D:/C#/Rulixa/docs/polaris.md)
- [Phase 1 仕様の入口](D:/C#/Rulixa/docs/spec/phase1/README.md)
