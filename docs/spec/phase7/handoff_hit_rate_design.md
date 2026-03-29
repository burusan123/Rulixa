# Handoff Hit Rate Design

## 目的

Phase 6 までの `unknown guidance` は「次に見る候補」を返せるが、その候補が本当に有効だったかの評価は warning と人手確認に留まっていた。  
Phase 7 では、`unknown_guidance_hit_rate` を完全自動採点にはせず、**case 単位の hit/miss/unknown** として構造化する。

## 判定単位

1 case = 1 workspace + 1 entry + 1 goal

各 case に対して、次を記録する。

- representative chain が十分だったか
- unknown guidance が出たか
- first candidate が family として妥当か
- candidate 群に期待 family が含まれているか
- degraded diagnostic が raw exception ではなく説明として出ているか

## 判定カテゴリ

- `hit`
  representative chain または unknown guidance が、期待 family を含み、次に見る候補として有効
- `miss`
  candidate が UI ノイズで終わる、または期待 family が含まれない
- `unknown`
  guidance 自体は出ているが、自動判定では有効性を決められない

## family ベースの期待値

### Drafting 系

- `Drafting`
- `Algorithm`
- `Analyzer`
- `Persistence`

のいずれかが candidate family に含まれること

### Settings / Report 系

- `Settings`
- `Report/Export`
- `External Assets`

のいずれかに届くこと  
`Overlay`、`Prompt`、`Renderer` だけで終わる場合は miss とみなす

### degraded case

- raw exception 単独ではなく diagnostic + candidate を含むこと
- `xaml.parse-degraded` のような説明可能な code が見えること

## Artifact 追加項目

- `handoff_outcome`: `hit` / `miss` / `unknown`
- `handoff_expected_families`
- `handoff_observed_families`
- `handoff_first_candidate`
- `handoff_reason`

## Gate への扱い

- Phase 7 では release fail 条件にはまだ入れない
- `summary.md` と CI artifact では corpus ごとの hit/miss 比率を表示する
- 将来のフェーズで required gate 候補へ昇格できるよう、schema を安定化する
