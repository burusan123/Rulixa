# Phase 7

Phase 7 は `Measured Handoff Quality and Performance Hardening` を主題にしたフェーズです。  
Phase 6 までで local quality gate と GitHub Actions 上の required gate は運用可能になったため、このフェーズではその artifact を使って handoff 品質と performance を比較可能にします。

## 目的

- `Rulixa -> 全文検索` handoff の hit / miss / unknown を case 単位で評価する
- synthetic / observed corpus を拡張し、複数構造で比較できるようにする
- `first useful map time` などの performance 指標を baseline 比較できるようにする
- GitHub 上で読まれる docs / examples / plugin metadata を hardening する

## 到達点

- handoff outcome を quality artifact に記録できる
- corpus / case 単位の比較を `summary.md` に出せる
- observed corpus を observation-only のまま複数カテゴリで扱える
- public-facing docs に壊れるローカル絶対リンクや空例が残らない

## ドキュメント構成

1. [scope.md](scope.md)
2. [architecture.md](architecture.md)
3. [handoff_hit_rate_design.md](handoff_hit_rate_design.md)
4. [corpus_expansion_strategy.md](corpus_expansion_strategy.md)
5. [performance_strategy.md](performance_strategy.md)
6. [implementation_plan.md](implementation_plan.md)
