# High-signal Sections

## 方針

Phase 2 では UI 周辺 section に加えて、システム理解に直結する section を追加する。
ここでの目的は「ファイル数を増やす」ことではなく、「少ないトークンで地図として価値が高い契約を増やす」ことにある。

## 追加 section

### 1. Workflow

対象:

- Command 実行先
- helper 1 hop
- Application service / UseCase
- Port / Adapter

出力:

- `A -> B -> C` の短い因果鎖
- 実行入口と副作用境界
- 主要 snippet

### 2. Persistence

対象:

- Repository
- Query / Saver
- `ProjectDocument` のような中心状態
- ファイル形式やエントリ名

出力:

- 読み込み元、保存先、主要 entry / DTO
- 「どこで状態が持たれるか」の契約
- 永続化境界を示す snippet

### 3. ExternalAsset

対象:

- Excel master
- report template
- ONNX model
- JSON config
- PDF template / image asset

出力:

- どの service がどの資産を読むか
- fallback ルール
- 実行時に必要な外部ファイル候補

### 4. ArchitectureTest

対象:

- layer guard
- golden test
- regression test

出力:

- システムが何を壊したくないか
- 依存方向の明示
- 主要テストファイル

### 5. HubObject

対象:

- システムの中心状態
- 複数ユースケースから共有されるオブジェクト

例:

- `ProjectDocument`
- `SettingsDocument`
- `DraftingState`

出力:

- オブジェクトの責務
- 誰が更新し、誰が読むか
- dirty state、snapshot、identity の有無

## section 優先度

`goal` がシステム理解寄りなら、Phase 1 の `Dialog` より `HubObject` と `Persistence` を優先してよい。
`goal` が UI 操作説明寄りなら既存 section を優先する。

## file/snippet 選定ルール

- Workflow は chain の各 hop 全部ではなく、入口・中継・境界の 3 点を優先
- Persistence は Query/Saver の対を優先
- ExternalAsset は資産そのものではなく、資産を解決するコードを優先
- ArchitectureTest は最も拘束力の強いテストを優先
- HubObject は定義と代表的更新点を優先

## RealWorkspace での期待例

- Workflow
  `ShellViewModel -> ProjectWorkspaceFlowService -> ProjectWorkspaceService`
- Persistence
  `ProjectDocument <- AsmProjectRepository`
- ExternalAsset
  `ExcelSettingsQuery -> ProductSetting_R*.xlsx`
  `ReportExportService -> ProductReport_*.xlsx`
  `DraftingAiDiagramAnalysisService -> ONNX model`
- ArchitectureTest
  `LayerGuardTests`
- HubObject
  `ProjectDocument`
