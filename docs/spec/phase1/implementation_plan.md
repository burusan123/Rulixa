# 実装計画

## 現在の到達点
Phase 1 の現在実装は、`WPF + .NET 8` ワークスペースに対して次を行えます。

- `scan` で WPF 固有の事実を抽出する
- `resolve-entry` で `file` / `symbol` を具体解決する
- `pack` で契約・index・selected files・selected snippets を組み立てる

## 現在の構成

### Domain

- `Entry`
- `ResolvedEntry`
- `Budget`
- `ContextPack`
- `Contract`
- `IndexSection`
- `SelectedFile`
- `SelectedSnippet`

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

`WpfNet8ContractExtractor` と `WpfNet8WorkspaceScanner` は orchestration に寄せ、抽出責務は section builder に分離しています。

## 実装済み

### Pack 圧縮

- `file entry` の `DataTemplate` は個別列挙せず、`DataTemplate 二次文脈` として要約する
- `SelectedItem -> CurrentPage` の因果を契約と index の両方で短く表現する
- コマンド導線が多い場合は要約契約に圧縮する

### DI 表示

- 主要 ViewModel の DI 登録を明示する
- 直接依存の lifetime を `Singleton / Scoped / Transient / Factory` 単位で要約する
- index に `DI` セクションを持つ

### 巨大 C# ファイルのスニペット化

- `ContextPack` に `SelectedSnippets` を追加した
- `maxSnippetsPerFile` を実際に適用する
- `*.cs` かつ `LineCount > 250` のファイルでは、constructor / navigation update / command execute / dialog activation の根拠を snippet 化する
- snippet が採用された巨大 `*.cs` は `SelectedFiles` から除外する
- `MarkdownContextPackRenderer` は `## 重要スニペット` を描画する

## テスト状況

- Domain
  - budget での全文選定
  - snippet 置換
  - snippet merge
  - snippet 上限
- Application
  - `BuildContextPackUseCase`
  - `MarkdownContextPackRenderer`
- Plugin
  - fixture scan
  - `file entry` pack
  - `symbol entry` pack
  - command summary
  - generated / temp file の除外

## 次の候補

### P1

- `ViewModelBinding` に行番号を持たせ、root binding も snippet 化できるようにする
- `ServiceRegistration` に行番号を持たせ、DI 登録ファイルも line-range で指せるようにする
- snippet merge 後の `anchor` と `reason` の表現をさらに短く整える

### P2

- コマンドの影響対象を 1 段深く出す
- 選定ファイル理由の粒度をさらに上げる
- dialog 起動の owner / activation kind を pack 本文に反映する

### P3

- XAML / code-behind の snippet 化
- 行番号の利用強化
- snippet 優先度の goal 連動
