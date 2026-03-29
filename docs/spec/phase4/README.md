# Phase 4

Phase 4 は `Legacy WPF Compatibility` を主題にする。  
Phase 3 までで `Rulixa` は modern WPF + DI ベースの workspace に対して高密度な system map を返せるようになった。一方で、古い WPF / XAML 構成、manual `new`、service locator、ResourceDictionary 多用、code-behind 主体の起動では pack 生成が失敗する余地が残っている。

`Rulixa` を市販できるレベルで汎用化するには、まず「対応できる workspace を広げる」ことと、「対応しきれない場合でも落ちずに partial pack を返す」ことが必要である。Phase 4 ではここを優先する。

## Phase 4 の主題

- legacy WPF / XAML 構成への互換性強化
- graceful degradation
  未対応構成でも crash せず、partial pack と diagnostics を返す
- product-grade diagnostics
  「なぜ薄いか」「次に何を見るべきか」を deterministic に返す

## 読み順

1. [scope.md](scope.md)
2. [architecture.md](architecture.md)
3. [compatibility_targets.md](compatibility_targets.md)
4. [graceful_degradation.md](graceful_degradation.md)
5. [diagnostics_and_acceptance.md](diagnostics_and_acceptance.md)
6. [implementation_plan.md](implementation_plan.md)

## Phase 3 との違い

- Phase 3 は `System Pack` による system-level map の完成が主目的だった
- Phase 4 は map の深さより先に、`壊れずに広く使えること` を優先する
- Phase 4 の成功条件は `より多く拾うこと` ではなく、`より多くの実際の WPF workspace に対して安定して pack を返すこと`

## 完了条件

- 古い WPF workspace でも pack が crash しない
- modern / legacy の両方で root entry から partial 以上の pack が返る
- 未対応構成では必ず diagnostics と handoff guidance が返る
- `AssessMeister` と `AssessMeister_20260204` の両方で acceptance が成立する
