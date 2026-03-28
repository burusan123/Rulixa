# 実装計画

## 方針

Phase 1 は、文書ごとに機能を作るのではなく、**内側から外側へ**実装する。

順序:

1. Domain
2. Application
3. Plugin / Infrastructure
4. Frontend

## 1. プロジェクト作成順

### Step 1. `Rulixa.Domain`

最初に作るもの:

- `Entry`
- `ResolvedEntry`
- `Budget`
- `ContextPack`
- `Contract`
- `Index`
- Pack 選定優先順位

完了条件:

- Domain が外部技術に依存していない

### Step 2. `Rulixa.Application`

次に作るもの:

- `ScanWorkspaceUseCase`
- `ResolveEntryUseCase`
- `BuildContextPackUseCase`
- `IWorkspaceScanner`
- `IEntryResolver`
- `IContractExtractor`
- `IContextPackRenderer`

完了条件:

- UseCase がポートだけに依存して動く

### Step 3. `Rulixa.Plugin.WpfNet8`

実装するもの:

- solution / csproj 検出
- XAML 読み取り
- View-ViewModel 対応抽出
- Command 抽出
- Dialog 起動抽出
- DI 登録抽出

完了条件:

- `AssessMeister` の `ShellView.xaml` を入口に IR を出せる

### Step 4. `Rulixa.Infrastructure`

実装するもの:

- ファイルシステム
- ハッシュ
- JSON 保存
- キャッシュ
- ログ

完了条件:

- 同一入力で同一 IR / Pack を再現できる

### Step 5. `Rulixa.Cli`

実装するもの:

- `scan`
- `resolve-entry`
- `pack`

完了条件:

- CLI から `AssessMeister` に対して Pack を生成できる

## 2. 最初のユースケース

最初に通すべきユースケースは 1 つに絞る。

```text
entry=file:src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml
goal=Shell 画面に新しいページを追加したい
```

このユースケースで最低限出したいもの:

- Root 起動経路
- `ShellView` と `ShellViewModel`
- ページ切り替え契約
- DI 登録
- 主要コマンド

## 3. scan pipeline

Phase 1 の `scan` は次の順で構成する。

1. ワークスペース走査
2. solution / csproj 発見
3. WPF 対象ファイル抽出
4. シンボル一覧抽出
5. View-ViewModel 契約抽出
6. Command 契約抽出
7. Dialog 起動契約抽出
8. DI 契約抽出
9. IR 正規化と並び替え

## 4. テスト方針

### Domain

- 不変条件のユニットテスト
- Pack 優先順位テスト

### Application

- UseCase 単位のテスト
- `entry` 解決テスト

### Plugin / Infrastructure

- `AssessMeister` の固定ファイルを使ったゴールデンテスト
- `ShellView.xaml` の Pack 例との一致確認

### Frontend

- 引数解決
- 出力フォーマット

## 5. Phase 1 完了条件

- `AssessMeister` に対して CLI から Pack を安定生成できる
- `file` と `symbol` の両入口が機能する
- XAML / ViewModel / Dialog 起動サービスの主要導線を Pack に含められる
- 出力が決定的である

## 6. 追加文書を増やさないルール

残りの文書スロットは少ないため、以後は新規作成より追記を優先する。

- 実装順はこの文書に追記
- scan 詳細は `architecture.md` またはこの文書に追記
- 例は `examples/` 配下に増やすが、必要最小限にする
