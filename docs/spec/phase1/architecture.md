# Phase 1 アーキテクチャ

## 位置づけ

この文書は Phase 1 の具体アーキテクチャ仕様です。
Rulixa 全体の正本ではなく、`WPF + .NET 8` を攻略対象にしたときのプロジェクト分割と責務分離を定義します。

## 基本方針

- Frontend と Core を分離する
- 入口の都合を Domain / Application に持ち込まない
- `WPF + .NET 8` 固有解析は Plugin に閉じ込める
- Infrastructure にドメインルールを置かない

## 層構造

1. Frontend
2. Plugin / Infrastructure
3. Application
4. Domain

依存方向は外側から内側だけに向けます。

## プロジェクト分割

### `Rulixa.Domain`

- `Entry`
- `ResolvedEntry`
- `Budget`
- `ContextPack`
- `Contract`
- `IndexSection`
- `NavigationTransition`
- Pack 選定ルール

禁止:

- Roslyn 依存
- WPF 依存
- ファイル I/O
- CLI 都合の型

### `Rulixa.Application`

- `ScanWorkspaceUseCase`
- `ResolveEntryUseCase`
- `BuildContextPackUseCase`
- ポート定義

役割:

- Domain を使ってユースケースを組み立てる
- 外部 I/O は抽象に依存する

### `Rulixa.Plugin.WpfNet8`

- XAML 解析
- View-ViewModel 抽出
- NavigationTransition 抽出
- Command 抽出
- Dialog 抽出
- DI 抽出
- Pack 契約の組み立て
- Scan IR の組み立て

役割:

- Phase 1 の具体攻略対象を Core から分離する

内部構成:

- `Extraction/Context`
  Pack 生成時に使う関連 ViewModel 群や規約ベース解決をまとめる
- `Extraction/Sections`
  `DI`、`Navigation`、`ViewBinding`、`Command`、`Dialog` ごとの Pack セクションを組み立てる
- `Scanning/Context`
  scan 実行時のファイル内容と `ScanFile` 一覧をまとめる
- `Scanning/Sections`
  ワークスペース列挙、Symbol 抽出、ProjectSummary 組み立てを責務ごとに分離する

設計意図:

- `WpfNet8ContractExtractor` と `WpfNet8WorkspaceScanner` は調停役に留める
- WPF 固有の抽出知識は section builder / context builder に閉じ込める
- 巨大クラス化を避け、機能追加時の変更理由ごとにファイルを分ける

### `Rulixa.Infrastructure`

- ファイルシステム
- ハッシュ
- Markdown renderer
- entry 解決補助

役割:

- Application のポートを実装する

### `Rulixa.Cli`

- `scan`
- `resolve-entry`
- `pack`

役割:

- ユーザー入力を受けて UseCase を呼ぶ
- 出力を整形する

## 判断基準

迷ったら次で判断します。

- 製品全体で再利用されるルールは Core
- `WPF + .NET 8` に閉じる事実抽出は Plugin
- 入出力、表示、実行環境依存は Frontend / Infrastructure
- Plugin 内でも、scan と pack で変更理由が違うものは別フォルダに分ける
