# Phase 2 アーキテクチャ

## 方針

Phase 2 では `BuildContextPackUseCase` の責務を壊さず、`contractExtractor` の内部強化として実装する。
中心方針は「scan 済み IR を再利用しつつ、goal と symbol aggregation を使って高シグナル section を増やす」ことにある。

## 追加する責務

### 1. Goal-driven expansion

`resolvedEntry` を起点に、`goal` に応じて relevant context を広げる責務。
Phase 1 の `RelevantPackContextFactory` は View / ViewModel / Navigation に強いが、Phase 2 ではこれに「因果鎖候補」の収集を追加する。

候補例:

- Command 実行先
- private helper 1 hop
- Application service / UseCase
- Repository / Query / Saver
- 設定 Query / マスタ読込
- 外部資産参照
- テストによる依存方向保証

### 2. Symbol aggregation

`partial` 宣言を 1 つの symbol unit として扱う責務。
scan 時点または pack 時点で、同じ fully-qualified symbol に属する複数ファイルを束ねる。

### 3. High-signal sections

WPF 固有 section に加えて、次の section を plugin 側に追加する。

- WorkflowPackSectionBuilder
- PersistencePackSectionBuilder
- ExternalAssetPackSectionBuilder
- ArchitectureTestPackSectionBuilder
- HubObjectPackSectionBuilder

## 推奨構成

### Application

- `BuildContextPackUseCase`
  既存の orchestrator。大きくは変えない

### Plugin.WpfNet8

- `Extraction/Context/RelevantPackContextFactory`
  既存拡張点。Phase 2 の関連候補ノード収集を追加
- `Extraction/Context/GoalDrivenExpansionPlanner`
  `goal` から優先ノード種別を決める
- `Extraction/Context/SymbolAggregateResolver`
  partial 宣言をまとめる
- `Extraction/Sections/*`
  high-signal section を追加

### Domain

新しい複雑な型は増やしすぎない。
ただし section 間で共有する最小単位として次を追加してよい。

- `SymbolAggregate`
- `ExpansionHint`
- `UnknownItem`
- `PackConfidence`

## データフロー

1. `scan` 結果を入力
2. `resolvedEntry` を取得
3. `SymbolAggregateResolver` が relevant symbol を統合
4. `GoalDrivenExpansionPlanner` が goal から優先探索軸を作る
5. `RelevantPackContextFactory` が既存 relevant context に因果鎖候補を追加
6. 各 section builder が contracts / index / snippets / files / unknowns を追加
7. renderer が最終 Markdown に整形

## 依存関係

- section builder は scan IR と `RelevantPackContext` に依存してよい
- section builder 同士は依存しない
- partial 統合ロジックは scan または extraction context に閉じ込める
- goal 解析は renderer に持ち込まない

## 設計原則

- 検索量ではなく高シグナル優先度で勝つ
- 1 つの section が巨大な責務を持たない
- 出力できないものは `unknowns` として残す
- budget 制御は最後ではなく candidate 生成時点でも意識する
