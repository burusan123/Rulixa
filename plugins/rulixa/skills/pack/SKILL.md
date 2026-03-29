---
name: pack
description: `Rulixa.Cli` を使って、WPF / .NET ワークスペースの Context Pack を `entry=file` または `entry=symbol` で生成します。
---

# Rulixa Pack

ローカルワークスペースから `Rulixa.Cli` で Context Pack を生成したいときに使います。

## このスキルの目的

- 主導線は `pack` にする。
- `entry=file` と `entry=symbol` の両方を扱う。
- `scan` と `resolve-entry` は補助コマンドとして扱う。
- まず system map を作り、実装レベルの検証が必要なときだけ全文検索に進む。

## 事前に確認する入力

コマンド実行前に次を確認する。

- `workspace`
  解析対象ワークスペースのパス。
- `entry`
  対象の ViewModel や型名が分かっているなら `symbol:` を使う。
  ユーザーが XAML やコードファイルを直接指定しているなら `file:` を使う。
- `goal`
  生成する Context Pack に含める目的。
- 任意の budget override
  `--max-files`
  `--max-total-lines`
  `--max-snippets-per-file`

ユーザーが budget を指定しないなら CLI の既定値を使う。

## 効果的な使い方

1. まず `pack` を使って system map を作る。
2. 対象が既知の ViewModel なら `entry=symbol` を優先する。
3. ユーザーが XAML を指しているなら `entry=file` を使う。
4. `resolve-entry` は entry が曖昧なときだけ使う。
5. `scan` は raw IR やスキャン結果そのものが必要なときだけ使う。

### 使い分けの原則

- `Rulixa` は「最小コンテキストで全体地図を得る」ときに強い。
- 全文検索は「実装の根拠を確認する」「深掘りする」ときに強い。
- 迷ったら次の順で進める。
  1. `pack`
  2. pack の `unknowns` と代表チェーンを確認
  3. 必要な箇所だけ全文検索で検証

## コマンド

`Rulixa` リポジトリのルートから実行する。

### メインコマンド

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry <entry> `
  --goal "<goal>"
```

### 例: symbol entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace D:\C#\AssessMeister `
  --entry symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "AssessMeister の全体構造を理解する"
```

### 例: file entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace D:\C#\AssessMeister `
  --entry file:src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml `
  --goal "Shell 画面の workflow と persistence map を理解する"
```

### 補助: resolve-entry

```powershell
dotnet run --project src\Rulixa.Cli -- resolve-entry `
  --workspace <target-workspace> `
  --entry <entry>
```

### 補助: scan

```powershell
dotnet run --project src\Rulixa.Cli -- scan `
  --workspace <target-workspace>
```

## 出力の見方

- 主出力は Markdown。
- pack には少なくとも `goal`、`resolved entry`、`contracts`、`index`、`selected files`、`unknowns` が含まれる。
- `ShellViewModel` のような root entry では、個別ページを列挙するよりも system map が先に読めることを重視する。
- `unknowns` は失敗ではなく「次に全文検索で確認すべき候補」として扱う。

## 期待する進め方

- `ShellViewModel` や root ViewModel を起点にすると、`Shell / Drafting / Settings / 3D / Report` のような system map を得やすい。
- `DraftingWindowViewModel` のような局所 entry を起点にすると、workflow、hub object、persistence の関係を短く把握しやすい。
- pack の時点で十分に説明できるなら、そのまま要約する。
- algorithm 実装や永続化の詳細まで必要なら、pack に出た候補を起点に全文検索へ進む。
