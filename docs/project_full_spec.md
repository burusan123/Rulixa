# Rulixa Full Spec

## 0. この文書について

この文書は `Rulixa` の全体仕様を短くまとめたものです。  
各 Phase の詳細仕様より上位にあり、現在の到達点と主要な出力面を整理します。

## 1. 目的

`Rulixa` は、設計レビュー、監査、全文検索の前段として使える高密度な Context Pack を生成するツールです。  
全部読む前に、どこから理解すべきかを圧縮して返すことを主目的にします。

## 2. 主な機能

### 2.1 Context Pack

- WPF / .NET ワークスペースを scan する
- `entry=file` / `entry=symbol` から root を解決する
- root binding、navigation、dialog activation、dependency injection、workflow を pack にまとめる

### 2.2 Evidence Bundle

- `pack --evidence-dir` で scan / resolved-entry / pack をまとめて出力する
- `compare-evidence` で 2 つの bundle を比較する

### 2.3 Quality Artifact

- local quality gate と GitHub Actions で `kpi.json` / `gate.json` / `summary.md` を出力する
- required gate と advisory 指標を分離して運用する
- release review 用に `release-review.md` と synthetic root cases 向け `human-outputs/` を補助 artifact として出力する

### 2.4 Human Output

- `render-human` で review / audit / knowledge の文章を生成する
- release review では `summary.md` を一次資料、`release-review.md` と `human-outputs/` を補助資料として使う

### 2.5 Visual Output

- `render-visual` で `index.html` / `app.css` / `app.js` の探索型 artifact を生成する
- 5 view は `Overview` / `Workflow` / `Evidence` / `Unknowns` / `Architecture`
- local / GitHub の release gate 自体には入れず、探索用の補助資料として扱う

## 3. アーキテクチャ

### 3.1 プロジェクト

- `src/Rulixa.Domain`
- `src/Rulixa.Application`
- `src/Rulixa.Infrastructure`
- `src/Rulixa.Plugin.WpfNet8`
- `src/Rulixa.Cli`
- `plugins/rulixa`

### 3.2 境界

- Domain
  - ルールとモデル
- Application
  - ユースケース
- Infrastructure
  - ファイルシステム、artifact、rendering
- Plugin
  - WPF / .NET 8 抽出
- CLI / Plugin
  - 入出力面

## 4. 品質原則

- crash-free を最優先にする
- false confidence を抑制する
- degraded でも diagnostics と next candidates を返す
- local quality gate と GitHub Actions の required gate を維持する
- optional smoke は observation-only として扱う

## 5. セキュリティと公開方針

- secret を docs や artifact に残さない
- public-facing docs にローカル絶対パスを残さない
- 実 workspace 名を説明の主語にしない
- plugin metadata と examples は GitHub 上で読める形を維持する

## 6. 現在のフェーズ

- Phase 1
  scan / resolve-entry / pack の基盤
- Phase 2
  高信号 sections
- Phase 3
  system pack
- Phase 4
  legacy WPF compatibility
- Phase 5
  quality artifact と local quality gate
- Phase 6
  GitHub Actions と release gate
- Phase 7
  handoff scoring と corpus / case 比較
- Phase 8
  human output (`render-human`) と release review artifact
- Phase 9
  visual output (`render-visual`) による探索型 artifact
