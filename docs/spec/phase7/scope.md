# Scope

## In scope

- `unknown guidance` の hit/miss を case 単位で記録する仕組み
- handoff quality を synthetic / real workspace の両方で比較できる corpus 拡張
- `first useful map time`、representative chain 数、degraded reason 数の継続 benchmark
- quality artifact / `summary.md` / CI artifact の比較運用強化
- optional smoke を含む observation-only 指標の整理
- product readiness に対する定量的な判断材料の追加
- GitHub 上で壊れるローカル絶対パスの除去
- 中身の薄い例や空の例を、公開向けに意味のある例へ差し替えること

## Out of scope

- 新しい `pack` CLI や mode の追加
- `ContextPack` / evidence manifest の shape 変更
- deep drilldown の一般化
- 3 hop 以上の一般探索
- 全文検索自体の自動実行や統合
- release の自動承認
- docs の全面書き換えや新しい docs システム導入

## 前提

- required gate は引き続き synthetic corpus 中心で運用する
- real workspace は Phase 7 でも advisory / observation の比重が高い
- `unknown_guidance_hit_rate` は完全自動採点ではなく、case 別の構造化判定から始める
- docs hardening は public repo 向けの整備であり、pack schema や抽出ロジックの変更を前提にしない

## 成功条件

- handoff quality が「読める」だけでなく「比較できる」
- corpus の偏りを説明できる
- performance 退行を継続 run で検知できる
- GitHub 上で壊れるリンクやローカル専用例が公開 docs に残らない
