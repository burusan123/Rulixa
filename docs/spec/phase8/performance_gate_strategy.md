# Performance Gate Strategy

## 目的

Phase 7 では performance を advisory として比較できるようになりました。  
Phase 8 では、これを「どこまで gate 候補にするか」と「人間向け出力でどう見せるか」を整理します。

## 対象指標

- `first_useful_map_time_ms`
- `representative_chain_count`
- `unknown_guidance_case_count`
- `degraded_reason_count`

## 方針

### required gate にしないもの

- observed corpus の絶対時間
- hosted runner 依存が強い time metric

### advisory のまま強化するもの

- synthetic corpus の time delta
- corpus / case 単位の regression warning
- summary 上の悪化ケース一覧

### gate 候補として監視するもの

- synthetic corpus の極端な time regression
- representative chain の急減
- degraded reason の急増

## Phase 8 の完了条件

- regression warning の意味が固定される
- `どの悪化は warning で、どの悪化は fail 候補か` を説明できる
- release review で見る performance セクションが固定される
- `Review Brief` と `Design Knowledge Snapshot` で速度と密度の悪化を補足できる
