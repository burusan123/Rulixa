# Phase 7

Phase 7 は `Measured Handoff Quality and Performance Hardening` を主題にする。  
Phase 6 までで、`Rulixa` は local quality gate と GitHub Actions 上の required gate を持ち、`summary.md` / `gate.json` / `kpi.json` を使って release 判定に入れるところまで進んだ。  
一方で、次の 4 点はまだ「観測できる」止まりであり、継続改善のための自動評価と比較運用が弱い。

- `Rulixa -> 全文検索` handoff が本当に効いているか
- 実プロジェクト corpus に対して汎用性がどこまであるか
- `first useful map time` が退行していないか
- GitHub 上で読む docs / examples が公開向けに整っているか

Phase 7 では、新しい `pack` schema や CLI は増やさず、既存の quality artifact と CI 運用の上に次の 4 本柱を積む。

- handoff hit の半自動評価
- real workspace を含む corpus 拡張
- performance benchmark の継続比較
- GitHub 向け docs / examples の hardening

このフェーズの狙いは、「地図を返せる」から「地図が有効だと測って示せる」へ進めることにある。

## 読む順番

1. [scope.md](scope.md)
2. [architecture.md](architecture.md)
3. [handoff_hit_rate_design.md](handoff_hit_rate_design.md)
4. [corpus_expansion_strategy.md](corpus_expansion_strategy.md)
5. [performance_strategy.md](performance_strategy.md)
6. [implementation_plan.md](implementation_plan.md)

## Phase 6 との差分

- Phase 6 は CI / release gate / quality artifact の運用開始が中心だった
- Phase 7 は、その artifact を使って handoff quality と performance を比較可能にする
- optional smoke は引き続き observation-only だが、Phase 7 では real workspace corpus を明示的に増やす
- `unknown_guidance_hit_rate` は warning から一歩進め、case 単位の hit/miss 観測にする
- 公開 docs に残りやすいローカル絶対パスや空の例も、Phase 7 で保守対象に含める

## 完了条件

- handoff hit を case 単位で記録できる
- real workspace corpus が複数系統に増えている
- performance の baseline と退行比較が artifact で確認できる
- required gate と advisory 指標の役割分担が明確になっている
- GitHub 上で壊れるローカル絶対パスと空の例が公開 docs / examples から除去されている
