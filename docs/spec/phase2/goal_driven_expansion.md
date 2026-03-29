# Goal-driven Expansion

## 目的

`goal` をただの表示文ではなく、Pack の展開戦略を決める入力として扱う。
Phase 2 では entry 起点の関連箇所抽出に加え、`goal` に応じて 2 hop 前後の因果鎖を追加で拾う。

## 基本ルール

- hop は原則 2 まで
- 2 hop を超える再帰展開はしない
- 直接辿れない場合は `unknowns` を返す
- 1 hop / 2 hop の関係は contract に明示する

## 展開単位

Phase 2 で扱うノード例:

- View
- ViewModel
- Command
- Method
- Helper method
- Application service / UseCase
- Port / Adapter
- Repository / Query / Saver
- Core state object
- External asset
- Architecture test

エッジ例:

- binds-to
- invokes
- delegates-to
- loads-from
- saves-to
- reads-setting
- uses-template
- guarded-by-test

## goal からの優先度付け

### `system` / `understand` / `explain`

優先:

- HubObject
- Workflow
- Persistence
- ExternalAsset
- ArchitectureTest

### `save` / `open` / `project` / `workspace`

優先:

- Workflow
- Persistence
- HubObject

### `drafting` / `ocr` / `ai` / `analyze`

優先:

- Workflow
- Port / Adapter
- ExternalAsset
- Algorithm boundary

### `architecture` / `layer` / `dependency`

優先:

- ArchitectureTest
- DI
- Workflow

goal に複数語がある場合は加点方式で優先度を決める。
未知の goal では Phase 1 の section に加え、HubObject と Workflow を最低限有効化する。

## 展開アルゴリズム

1. entry から seed symbol / seed file を作る
2. partial 統合後の symbol aggregate を使う
3. goal ごとの優先ノード種別に従って candidate を集める
4. 1 hop で十分な高シグナル候補を優先する
5. 足りない場合のみ 2 hop を追加する
6. budget を超えそうなら signal score の低い候補から落とす

## unknowns

Phase 2 の `unknowns` は次を満たすときに必須で出す。

- 期待した hop が見つからない
- symbol は見つかったが代表的な downstream が特定できない
- 重要候補が複数あり絞り込めない
- 反射、DI factory、命名不一致で追跡が止まる

`unknowns` には少なくとも次を含める。

- 何が未解決か
- どこまで分かったか
- 次に読むべきファイルまたはシンボル候補

## confidence

Pack 全体と契約ごとに confidence を持たせてよい。

高:

- 明示的な binding、registration、constructor injection、直接呼び出し

中:

- 命名規約、1 hop helper、規約ベース対応

低:

- 推定のみ、複数候補あり、partial 未解決、文字列ベース参照

confidence が低い契約は summary 側でも推定であることが分かる表現にする。

## 期待出力

`RealWorkspace` の `DraftingWindowViewModel` なら、少なくとも次のような因果鎖を出せる状態を目指す。

- `AiAnalyzeCommand -> RunAiDiagramAnalysisAsync -> IDraftingWorkflowService`
- `IDraftingAiDiagramAnalysisPort -> DraftingAiDiagramAnalysisPortAdapter -> DraftingAiDiagramAnalysisService`
- `DraftingAiDiagramAnalysisService -> IDraftingWallAlgorithmExecutor -> WallAlgorithmRunner`

この全鎖が出ない場合は `unknowns` に不足区間を残す。
