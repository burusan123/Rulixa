# Architecture

## 基本方針

`scan -> resolve-entry -> pack` の流れは維持する。
Phase 3 では `pack` の内部に `system expansion` を追加し、root entry の場合だけ system-level candidate を広げる。

## 追加する責務

### root candidate resolution

- 解決済み entry が root seed として扱えるかを判定する
- root data context / startup root ViewModel / 明示 entry を統一的に root seed として扱う

### system expansion planning

- root seed から first expansion 対象を決める
- 対象 family:
  - workflow
  - hub object
  - major service
  - dialog / window
  - sibling ViewModel

### sub-map aggregation

- expansion で得た局所 map を 1 つの system map に束ねる
- `Shell`, `Drafting`, `Settings`, `3D`, `Report/Export` のような family 単位で sub-map を整理する

### section-level compression

- 既存 section を使って system-level representative を構成する
- sub-map ごとの重複 signal を落とし、section 内で canonicalize する

### unknown guidance aggregation

- sub-map 単位の unknown を system-level guidance に統合する
- 候補は system-level で優先順位づけし、最大 3 件に制限する

## 依存方向

- Domain / Application / Infrastructure / Plugin / CLI の責務分割は Phase 1 / 2 を維持する
- Phase 3 の追加は `Rulixa.Plugin.WpfNet8` と renderer 周辺に閉じる
- CLI や evidence schema の変更を前提にしない

## 既存コンポーネントの再利用

- `RelevantPackContext`
- `GoalExpansionProfile`
- `HighSignalSelectionSupport`
- `SectionCompressionSupport`
- 既存 section builder 群

Phase 3 はこれらの上に `system expansion planner` と `sub-map aggregator` を追加する設計とする。
