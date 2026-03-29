# Rulixa Full Spec

## 0. 位置づけ

この文書は `Rulixa` の全体仕様です。  
Phase ごとの詳細仕様より上位にあり、目的、主機能、アーキテクチャ、品質方針、運用方針をまとめます。

## 1. 目的

`Rulixa` は、設計レビュー、保守、移行、調査に使える高密度な Context Pack を生成するためのツールです。  
全文検索を置き換えることではなく、「最初に何を理解すべきか」を圧縮して返すことを主目的にします。

## 2. 主な機能

### 2.1 Context Pack の生成

- WPF / .NET ワークスペースを scan する
- `entry=file` / `entry=symbol` / `entry=auto` で入口を解決する
- root binding、navigation、dialog activation、dependency injection、workflow の要点を pack にまとめる

### 2.2 Evidence bundle

- `pack --evidence-dir` で scan / resolved-entry / pack をまとめて出力する
- `compare-evidence` で 2 つの bundle の差分を読む

### 2.3 Quality artifact

- local quality gate と GitHub Actions で `kpi.json` / `gate.json` / `summary.md` を出力する
- required gate と advisory 指標を分離して継続運用する

### 2.4 Handoff

- `Rulixa -> 全文検索` handoff を前提にする
- `unknowns` と next candidates で「次にどこを掘るか」を返す
- handoff outcome と performance を artifact に記録する

## 3. 技術構成

### 3.1 プロジェクト

- `src/Rulixa.Domain`
- `src/Rulixa.Application`
- `src/Rulixa.Infrastructure`
- `src/Rulixa.Plugin.WpfNet8`
- `src/Rulixa.Cli`
- `plugins/rulixa`

### 3.2 アーキテクチャ

- Domain
  - ルールとモデル
- Application
  - ユースケース
- Infrastructure
  - ファイルシステム、artifact、rendering
- Plugin
  - WPF / .NET 8 抽出
- CLI / Plugin
  - 外部入出力

## 4. 品質方針

- crash-free を最優先にする
- false confidence を許容しない
- degraded でも diagnostics と next candidates を返す
- local quality gate と GitHub Actions で required gate を回す
- optional smoke は observation-only として扱う

## 5. セキュリティと公開方針

- secret を docs や artifact に残さない
- public-facing docs にローカル絶対パスを残さない
- 固有 workspace 名より構造カテゴリを主語にする
- plugin metadata と examples は GitHub 上で読める形を維持する

## 6. 現在の到達点

- Phase 1
  scan / resolve-entry / pack の土台
- Phase 2
  高シグナル sections
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
