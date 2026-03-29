# Diagnostics And Handoff

## 目的

Phase 5 の diagnostics は「失敗理由を表示する」だけでなく、**次の探索へ安全に handoff するためのガイド**でなければならない。

## Diagnostics の要件

各 degraded / unknown / unresolved signal は次を必須にする。

- 既知の事実
- 停止点
- 次に見る候補 1〜3 件
- その候補を出した理由

## Unknown Guidance の品質要件

- family 単位で集約されていること
- 重複候補が乱立しないこと
- 候補が root / goal と関係していること
- 低 confidence の推測だけで候補順を決めないこと

## Handoff の基本フロー

1. Rulixa で system map を得る
2. representative chain と unknowns を読む
3. 次に読むべき file / symbol を 1〜3 件に絞る
4. 必要時だけ全文検索で深掘る

## compare-evidence の役割

compare-evidence は件数差分を見るものではなく、以下を確認するために使う。

- representative chain が改善したか
- unknown guidance が改善したか
- degraded reason がより説明可能になったか
- UI ノイズが減ったか

## 禁止事項

- unsupported construct を黙って無視する
- unknown を空にして分かったふりをする
- candidate を大量列挙して手掛かりを薄める
- raw exception をそのまま product diagnostics に出す
