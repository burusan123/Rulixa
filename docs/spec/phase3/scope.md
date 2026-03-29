# Scope

## In Scope

- root ViewModel 起点の system-level pack
- workflow / persistence / hub object / external asset / architecture test の system 集約
- 主要 dialog / window / sibling ViewModel / sub-workspace への拡張
- 既存 `pack` shape の加算的強化
- system-level unknown guidance の集約
- `RealWorkspace` を acceptance 題材にした optional smoke

## Out of Scope

- 新規 plugin 抽象の導入
- WPF 以外の新規 plugin 対応
- 3 hop 以上の一般探索
- 全文検索代替を目指す recall 最大化
- CLI / `ContextPack` / evidence manifest の破壊的変更
- `pack --mode map|drilldown` の正式実装
- 一般的な helper / lambda 深掘り強化の全面対応

## スコープ境界

Phase 3 は `System Pack` を成立させるために必要な範囲だけを追加する。
そのため、helper / lambda の追跡改善は system expansion に必要な場合のみ対象とし、単一 entry の深掘り改善を主目的にはしない。

また、system pack は新しい公開コマンドではなく、既存 `pack` が root entry に対して system-level に振る舞う拡張として定義する。
