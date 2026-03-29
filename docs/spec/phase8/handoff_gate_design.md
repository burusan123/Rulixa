# Handoff Gate Design

## 目的

Phase 7 では handoff outcome を artifact に残せるようになりました。  
Phase 8 では、それを `Review Brief` と `Audit Snapshot` に接続できる「半自動スコア」へ進めます。

## 判定対象

- synthetic root case
- synthetic weak-signal case
- observed corpus case

## 判定の考え方

### `hit`

- representative chain または guided unknown が期待 family に届く
- first candidate が UI ノイズだけで終わらない

### `unknown`

- false confidence はない
- guided unknown はある
- degraded だが diagnostic と next candidate が返る

### `miss`

- expected family に届かない
- first candidate が UI ノイズのみ
- raw failure に寄りすぎて次候補が読めない

## Phase 8 の追加

- case 単位の outcome を review score に変換する
- corpus 単位で `hit rate / miss rate / unknown rate` を見る
- release fail 条件にはまだ直結させず、warning と人間向け出力の入力として使う

## 完了条件

- `summary.md` から corpus ごとの handoff quality を読める
- miss case の first candidate と reason を review できる
- `Review Brief` と `Audit Snapshot` に handoff 結果を埋め込める
