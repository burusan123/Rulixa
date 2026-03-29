# Phase 8

Phase 8 は `Human Review / Audit / Design Knowledge Outputs` を主題にしたフェーズです。  
Phase 7 までで、`Rulixa` は handoff outcome、corpus / case 比較、performance baseline、public docs hardening まで揃いました。  
このフェーズでは、それらを人間がレビュー、監査、設計知蓄積に直接使える出力へ持ち上げます。

## 現在地

- required gate は GitHub Actions 上で運用できている
- handoff outcome は `hit / miss / unknown` で artifact に残る
- synthetic / observed corpus の比較が `summary.md` に出る
- performance baseline は advisory として観測できる
- optional smoke は observation-only のまま分離されている

## Phase 8 の狙い

- `Review Brief` を生成し、レビュー会でそのまま読める要約を返す
- `Audit Snapshot` を生成し、証跡と根拠を残せるようにする
- `Design Knowledge Snapshot` を生成し、設計知の蓄積に使えるようにする
- 既存の handoff / observed corpus / performance artifact を人間向け出力に接続する

## 到達点

- 人間向け出力を `pack` の上位レイヤとして追加できる
- 各出力が evidence と unknown を失わずに構成される
- release review と設計知蓄積の両方で再利用できる
- handoff / performance / observed corpus の観測値が各出力に接続される

## ドキュメント構成

1. [scope.md](scope.md)
2. [architecture.md](architecture.md)
3. [human_outputs.md](human_outputs.md)
4. [handoff_gate_design.md](handoff_gate_design.md)
5. [observed_corpus_ci_strategy.md](observed_corpus_ci_strategy.md)
6. [performance_gate_strategy.md](performance_gate_strategy.md)
7. [release_review_flow.md](release_review_flow.md)
8. [implementation_plan.md](implementation_plan.md)
