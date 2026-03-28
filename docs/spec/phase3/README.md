# Phase 3

Phase 3 は `System Pack` を主テーマにする。
Phase 2 までで `Rulixa` は単一 entry から高密度な局所地図を返せるようになったが、`AssessMeister` のような大きな WPF ワークスペースを「システム全体として理解して説明する」用途では、複数 sub-map を束ねる仕組みが不足している。

Phase 3 では `ShellViewModel` のような root ViewModel を起点に、workflow、hub object、主要 dialog/window、主要 service を system-level に束ねる。
目的は「単一画面の詳細 pack」ではなく、「システム全体を説明し始められる最小地図」を返すことにある。

## Phase 3 の主題

- `System Pack = root 起点で複数 sub-map を圧縮統合する pack`
- 対象は `WPF + .NET 8` plugin 内に限定する
- CLI や evidence schema は原則維持し、`pack` の内部挙動として system expansion を追加する
- recall 最大化ではなく、`signal density` と `map usefulness` を主価値とする

## 読み順

1. [scope.md](scope.md)
2. [architecture.md](architecture.md)
3. [system_pack_design.md](system_pack_design.md)
4. [aggregation_and_compression.md](aggregation_and_compression.md)
5. [root_expansion_rules.md](root_expansion_rules.md)
6. [implementation_plan.md](implementation_plan.md)

## Phase 2 との差分

- Phase 2 は単一 entry を高密度に map 化することが中心だった
- Phase 3 は root から複数の局所 map を system-level に統合する
- Phase 2 の `Workflow / Persistence / Hub Objects / External Assets / Architecture Tests` を再利用しつつ、system-level representative を追加する
- `unknowns` は sub-map 単位ではなく system-level guidance として集約する

## 期待する結果

`AssessMeister` の `ShellViewModel` pack で、少なくとも次が読める状態を目標とする。

- 起動経路
- 中心状態としての `ProjectDocument`
- 永続化境界
- `Drafting`
- `Settings / Report`
- `Architecture Tests`

これにより、LLM や人間が全文検索に入る前に、システムの輪郭を短いコンテキストで掴めることを目指す。
