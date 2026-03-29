# Architecture

Phase 5 では大きな public interface は増やさず、既存の `scan -> resolve-entry -> pack` パイプラインの中に product hardening 用の責務を追加する。

## 責務分割

### 1. Compatibility Layer

- legacy / modern の構文差を吸収する
- unsupported construct で fail-fast せず degraded signal に落とす
- extractors ごとの tolerant parsing を統一ポリシーで扱う

### 2. Corpus Validation Layer

- synthetic fixture
- real workspace optional smoke
- golden / regression

を一貫した acceptance matrix として扱う。

### 3. Quality Measurement Layer

- pack success rate
- partial pack rate
- crash-free rate
- unknown guidance hit rate
- false confidence rate
- deterministic rate

を定義し、CI・optional smoke・manual acceptance のいずれで測るかを固定する。

### 4. Handoff Quality Layer

- unknown guidance
- diagnostics
- compare-evidence
- representative chain

の品質を「次の探索にどう繋がるか」で評価する。

## 境界

- scanner / extractor は compatibility と diagnostics を担う
- pack builder / renderer は map usefulness と handoff quality を担う
- tests / corpus は acceptance と KPI の事実源になる

## 設計原則

- unsupported construct を例外で止めない
- degraded の理由は必ず observable にする
- modern WPF の品質を落とさずに legacy 対応を加える
- product quality は実装者感覚ではなく corpus と KPI で判断する
