# Context Pack ルール

## 目的
Phase 1 の `Context Pack` は、WPF + .NET 8 の変更開始に必要な最小文脈を AI に渡すための出力です。  
`entry=file` と `entry=symbol` の両方で、因果と根拠を短く強く読めることを優先します。

## Pack の基本構成

1. `goal`
2. `entry`
3. `resolved entry`
4. `contracts`
5. `impact / index`
6. `selected snippets`
7. `selected files`
8. `unknowns`

## contracts

Phase 1 では次を扱います。

- 起動経路
- DI 登録
- View と ViewModel の対応
- ナビゲーション
- コマンド
- ダイアログ起動

原則:

- 変更開始に効く因果を先に出す
- `file entry` の `DataTemplate` は個別列挙せず要約する
- DI は関連登録だけを短く要約する

## impact / index

Index は Pack の速読用です。代表的な節は次です。

- `View-ViewModel`
- `ナビゲーション`
- `選択から表示への因果`
- `ナビゲーション更新点`
- `起動経路`
- `DI`
- `コマンド`

## selected snippets

`selected snippets` は巨大な `*.cs` を全文ではなく根拠付き snippet で読むための節です。

保持項目:

- `path`
- `reason`
- `priority`
- `isRequired`
- `anchor`
- `startLine`
- `endLine`
- `content`

### snippet 選定規則

- `*.cs` かつ `LineCount > 250` のファイルで snippet が 1 件以上採用された場合、その全文は `selected files` から外す
- 同一ファイル内の snippet は `maxSnippetsPerFile` 件までに制限する
- 重なり、または 3 行以内に接する snippet は 1 件にマージする
- 予算計算は全文行数ではなく採用 snippet の行数合計を使う

### Phase 1 で snippet 化する対象

- `ShellViewModel(...)` などの constructor
- `Select(...)` / `RestoreSelection(...)` などの navigation update
- root binding の code-behind 行
- `ServiceRegistration.cs` の関連 DI 登録行
- 個別契約として残る command execute / dialog activation method

### snippet の読解順

優先度はおおむね次の順です。

1. root binding
2. DI registration
3. DI constructor
4. navigation update
5. command / dialog method

これにより `MainWindow.xaml.cs -> ServiceRegistration.cs -> ShellViewModel.cs` の順で読めます。

## selected files

`selected files` は全文で読むべき小さめのファイル一覧です。主に次を残します。

- entry XAML / code-behind
- 起動経路
- `ServiceRegistration.cs`
- root view を支える小さな関連ファイル
- command support の補助クラス

## budget 優先順

予算超過時は次の順で削ります。

1. `DataTemplate` 由来の二次文脈
2. 任意の補助サービス
3. 大きいファイルの全文

次は維持対象です。

- entry
- 主要 ViewModel の因果
- root binding
- 起動経路
- 関連 DI 登録
- `SelectedItem` / `CurrentPage` の更新点
