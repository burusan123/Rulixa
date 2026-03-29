# 実装計画

## 現在の到達点

Phase 1 は `WPF + .NET 8` ワークスペースを対象に、`scan -> resolve-entry -> pack` を一通り通せる状態です。現在の主な実装済み項目は次です。

- `scan` で WPF 固有の事実を抽出する
- `resolve-entry` で `file` / `symbol` / `auto` を解決する
- `pack` で contracts / index / selected files / selected snippets / unknowns を生成する
- `pack --evidence-dir` で `manifest.json`、`scan.json`、`resolved-entry.json`、`pack.md` を bundle として保存する
- `compare-evidence` で bundle 間の metadata / contracts / selected files / selected snippets 差分を描画する
- command 詳細化に対する `goal` 根拠を `decisionTraces` として manifest に保存し、差分比較に含める
- `publish/*` と `*_wpftmp.csproj` を scan から除外する
- `file entry` では `DataTemplate` を要約契約として扱う
- `SelectedItem -> CurrentPage` の navigation 導線を契約と index に出す
- root binding、DI registration、navigation update を snippet として選定する
- `auto` entry では `root-data-context` / `view-data-context` を優先し、`data-template` 由来候補を混ぜすぎない
- dialog activation は caller / target window / activation kind / owner kind を invocation 単位で抽出する
- command は少数件なら全件詳細化し、多数件なら summary を維持しつつ `goal` に近い command だけを詳細化する
- command の詳細化では `execute -> direct service/dialog` だけでなく、同一 ViewModel 内の `private helper 1 hop` を `execute -> helper -> service/dialog` として出す
- 大きい `*.cs` は snippet 中心で Pack に含める

## 責務分割

### Domain

- `Entry`
- `ResolvedEntry`
- `Budget`
- `ContextPack`
- `SelectedSnippet`
- `SourceSpan`

### Application

- `ScanWorkspaceUseCase`
- `ResolveEntryUseCase`
- `BuildContextPackUseCase`
- `IWorkspaceScanner`
- `IEntryResolver`
- `IContractExtractor`
- `IContextPackRenderer`

### Plugin / Infrastructure

- `Extraction/Context`
- `Extraction/Sections`
- `Extraction/Snippets`
- `Scanning/Context`
- `Scanning/Sections`

`WpfNet8ContractExtractor` と `WpfNet8WorkspaceScanner` は orchestration に留め、WPF 固有の抽出は section / builder に閉じ込める方針です。

## テスト状況

### Domain

- budget 配下での file/snippet 選定
- snippet merge
- snippet 優先順
- `SourceSpan` の整列

### Application

- `BuildContextPackUseCase`
- `MarkdownContextPackRenderer`
- evidence bundle writer / reader / diff renderer

### Plugin

- fixture scan
- `file entry` pack
- `symbol entry` pack
- `auto` entry 解決
- command summary と goal 連動の詳細化
- command helper 1 hop 追跡
- direct service / dialog / non-dialog の command 影響先
- root binding / registration snippet
- XAML navigation snippet
- generated / temp file 除外
- dialog activation の `show` / `show-dialog` 判定
- Pack の決定性
- evidence bundle の再利用と revision 退避
- evidence bundle 差分の決定的表示
- command 詳細化に対する `goal` 根拠の保存と比較

## Phase 1 の出口条件

- `plugins/rulixa/.codex-plugin/plugin.json` が JSON として正常に読める
- fixture ベースの `scan` / `resolve-entry` / `pack` が安定して通る
- 同一入力の `pack --evidence-dir` が同一 bundle 名に収まり、既存 evidence を上書き破壊しない
- `compare-evidence` が metadata / contracts / selected files / selected snippets の差分を安定表示する
- `goal` を変えたときに command 詳細化根拠の差分が manifest / compare-evidence で追える
- `auto` entry で二次文脈候補が先頭に混ざりすぎない
- dialog 契約が caller / target window / activation kind を安定して含む
- command 契約が実用上必要な範囲で `execute -> service/dialog` または `execute -> helper -> service/dialog` を示せる
- `dotnet test Rulixa.sln` が常時グリーン

## 任意スモーク検証

- `<modern-real-workspace>` を参考ワークスペースとして使う
- 既定ではスキップし、`RULIXA_RUN_ASSESSMEISTER_SMOKE=1` のときだけ実行する
- 対象 entry は `symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel`
- 確認対象は次に限定する
  - resolved entry
  - Shell 導線の主要 contracts
  - `SelectedItem -> CurrentPage`
  - DI 要約
  - command helper 経路を含む詳細契約が少なくとも 1 件あること

## CI 設計メモ

この段階では CI 自体は未実装です。次段で自動化する対象は次です。

- `dotnet restore`
- `dotnet build`
- `dotnet test Rulixa.sln`
- plugin manifest 妥当性確認

外部ワークスペース依存の optional smoke は CI 対象外とします。

## 次の backlog

### P1

- Pack 文面と renderer に残っている文字化けの整理
- `goal` による command 詳細化の根拠表示
- compare-evidence の差分粒度フィルタ

### P2

- selected file reason / snippet reason の粒度整理
- command helper 経路の説明文の磨き込み
- dialog activation の owner / activation kind の表現統一

### P3

- `goal` に応じた snippet 優先度の細かい調整
- 配布 plugin 向けの導入説明と運用導線の整備
