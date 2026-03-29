# Phase 5

Phase 5 は `Product Hardening` を主題にする。  
Phase 1 から Phase 4 までで、`Rulixa` は `scan -> resolve-entry -> pack` の基盤、high-signal pack、system pack、legacy WPF compatibility まで到達した。次に必要なのは「さらに賢くすること」より、**より多くの現実的な workspace で、安定して、説明可能に、有用な pack を返せること**である。

Phase 5 の狙いは 4 つに絞る。

- compatibility coverage を広げる
- acceptance corpus を増やす
- KPI と quality gate を定義して測る
- diagnostics / unknown guidance / handoff quality を製品品質として固める

## 読み順
1. [scope.md](scope.md)
2. [architecture.md](architecture.md)
3. [compatibility_and_corpus.md](compatibility_and_corpus.md)
4. [kpi_and_quality_gates.md](kpi_and_quality_gates.md)
5. [diagnostics_and_handoff.md](diagnostics_and_handoff.md)
6. [implementation_plan.md](implementation_plan.md)

## Phase 4 との違い

- Phase 4 は `legacy WPF でも crash-free にする` ことが中心だった
- Phase 5 は `市販レベルで安定稼働・測定・サポートできる状態にする` ことが中心になる
- そのため、主戦場は新しい section や mode ではなく、compatibility、KPI、corpus、diagnostics である

## 完了条件

- modern / legacy の複数パターンで crash-free に pack が返る
- partial pack を含めた success rate が定量把握されている
- false confidence を抑えた diagnostics / unknown guidance が返る
- `Rulixa -> 全文検索` handoff が一貫した品質で成立する
