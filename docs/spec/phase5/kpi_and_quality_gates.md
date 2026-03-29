# KPI And Quality Gates

## Core KPI

| KPI | 定義 | 目的 |
|---|---|---|
| `pack_success_rate` | top-level failure なしに pack を返した割合 | 基本品質 |
| `partial_pack_rate` | degraded だが有用な map を返した割合 | graceful degradation |
| `crash_free_rate` | 例外停止せず終了した割合 | release gate |
| `first_useful_map_time` | 最初の有用な map までの時間 | 体験品質 |
| `unknown_guidance_hit_rate` | unknown candidate から正しい次探索に繋がった割合 | handoff quality |
| `false_confidence_rate` | 分かったふりをした pack の割合 | 信頼性 |
| `deterministic_rate` | 同一入力で同一出力になる割合 | 再現性 |

## Quality Gates

### 開発中

- 新しい compatibility 対応は regression fixture を伴う
- `dotnet test .\Rulixa.sln` が通る
- 対象 workspace の optional smoke が通る

### Phase 5 完了判定

- `AssessMeister` と `AssessMeister_20260204` の両方で crash-free
- synthetic corpus の主要パターンで partial pack 以上を返す
- false confidence の新規悪化がない
- compare-evidence で改善点が説明可能

## 測定運用

- success / crash-free / deterministic は CI 向け
- partial / unknown guidance / false confidence は manual acceptance と golden review 向け
- first useful map time は benchmark で追跡する

## 改善判定の基準

- 件数増加は改善条件にしない
- representative chain が明確になること
- unknown candidate が妥当になること
- degraded reason が説明可能になること

を改善とみなす。
