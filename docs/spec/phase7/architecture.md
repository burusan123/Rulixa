# Architecture

Phase 7 の中心は、新しい抽出器を足すことではなく、既存の `pack -> quality artifact -> CI artifact` の流れに比較軸を追加することである。

## 責務分割

### 1. Pack / Scan 層

- 既存の `scan -> resolve-entry -> pack` を維持する
- `ContextPack` 自体には handoff 採点ロジックを持ち込まない
- 既存の representative chain / unknown guidance / degraded diagnostics をそのまま quality 側へ渡す

### 2. Quality Artifact 層

- handoff 観測値を case 単位で集約する
- benchmark 指標を run 単位で集約する
- required gate と advisory 指標を分離したまま保持する
- 既存 schema は加算で拡張する

### 3. Corpus / Acceptance 層

- synthetic corpus
- real workspace corpus
- optional smoke

を同じ観測モデルで扱う。  
ただし、release gate への寄与は corpus 種別ごとに分ける。

### 4. CI / Release 層

- required gate の pass/fail は従来どおり `gate.json` を正本にする
- handoff hit と performance は advisory 指標として CI artifact に載せる
- `summary.md` で「release を止める指標」と「改善を見る指標」を分けて表示する

### 5. Docs / Example 層

- 公開 docs / examples に含まれるローカル絶対パスを管理する
- GitHub 上で参照不能な例や、情報価値の薄い空例を検出して修正する
- この責務は pack 本体ではなく docs hardening として扱う

## 依存方向

- `Rulixa.Plugin.WpfNet8` は handoff 採点ロジックに依存しない
- handoff 採点と benchmark 集計は `Infrastructure/Quality` に閉じる
- tests は corpus 定義と quality assertion を持つが、本体に評価用の特殊分岐を持ち込まない

## 設計原則

- hit/miss の採点は pack ロジックの分岐ではなく、artifact 後処理で行う
- real workspace 固有名に依存する採点を避け、family / candidate / diagnostic の形で評価する
- performance 比較は exact 秒数の固定ではなく、baseline 比較と退行率で扱う
- docs 修正は schema 変更ではなく、公開 repo の可読性と移植性の改善として分離する
