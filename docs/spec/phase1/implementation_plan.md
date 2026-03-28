# 実装計画

## 方針

Phase 1 は、文脈圧縮の完全自動化を一気に狙うのではなく、まず `WPF + .NET 8` の変更に必要な最小 Context Pack を安定して生成するところまでを到達点とする。  
実装順は **Core を先に固め、WPF 固有解析を後から乗せ、最後に CLI と出力品質を磨く** 形を維持する。

優先順は次のとおり。

1. Domain
2. Application
3. Plugin / Infrastructure
4. Frontend

## 初期実装の段階

### Step 1. `Rulixa.Domain`

最初に固めるもの:

- `Entry`
- `ResolvedEntry`
- `Budget`
- `ContextPack`
- `Contract`
- `Index`
- Pack 選定ルール

完了条件:

- Domain が外部技術に依存せず、Pack 選定を単体テストで保証できること

### Step 2. `Rulixa.Application`

次に固めるもの:

- `ScanWorkspaceUseCase`
- `ResolveEntryUseCase`
- `BuildContextPackUseCase`
- `IWorkspaceScanner`
- `IEntryResolver`
- `IContractExtractor`
- `IContextPackRenderer`

完了条件:

- UseCase がポート経由で動作し、WPF 固有処理を直接持たないこと

### Step 3. `Rulixa.Plugin.WpfNet8`

実装するもの:

- solution / csproj 読み取り
- XAML 読み取り
- View-ViewModel 対応抽出
- Command 抽出
- Dialog 起動抽出
- DI 登録抽出

完了条件:

- `AssessMeister` の `ShellView.xaml` / `ShellViewModel` を起点に IR を返せること

### Step 4. `Rulixa.Infrastructure`

実装するもの:

- ファイルシステム
- ハッシュ
- JSON 出力
- キャッシュ
- ログ

完了条件:

- 同一入力で同一 IR / Pack を返せること

### Step 5. `Rulixa.Cli`

実装するもの:

- `scan`
- `resolve-entry`
- `pack`

完了条件:

- CLI から `AssessMeister` に対して Pack を生成できること

## 最初のユースケース

最初に通すユースケースは 1 つに絞る。

```text
entry=file:src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml
goal=Shell 画面に新しいページを追加したい
```

このユースケースで最低限含めるもの:

- Root 起動経路
- `ShellView` と `ShellViewModel`
- ページ切り替え導線
- DI 登録
- 主要コマンド

## scan pipeline

Phase 1 の `scan` は次の順で事実を抽出する。

1. ワークスペース列挙
2. solution / csproj 特定
3. WPF 対象ファイル抽出
4. シンボル抽出
5. View-ViewModel 抽出
6. Command 抽出
7. Dialog 起動抽出
8. DI 抽出
9. IR 正規化と並び替え

## テスト戦略

### Domain

- 不変条件のユニットテスト
- Pack 選定ルールのテスト

### Application

- UseCase 単位のテスト
- `entry` 解決のテスト

### Plugin / Infrastructure

- `AssessMeister` 風 fixture を使ったゴールデンテスト
- `ShellView.xaml` の Pack 例での受け入れ確認

### Frontend

- 引数解決
- 出力フォーマット

## Phase 1 完了条件

- `AssessMeister` に対して CLI から Pack を安定生成できる
- `file` と `symbol` の両入口が動作する
- XAML / ViewModel / Dialog 起動サービスの主要導線を Pack に含められる
- 同じ入力に対して同じ出力を返せる

## 改善バックログ

### P1

- `CurrentPage` の更新起点を Pack に出す  
  `SelectedItem / CurrentPage` の binding だけでなく、`CurrentPage = item.PageViewModel` のような実際の更新地点まで Pack に出す。
- `SelectedItem` と `CurrentPage` の因果関係を契約化する  
  単なる binding 一覧ではなく、「選択変更が表示切替を駆動する」というナビゲーション規約として表現する。
- `DataContext` の由来を root / view / code-behind で明示的に分ける  
  XAML 側、code-behind 側、コンストラクタ注入経由を区別して表示し、誤読を減らす。

### P2

- DI 登録のライフタイムを Pack に出す  
  `Singleton / Scoped / Transient / Factory` の差分を契約に含める。
- コマンドの影響対象を出す  
  `Command -> ExecuteSymbol` だけでなく、実行メソッドが触る主要状態や関連サービスも Pack に含める。
- 選定ファイルの理由をもう一段具体化する  
  巨大ファイルには「どの観点で必要か」を補足し、全文を読む必要があるのかを判断しやすくする。

### P3

- 巨大 ViewModel のスニペット抽出  
  `ShellViewModel.cs` 全文ではなく、`SelectedItem`、`CurrentPage`、`CreatePageViewModel` 周辺だけを抜けるようにする。
- 実コードの行番号を Pack に出す  
  直接確認への接続を速くする。
- `DataTemplate` 群を「省略したが存在する」と明示する  
  省略判断をユーザーに伝える。

## 改善バックログの成功条件

- Pack だけ見て「次に開くべき行」が分かる
- 直接確認が必要な範囲を 3〜5 箇所まで縮められる
- `ShellViewModel` のような巨大ファイルでも、全文読まずに修正導線へ入れる
