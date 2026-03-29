# Performance Strategy

## 目的

`Rulixa` は読む量を減らせる一方で、workspace によっては実行時間がまだ重い。  
Phase 7 では `first useful map time` を継続比較できるようにし、pack 品質と両立した性能改善の土台を作る。

## 観測する指標

- `first_useful_map_time_ms`
- `duration_ms`
- `representative_chain_count`
- `unknown_guidance_case_count`
- `degraded_reason_count`

## 比較方法

- exact 値の固定ではなく baseline 比較
- `corpus + entry + goal` を key にして run 間比較する
- 退行は比率で見る
  - 例: `+20%` を warning
  - 極端な退行だけを fail 候補にする

## baseline の扱い

- baseline は repo tracked の固定ファイルではなく、artifact の比較対象として扱う
- GitHub Actions の直近成功 run か、ローカルで明示的に採用した run を参照する
- baseline 更新は release review の判断と分ける

## summary 表示

`summary.md` に最低限次を出す。

- corpus ごとの `first useful map time`
- baseline との差分
- representative chain 数の増減
- degraded reason 数の増減

## 設計上の注意

- performance のために signal density を落とさない
- optional smoke の real workspace 性能は advisory とする
- performance 評価は CI hosted runner とローカルを混同しない

## 完了条件

- benchmark が run artifact に継続記録される
- 退行が summary 上で読める
- release gate に入れない advisory 指標として運用できる
