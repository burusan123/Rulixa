# Scope

## In Scope

- WPF + .NET workspace に対する compatibility coverage の拡張
- acceptance corpus の整備
- KPI / quality gate の定義と測定導線
- degraded pack / unknown guidance / diagnostics の品質改善
- compare-evidence を product validation に使うための差分観点整理
- `Rulixa -> 全文検索` handoff quality の明文化

## Out of Scope

- 新しい public CLI command の追加
- `ContextPack` / evidence manifest の破壊的変更
- WPF 以外の UI framework 対応
- 3 hop 以上の一般探索
- 全文検索代替を目指す recall 最大化
- `map/drilldown` mode 分離の実装
- 汎用 LSP / IDE integration

## Priorities

1. crash-free
2. compatibility breadth
3. diagnostic quality
4. measurable quality gates
5. corpus-driven regression

## Non-Goals

- 最短で詳細 drilldown を増やすこと
- すべての workspace を完全理解できるようにすること
- pack 本文の件数を増やすこと自体
