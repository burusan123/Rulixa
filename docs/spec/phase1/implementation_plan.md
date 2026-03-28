# 実装計画

## 現在の到達点

Phase 1 の実装は `WPF + .NET 8` ワークスペースを対象に、`scan -> resolve-entry -> pack` を通せる状態です。

実装済み:

- `scan` で WPF 固有事実を抽出する
- `resolve-entry` で `file` / `symbol` を解決する
- `pack` で contracts / index / selected files / selected snippets を組み立てる
- `publish/*` と `*_wpftmp.csproj` を走査除外する
- `file entry` の `DataTemplate` を要約表示する
- `SelectedItem -> CurrentPage` の因果を要約契約として出す
- コマンド導線を件数ベースで要約する
- 主要 ViewModel と直接依存の DI lifetime を要約する
- 巨大 `*.cs` を `SelectedSnippets` へ置き換える
- `ViewModelBinding` / `NavigationTransition` / `ServiceRegistration` に `SourceSpan` を持たせる
- `MainWindow.xaml.cs` の root binding と `ServiceRegistration.cs` の登録行も snippet 化する
- `ShellView.xaml` など entry XAML の binding 根拠を `selected snippets` に line-range で含める

## 現在の構成

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

`WpfNet8ContractExtractor` と `WpfNet8WorkspaceScanner` は orchestration に限定し、WPF 固有知識は builder に分割しています。

## テスト観点

### Domain

- budget 下での file/snippet 選定
- snippet merge
- snippet 上限
- `SourceSpan` の不変条件

### Application

- `BuildContextPackUseCase`
- `MarkdownContextPackRenderer`

### Plugin

- fixture scan
- `file entry` pack
- `symbol entry` pack
- command summary
- DI summary
- root binding / registration snippet
- XAML navigation snippet
- generated / temp file 除外

## 次の候補

### P1

- snippet reason の粒度整理

完了:

- `ServiceRegistration` の複数行登録への span 拡張

### P2

- command 影響先の 1 段深掘り
- selected file reason の詳細化
- dialog activation の owner / activation kind 強化

### P3

- goal に応じた snippet 優先度の調整
- 追加 plugin への抽出規則展開
