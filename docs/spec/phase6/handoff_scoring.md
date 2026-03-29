# Handoff Scoring

## 目的

`Rulixa -> 全文検索` handoff の品質を、完全自動採点ではなく半自動評価できるようにする。

## 基本方針

- `unknown_guidance_hit_rate` を即座に数値一本へ潰さない
- case 単位で「次に見る候補」が妥当かを評価できる形にする
- family ごとの期待値を定義し、warning を出せるようにする

## 評価対象

### Representative Chain
- root から主要 sub-map へ届いているか
- system map に必要な family が抜けていないか

### Unknown Guidance
- next candidate が 1 件以上あるか
- family が期待に合っているか
- UI ノイズだけで終わっていないか
- degraded case でも exception text ではなく diagnostic + candidate になっているか

## ケース別期待値

### Drafting 系
- `Drafting`
- `Algorithm`
- `Analyzer`
- `Persistence`

のいずれかを unknown guidance family に含める。

### Settings / Report 系
- `Settings`
- `Report/Export`

の family が出ること。  
first candidate は `Overlay`、`Prompt`、`Renderer` のような UI ノイズで終わらないこと。

### Legacy degraded 系
- raw exception を返さない
- `xaml.parse-degraded` などの diagnostic が残る
- 次に見る file / symbol が候補として出る

## 出力

- case ごとの first candidate
- family 一覧
- representative chain 数
- warning 一覧

## Gate との関係

handoff quality は Phase 6 では advisory 扱いとする。  
ただし warning の件数・内容は `summary.md` と release review に必ず出す。
