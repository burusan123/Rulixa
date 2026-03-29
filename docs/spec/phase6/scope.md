# Scope

## In Scope

- local quality gate の CI 連携
- quality artifact を使った release gate 判定
- required synthetic corpus と optional smoke の役割分離
- handoff quality の半自動評価
- `first useful map time` の継続観測
- benchmark / regression / diagnostics の集約表示
- product readiness の実運用ルール化

## Out of Scope

- 新しい pack mode の追加
- deep drilldown の一般化
- 3 hop 以上の一般探索
- `ContextPack` / evidence manifest の破壊的変更
- 全文検索の代替を目指す深掘りエンジン化
- 新しいユーザー向け対話 CLI

## 前提

- Phase 5 までの local quality gate は維持する
- CI 連携は既存 artifact を再利用する
- handoff quality は完全自動採点ではなく、まず半自動評価に留める
- optional smoke は環境依存が強いため、引き続き観測対象として扱う

## 成功指標

- CI 上で required corpus の gate 判定が機械実行できる
- run ごとの差分が artifact で比較できる
- release 可否を人手の勘ではなく gate と readiness checklist で説明できる
