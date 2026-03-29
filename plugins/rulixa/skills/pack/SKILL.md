---
name: pack
description: `Rulixa.Cli` を使って、WPF / .NET ワークスペースの Context Pack と人間向け Markdown を生成します。
---

# Rulixa Pack

`Rulixa.Cli` は、WPF / .NET ワークスペースの system map を短時間で掴むための CLI です。  
まず `pack` で地図を取り、必要に応じて `render-human` で review / audit / knowledge 文書へ変換する使い方を推奨します。

## 入力

- `entry=symbol`
  root ViewModel や主要サービスの symbol が分かっているときに使います。
- `entry=file`
  root XAML や code-behind から入りたいときに使います。

## 基本コマンド

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry <entry> `
  --goal "<goal>"
```

## 例

### symbol entry で system map を出す

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "システム全体の地図を確認する"
```

### file entry で root XAML から入る

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry file:src/ReferenceWorkspace.Presentation.Wpf/Views/ShellView.xaml `
  --goal "Shell 画面の workflow と persistence map を確認する"
```

### review brief を生成する

```powershell
dotnet run --project src\Rulixa.Cli -- render-human `
  --workspace <target-workspace> `
  --entry <entry> `
  --goal "<goal>" `
  --mode review
```

### audit snapshot を evidence bundle と一緒に保存する

```powershell
dotnet run --project src\Rulixa.Cli -- render-human `
  --workspace <target-workspace> `
  --entry <entry> `
  --goal "<goal>" `
  --mode audit `
  --out artifacts\audit.md `
  --evidence-dir artifacts\evidence
```

## 出力の読み方

### `pack`

1. まず system summary と indexes を読みます。
2. `unknowns` と next candidates から、次に全文検索する候補を選びます。
3. 必要になった範囲だけ selected snippets / selected files を確認します。

### `render-human`

- `review`
  概要、中心状態、主要 workflow、unknown / risk、次に読む file / symbol をまとめます。
- `audit`
  root entry、observed facts、evidence source、degraded diagnostics、未確定事項をまとめます。
- `knowledge`
  subsystem map、dependency seams、architectural constraints、known unknowns をまとめます。

## 効果的な使い方

1. `pack` で最初の地図を取ります。
2. `unknowns` と next candidates を見て、必要な範囲だけ全文検索します。
3. 人間向けに共有したいときは `render-human` を使います。

`Rulixa` は「全部読むツール」ではなく、「どこから読むべきかを圧縮して返すツール」です。
