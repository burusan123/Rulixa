# Architecture

## 基本方針

Phase 8 では新しい抽出層は増やしません。  
既存の `pack / evidence / quality artifact` の上に、人間向けの出力レイヤを追加します。

## 主な構成

### 1. Quality Artifact

- `kpi.json`
- `gate.json`
- `summary.md`

これらを引き続き正本として扱います。Phase 8 では additive なフィールド追加だけを許可します。

### 2. Human Output Layer

- `Review Brief`
- `Audit Snapshot`
- `Design Knowledge Snapshot`

これらは `pack` の置き換えではなく、`pack / evidence / quality artifact` を材料に組み立てる上位出力です。

### 3. Handoff Evaluation

- case 単位の `hit / miss / unknown`
- corpus 単位の ratio
- warning / review 対象の分類

### 4. Observed Corpus

- synthetic corpus
  - required gate
- observed corpus
  - observation-only
  - 一部は CI 手動実行や専用 runner に寄せる

### 5. Performance

- `first_useful_map_time_ms`
- `representative_chain_count`
- `unknown_guidance_case_count`
- `degraded_reason_count`

case 単位と corpus 単位の両方で baseline 比較できるようにします。

### 6. Release Review

- required gate の pass/fail
- advisory warning の一覧
- observed corpus の状態
- performance regression の要約

この順でレビューできることを architecture 上の前提にします。
