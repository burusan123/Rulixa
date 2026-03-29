# Phase 6

Phase 6 は `Continuous Quality and Release Readiness` を主題にする。  
Phase 5 までで、`Rulixa` は `scan -> resolve-entry -> pack`、high-signal pack、system pack、legacy WPF compatibility、local quality gate、handoff quality observation まで到達した。  
次に必要なのは「測れる」状態を「継続運用できる」状態へ引き上げることだ。

Phase 6 の中心課題は 4 つに絞る。

- local quality gate を CI に持ち上げる
- release gate を機械判定にする
- `Rulixa -> 全文検索` handoff の品質を、観測から半自動評価へ進める
- `first useful map time` を継続観測できるようにする

このフェーズでは新しい pack schema や CLI の大きな改変は行わない。  
主対象は quality artifact、CI 実行経路、gate 判定、benchmark、diagnostics / handoff の評価基盤である。

## 読み順
1. [scope.md](scope.md)
2. [architecture.md](architecture.md)
3. [ci_and_release_gate.md](ci_and_release_gate.md)
4. [handoff_scoring.md](handoff_scoring.md)
5. [benchmark_and_telemetry.md](benchmark_and_telemetry.md)
6. [implementation_plan.md](implementation_plan.md)

## Phase 5 との差分

- Phase 5 は local quality gate までを整えた
- Phase 6 はその gate を継続運用し、release 可否まで結び付ける
- Phase 5 では handoff quality を観測した
- Phase 6 では handoff quality を半自動で評価し、quality gate の補助指標にする
- Phase 5 では実行結果を artifact と `summary.md` に出した
- Phase 6 では CI と release 判定で同じ artifact を使う

## 完了条件

- required corpus が CI 上で crash-free / success / deterministic / false-confidence を満たす
- optional smoke が観測対象として継続実行され、skip / fail / pass が artifact に残る
- handoff quality の観測値が run 単位で比較できる
- `first useful map time` の benchmark が継続記録され、退行を検知できる
- release gate が product readiness の判定材料として使える
