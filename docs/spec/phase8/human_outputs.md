# Human Outputs

## 目的

Phase 8 では、人間がそのまま使える 3 種類の出力を定義します。  
狙いは、`Rulixa` を「理解の入口」から「レビュー / 監査 / 設計知生成基盤」へ引き上げることです。

## 1. Review Brief

### 用途

- 実装レビュー
- 引き継ぎ
- システム理解共有

### 最低限含めるもの

- システム概要
- 中心状態
- 主要 workflow
- persistence / external assets
- unknown / risk
- 次に読むべき file / symbol

### 禁止

- unknown を隠して断定すること
- evidence なしの設計断定

## 2. Audit Snapshot

### 用途

- 監査
- 証跡
- 変更理由の記録

### 最低限含めるもの

- root entry
- observed facts
- evidence source
- degraded diagnostics
- compare-evidence 差分

### 禁止

- 推定を事実として書くこと
- raw exception だけで終わること

## 3. Design Knowledge Snapshot

### 用途

- 設計知の蓄積
- 将来変更時の参照
- 境界整理

### 最低限含めるもの

- subsystem map
- center state
- dependency seams
- architectural constraints
- known unknowns

### 禁止

- 一時的なコード構造をそのまま恒久知識として固定すること
- UI ノイズを設計知として残すこと

## 4. 共通原則

- 断定 / 推定 / unknown を分ける
- evidence への参照を持つ
- compare-evidence と接続できる
- handoff 候補を次の調査に使える
- `pack` と責務を混ぜない
