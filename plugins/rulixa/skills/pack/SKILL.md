---
name: pack
description: `Rulixa.Cli` を使って、WPF / .NET ワークスペースの Context Pack を `entry=file` または `entry=symbol` で生成します。
---

# Rulixa Pack

`Rulixa.Cli` の `pack` は、WPF / .NET ワークスペースの system map を素早く得るための入口です。

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

### symbol entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "システム全体の地図を理解する"
```

### file entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry file:src/ReferenceWorkspace.Presentation.Wpf/Views/ShellView.xaml `
  --goal "Shell 画面の workflow と persistence map を理解する"
```

### entry 解決だけ確認する

```powershell
dotnet run --project src\Rulixa.Cli -- resolve-entry `
  --workspace <target-workspace> `
  --entry <entry>
```

### scan のみ実行する

```powershell
dotnet run --project src\Rulixa.Cli -- scan `
  --workspace <target-workspace>
```

## 効果的な使い方

1. まず `pack` で system map を取得します。
2. `unknowns` と next candidates を見て、未確定部分の次候補を確認します。
3. 本当に詳細が必要な箇所だけ全文検索で深掘りします。

`Rulixa` は「全文を説明しきる」ツールではなく、「どこから読むべきかを整理して返す」ツールです。
