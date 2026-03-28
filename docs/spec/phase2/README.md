# Phase 2

このフォルダは `Rulixa` の Phase 2 仕様をまとめる。
Phase 2 の目的は、Phase 1 の「入口を素早く掴む」強みを維持したまま、全文検索に負けていた「実際の仕事の流れの圧縮」を強化することにある。

Phase 2 では `pack` を単なる入口要約ではなく、`goal` に応じて重要な因果鎖を 2 hop 前後で辿る高密度マップへ進化させる。
ここで目指すのは「検索量の増加」ではなく「少ないコンテキストで重要事実へ到達できる圧縮品質」である。

## Phase 2 の主要テーマ

- `goal` 駆動の多段展開
- partial class / partial record の統合
- 高シグナル section の追加
- `unknowns` と `confidence` の厳密化
- 「次に読むべき箇所」を返せる Pack

## ドキュメント一覧

- [scope.md](scope.md)
  Phase 2 の対象、非対象、成功条件
- [architecture.md](architecture.md)
  Phase 2 の責務分割と追加コンポーネント
- [goal_driven_expansion.md](goal_driven_expansion.md)
  `goal` を使った 2 hop 展開と unknowns / confidence の仕様
- [partial_symbol_unification.md](partial_symbol_unification.md)
  partial 宣言を 1 つのシンボルとして扱う仕様
- [high_signal_sections.md](high_signal_sections.md)
  Workflow / Persistence / ExternalAsset / ArchitectureTest / HubObject 抽出
- [implementation_plan.md](implementation_plan.md)
  実装順、検証、段階リリース案

## 読み順

1. [scope.md](scope.md)
2. [architecture.md](architecture.md)
3. [goal_driven_expansion.md](goal_driven_expansion.md)
4. [partial_symbol_unification.md](partial_symbol_unification.md)
5. [high_signal_sections.md](high_signal_sections.md)
6. [implementation_plan.md](implementation_plan.md)

## 背景

`AssessMeister` を題材にした比較では、Phase 1 の `pack` は次の点で強かった。

- `ShellViewModel` や `DraftingWindowViewModel` の入口特定が速い
- WPF の root binding / navigation / DI の要約が速い

一方で次の点では全文検索に負けた。

- 入口の先にある Application / Infrastructure / Domain の因果鎖を十分に辿れない
- partial class 分割された巨大 ViewModel の全体像を圧縮できない
- 実際には未展開なのに `unknowns` が空になる
- 設定、永続化、外部資産、アーキテクチャテストのような高シグナル情報を落とす

Phase 2 はこの差分を埋める。
