# Phase 2 実装計画

## 実装方針

Phase 2 は一括実装ではなく、Pack の品質を壊さないように段階導入する。
既存 `scan -> resolve-entry -> pack` の CLI 契約は維持し、内部改善として進める。

## ステップ

### Step 1. partial 統合

- partial symbol の集約処理を追加
- `pack` の relevant symbol 算出を partial aware に変更
- 既存 WPF pack の差分テストを追加

完了条件:

- partial class の代表ファイルが変わっても、主要 contracts が大きくぶれない

### Step 2. goal-driven expansion の基盤

- `goal` キーワードから優先軸を決める planner を追加
- 1 hop / 2 hop の candidate 抽出を追加
- `unknowns` と `confidence` モデルを導入

完了条件:

- `AssessMeister` の `ShellViewModel` pack で HubObject / Persistence 候補が拾える

### Step 3. high-signal sections

- Workflow
- Persistence
- ExternalAsset
- ArchitectureTest
- HubObject

の順で section builder を追加する。

完了条件:

- `ShellViewModel` pack に `ProjectDocument`、Repository、設定、レポート出力が入る
- `DraftingWindowViewModel` pack に `Workflow -> PortAdapter -> Service -> WallAlgorithm` の主鎖が入る

### Step 4. renderer / budget 調整

- section の優先度制御
- file / snippet candidate score の見直し
- `unknowns` と `confidence` の表示改善

完了条件:

- file 数と snippet 数が増えすぎず、Pack が読みやすい

## テスト計画

### fixture / unit

- partial symbol 集約
- goal planner
- 2 hop expansion
- section builder ごとの抽出結果
- unknowns 生成条件

### smoke

対象ワークスペース:

- `D:\C#\AssessMeister`

確認 entry:

- `symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel`
- `symbol:AssessMeister.Presentation.Wpf.ViewModels.Drafting.DraftingWindowViewModel`

確認観点:

- partial 統合
- workflow chain
- persistence chain
- external asset chain
- architecture test chain
- unknowns の妥当性

## 受け入れ条件

- `Rulixa` が全文検索の代わりになる必要はない
- ただし「最初に読む地図」として全文検索より有利な場面を明確に増やす
- `AssessMeister` で、入口だけでなく実ユースケースの輪郭まで 1 回の pack で掴める

## backlog

### P1

- generic method / lambda 越し helper の追跡改善
- score 設計の調整
- `unknowns` の文言改善

### P2

- `pack --mode map|drilldown` の追加検討
- section ごとの evidence export 強化

### P3

- WPF 以外の plugin への横展開
