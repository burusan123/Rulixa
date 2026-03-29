# Implementation Plan

## 実装順

1. CI から local quality gate と同じ実行経路を呼べるようにする
2. run artifact を release gate 向けに集約する
3. handoff quality warning を summary と gate に追加する
4. benchmark / telemetry の継続観測を追加する
5. product readiness と release 判定ルールを接続する

## 最初のスライス

最初に着手するのは次の 3 点に限定する。

- CI 用 runner の追加
- release gate JSON の整備
- `summary.md` の handoff warning 強化

理由は、Phase 5 の資産を最も少ない変更で運用に乗せられるためである。

## テスト方針

- existing acceptance matrix を維持する
- existing quality artifact tests を維持する
- CI runner は synthetic corpus のみで pass/fail を固定する
- optional smoke は skip / fail / pass を artifact に残す
- benchmark 値は exact match ではなく範囲または presence で確認する

## 受け入れ条件

- CI で required gate が実行できる
- release gate が JSON と markdown で確認できる
- handoff warning が summary に出る
- benchmark 観測値が artifact に残る
- `RealWorkspace` と `LegacyRealWorkspace` が観測対象として維持される

## 次フェーズに送るもの

- `unknown_guidance_hit_rate` の厳密自動採点
- mode 分離
- deep drilldown
- 3 hop 以上の一般探索

Phase 6 は product hardening の運用完成を主題にし、pack 自体の機能高度化は次へ送る。
