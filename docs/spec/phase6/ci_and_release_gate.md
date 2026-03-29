# CI and Release Gate

## 目的

local quality gate を「開発者が手元で回せる」状態から、「継続的に回り、release 判定に使える」状態へ持ち上げる。

## 実行区分

### Required Gate
- synthetic corpus
- deterministic regression
- weak-signal corpus

この区分は CI で常時実行し、pass/fail を release gate に直接使う。

### Observed Only
- modern real workspace smoke
- legacy real workspace smoke
- large workspace benchmark

この区分は artifact には残すが、fail しても直ちに release fail にはしない。  
ただし warning として summary に強く表示する。

## Gate 条件

- `crash_free_rate = 100%`
- `pack_success_rate = 100%` on required root cases
- `deterministic_rate = 100%`
- `false_confidence_rate = 0%`

## Advisory 指標

- `partial_pack_rate`
- `first_useful_map_time_ms`
- `unknown_guidance_case_count`
- `unknown_guidance_family_count`
- `degraded_reason_count`

これらは release gate の参考値として記録し、即 fail 条件にはしない。

## Release 判定

release 可否は次の順で決める。

1. required gate が pass
2. benchmark に重大退行がない
3. handoff quality に blocking warning がない
4. optional smoke の fail が既知例外として整理済み

## 生成物

- `kpi.json`
- `gate.json`
- `summary.md`
- benchmark 比較用 artifact
- CI 実行ログへのリンク
