# Scope

## Summary

Phase 8 は、Phase 7 までで蓄積した `quality artifact` を使って、handoff / observed corpus / performance の扱いを「観測」から「人間が使える出力」へ進めるフェーズです。

## In Scope

- `Review Brief` の設計と出力要件定義
- `Audit Snapshot` の設計と証跡要件定義
- `Design Knowledge Snapshot` の設計と蓄積要件定義
- handoff / observed corpus / performance を各出力へ接続する方針
- release review に必要な artifact と summary の整理
- docs / product-readiness への反映

## Out of Scope

- `pack` CLI の public interface 変更
- `ContextPack` / evidence manifest の shape 変更
- pack 抽出ロジックの大規模な再設計
- observed corpus を required gate に即時昇格すること
- 完全自動の release publish
- 完全自動の設計判断生成

## Success Criteria

- 人間向け出力が `pack` や `evidence` と責務分離されている
- 各出力が `断定` と `推定` と `unknown` を分けて表現する
- observed corpus の実行カテゴリが `CI で回すもの` と `手動観測のもの` に分かれる
- release review の入口が `summary.md` と `gate.json` に固定される
