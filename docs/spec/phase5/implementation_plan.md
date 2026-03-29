# Implementation Plan

## 実装順

1. compatibility coverage の追加
2. synthetic corpus の拡張
3. real workspace optional smoke の強化
4. diagnostics / unknown guidance の品質改善
5. KPI / quality gate の文書化と計測導線の整理
6. compare-evidence の validation 観点整理

## 最初のスライス

最初に着手するのは、Phase 4 の延長として次の 3 点をまとめる。

- helper / adapter を 1 段挟む legacy route 対応
- ResourceDictionary / commented XAML edge case の regression 拡充
- `AssessMeister_20260204` の pack 品質改善

## テスト方針

- bug fix ごとに synthetic fixture を追加する
- `AssessMeister` と `AssessMeister_20260204` は optional smoke で守る
- deterministic regression は維持する
- compare-evidence の期待値は representative / unknown guidance 中心で固定する

## 完了条件

- corpus の主要パターンで crash-free
- legacy / modern の両系統で partial pack 以上
- diagnostics が handoff guide として使える
- KPI の測定方法と release gate が文書化されている

## Phase 5 後の扱い

Phase 5 完了後に初めて、次フェーズとして以下を再開してよい。

- `map/drilldown` mode 分離
- 3 hop 以上の探索
- deep drilldown の高度化

その時点でも、compatibility / KPI / corpus を壊さないことを前提とする。
