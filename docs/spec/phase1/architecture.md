# アーキテクチャ

## 目的

Phase 1 のアーキテクチャは、**フロントエンドとコアを明確に分離**しつつ、`WPF + .NET 8` 向けの解析を最小コストで拡張可能にすることを目的とする。

ここでいう分離は次を意味する。

- UI/入口は Pack を「表示・要求」するだけ
- 解析、契約抽出、entry 解決、Pack 組み立ては Core が担う
- `WPF + .NET 8` 固有の解析はプラグインとして外側に置く

## 依存方向

依存方向は常に外側から内側に向ける。

1. Frontend
2. Plugin / Infrastructure
3. Application
4. Domain

禁止事項:

- Frontend から Domain を直接読んでロジックを組むこと
- Plugin が Frontend 型に依存すること
- Domain が Roslyn、WPF、CLI、MCP に依存すること

## レイヤ責務

### Domain

役割:

- `Entry`, `ResolvedEntry`, `Budget`, `ContextPack`, `Contract`, `Index` などの中核概念
- 不変条件
- 決定性を守るための並び順ルールや優先順位ルール

置くべきでないもの:

- Roslyn
- ファイル I/O
- JSON シリアライズ
- CLI/MCP 用 DTO

### Application

役割:

- `scan`
- `resolve entry`
- `extract contracts`
- `build context pack`

特徴:

- ユースケース単位で組み立てる
- 外部 I/O はインターフェース越しに使う
- 対象技術固有の実装詳細は持たない

### Plugin / Infrastructure

役割:

- ファイルシステム走査
- ハッシュ計算
- JSON 出力
- `WPF + .NET 8` 向け解析
- Roslyn / XML / csproj 読み取り

特徴:

- Application のポートを実装する
- 対象技術ごとの差分を吸収する

### Frontend

役割:

- ユーザー入力受付
- `entry`, `goal`, `budget` の受け渡し
- Pack 表示
- 候補解決 UI

Phase 1 では次を想定する。

- CLI
- MCP
- 将来の VS Code 拡張

## 推奨プロジェクト分割

Phase 1 での分割は、細かくし過ぎず次の粒度を推奨する。

### 1. `Rulixa.Domain`

含むもの:

- 中核モデル
- Pack 組み立てルール
- 契約・索引の値オブジェクト

### 2. `Rulixa.Application`

含むもの:

- `ScanWorkspaceUseCase`
- `ResolveEntryUseCase`
- `BuildContextPackUseCase`
- Application ポート

### 3. `Rulixa.Plugin.WpfNet8`

含むもの:

- XAML 解析
- View-ViewModel 対応抽出
- Command 抽出
- Dialog 起動抽出
- DI 登録抽出

理由:

- `WPF + .NET 8` 固有ロジックを Core から隔離するため

### 4. `Rulixa.Infrastructure`

含むもの:

- ファイルシステム
- キャッシュ
- ハッシュ
- JSON シリアライザ
- ログ実装

### 5. `Rulixa.Cli`

含むもの:

- コマンドライン入口
- 引数パース
- Pack 出力

### 6. `Rulixa.Mcp`

含むもの:

- MCP 入口
- リクエスト/レスポンス変換

## 分け過ぎないための基準

次のような目的だけで新規プロジェクトを作らない。

- Utility を分けたいだけ
- Model を別けたいだけ
- 将来使うかもしれないから

新規プロジェクト化の条件は、少なくとも次のどちらかを満たすときとする。

- 依存方向を明確に守るために物理分割が必要
- 変更理由が明確に異なる

## Phase 1 の処理フロー

1. Frontend が `entry + goal + budget` を受け取る
2. Application が workspace scan を要求する
3. Plugin が `WPF + .NET 8` 向け IR を構築する
4. Application が `entry` を解決する
5. Application が Pack 選定ルールで最小ファイル束を作る
6. Frontend が Pack を表示する

## `AssessMeister` を前提にした設計上の示唆

`AssessMeister` では次が主要論点だった。

- `App -> MainWindow -> ShellViewModel` の起動経路
- `ShellView.xaml` の `DataTemplate`
- `ShellViewModel` の `CurrentPage` 切り替え
- `ServiceRegistration.cs` の DI
- `ShowDialog()` 系サービス

したがって `Rulixa.Plugin.WpfNet8` は、まずこの5系統の抽出に集中する。

## Phase 1 の非目標

- 入口ごとに別 Core を作ること
- WPF 解析を Domain/Application に埋め込むこと
- 技術要素ごとに過剰に細かいプロジェクトへ分解すること
