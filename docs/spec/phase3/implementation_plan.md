# Implementation Plan

## 実装順

### 1. root seed / system expansion planner

- root seed 判定を追加する
- root から first expansion する planner を追加する
- family ごとの sub-map candidate を収集する

### 2. sub-map aggregation

- family 単位で局所 map を束ねる
- root / sibling / dialog / sub-window を横断した route を canonicalize する

### 3. section compression / unknown aggregation

- 既存 section に system-level representative を載せる
- sub-map unknown を system-level guidance に集約する

### 4. renderer / compare-evidence

- system-level summary を renderer に追加する
- compare-evidence で sub-map representative の追加が読み取れるようにする

### 5. fixture / smoke / regression

- root から sibling sub-map が統合される fixture を追加する
- optional smoke で `RealWorkspace` の `ShellViewModel` system pack を検証する

## Acceptance

### fixture / unit

- root ViewModel から sibling sub-map が統合される
- dialog / window 経由で主要サブシステムが拾われる
- system-level で hub object / persistence / external asset / architecture test が重複せず圧縮される
- system-level unknown が sub-map unknown を集約し、候補が 3 件以内で出る
- 同じ workspace で deterministic に同じ pack が出る

### optional smoke

- `RealWorkspace` の `ShellViewModel` pack で `Shell + Drafting + Settings/Report + Architecture` が読める
- `ProjectDocument` が中心状態として残る
- drafting 系が direct chain か guided unknown のどちらかで含まれる

### compare-evidence

- Phase2 pack と比較して、件数増加ではなく system-level representative の追加が読める

## Public Interfaces

- 新しい CLI コマンドは追加しない
- `pack` を維持し、root entry のときに内部で system expansion を有効化する
- `ContextPack` と evidence manifest の shape は変更しない
- `pack --mode map|drilldown` は future backlog として扱う

## Backlog

- `pack --mode map|drilldown`
- helper / lambda / adapter をまたぐ一般 deep drilldown 強化
- WPF 以外の plugin 横展開
