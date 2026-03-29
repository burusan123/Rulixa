# Benchmark and Telemetry

## 目的

`Rulixa` の品質を「通る / 落ちる」だけでなく、速度と圧縮品質の両面で継続観測できるようにする。

## 継続観測する値

- `first_useful_map_time_ms`
- `representative_chain_count`
- `unknown_guidance_case_count`
- `unknown_guidance_item_count`
- `unknown_guidance_family_count`
- `degraded_reason_count`

## ベンチマーク区分

### Synthetic benchmark
- 小さい corpus
- deterministic
- CI で毎回実行可能

### Real workspace benchmark
- `<modern-real-workspace>`
- `<legacy-real-workspace>`

この区分はローカルまたは専用 runner で実行し、観測値として保存する。

## 退行判定

- `first_useful_map_time_ms` の大幅悪化
- representative chain の急減
- degraded reason の急増
- unknown guidance family の消失

これらは warning または fail の候補として扱う。  
閾値は Phase 6 初期では緩めに設定し、運用しながら調整する。

## 表示

`summary.md` には以下を固定表示する。

- representative chain 総数
- unknown guidance family 一覧
- next candidate 上位
- degraded diagnostic / degraded reason 件数
- first useful map time の代表値

## 位置づけ

benchmark は性能最適化そのものではなく、  
「今の変更で product hardening を壊していないか」を見る観測基盤として扱う。
