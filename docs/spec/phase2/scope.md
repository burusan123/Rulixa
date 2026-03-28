# Phase 2 スコープ

## 目的

Phase 2 の目的は、`pack` を「入口の説明」から「システム理解のための最短マップ」へ強化することにある。
対象は、少ないトークンで LLM が次の推論を進められることを重視した設計改善である。

## 解決したい問題

Phase 1 の `pack` は次の問題を残している。

- 入口から 1 hop 先までは強いが、実ユースケースや永続化境界までの流れが薄い
- partial class に分割された型の代表ファイルだけを見て全体を誤認しやすい
- goal が「システム理解」でも UI 寄り section に偏りやすい
- 深掘り不足でも `unknowns` が空になりやすい
- 「次にどこを掘るべきか」が Pack から分かりにくい

## Phase 2 に含めるもの

- `goal` に応じて関連ノードを 2 hop 前後で展開する仕組み
- partial 宣言を 1 シンボルとして束ねる仕組み
- 新しい高シグナル section builder
  - Workflow
  - Persistence
  - ExternalAsset
  - ArchitectureTest
  - HubObject
- `unknowns` と `confidence` の再定義
- `pack` 出力の優先度付け改善
- `AssessMeister` を使った smoke / fixture / acceptance の更新

## Phase 2 に含めないもの

- 汎用コード検索エンジン化
- 無制限再帰の call graph 展開
- Roslyn ベースの完全な interprocedural analysis
- LLM 自体による要約生成の埋め込み
- WPF 以外の新規 plugin 対応
- 既存 `scan` IR の全面刷新

## 成功条件

- `ShellViewModel` の pack で、起動経路だけでなく `ProjectDocument`、Repository、設定、レポート出力まで見通せる
- `DraftingWindowViewModel` の pack で、少なくとも `Workflow -> PortAdapter -> Service -> WallAlgorithm` の主因果鎖が出る
- partial class の入口がどの `.cs` に resolve されても、同じ全体像が得られる
- 未解決箇所があるときは `unknowns` に明示され、次に読むべき候補ファイルが出る
- 追加 section が budget を大きく壊さず、Phase 1 と同等の実行速度感を大きく損なわない

## Phase 2 の価値指標

- `pack` 1 回で得られる高シグナル契約数
- `unknowns` の妥当性
- `selected files` の納得感
- 同じ entry に対する再現性
- 全文検索なしでも次の探索方針が立つ割合
