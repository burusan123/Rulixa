---
name: pack
description: `Rulixa.Cli` を使って、WPF / .NET ワークスペースの Context Pack と人間向け補助資料を生成します。
---

# Rulixa Pack

`Rulixa.Cli` は、WPF / .NET ワークスペースの system map を短時間で掴むための CLI です。  
まず `pack` で地図を取り、必要に応じて `render-human` で文章資料へ、`render-visual` で探索型 UI へ展開します。

## 入力

- `entry=symbol`
  root ViewModel や主要サービスの symbol が分かっているときに使います
- `entry=file`
  root XAML や code-behind から入りたいときに使います

## 基本コマンド

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry <entry> `
  --goal "<goal>"
```

## 例

### symbol entry で system map を取る

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "システム全体の地図を作る"
```

### file entry で root XAML から入る

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry file:src/ReferenceWorkspace.Presentation.Wpf/Views/ShellView.xaml `
  --goal "Shell 画面の workflow と persistence map を説明する"
```

### review brief を出す

```powershell
dotnet run --project src\Rulixa.Cli -- render-human `
  --workspace <target-workspace> `
  --entry <entry> `
  --goal "<goal>" `
  --mode review
```

### visual output を出す

```powershell
dotnet run --project src\Rulixa.Cli -- render-visual `
  --workspace <target-workspace> `
  --entry <entry> `
  --goal "<goal>" `
  --out-dir artifacts\visual
```

## 出力の読み方

### `pack`

1. system summary と indexes を読む
2. `unknowns` と next candidates から次に見る候補を選ぶ
3. selected snippets / selected files を根拠として確認する

### `render-human`

- `review`
  概要、中心状態、workflow、unknown / risk、次に読む file / symbol をまとめる
- `audit`
  root entry、observed facts、evidence source、degraded diagnostics、未確定事項をまとめる
- `knowledge`
  subsystem map、dependency seams、architectural constraints、known unknowns をまとめる

### `render-visual`

- `index.html`
  探索用の本体です
- `app.css`
  visual artifact の見た目を定義します
- `app.js`
  埋め込み JSON を読み、検索、折りたたみ、inspector 更新を行います

## 使い分けの目安

1. `pack` で最初の地図を取る
2. `unknowns` と next candidates を見て、どこを読むか決める
3. 人間向けの説明が必要なら `render-human` を使う
4. 局所 graph や evidence を探索したいなら `render-visual` を使う
