# Context Pack ルール

## 目的
Phase 1 の `Context Pack` は、WPF + .NET 8 の変更開始に必要な最小文脈を AI に渡すための出力です。  
対象は `entry=file` と `entry=symbol` で、巨大コードベースを丸ごと読ませるのではなく、変更に効く契約・因果・関連ファイルだけを短く強く渡します。

## Pack の基本構造

1. `goal`
2. `entry`
3. `resolved entry`
4. `contracts`
5. `impact / index`
6. `selected snippets`
7. `selected files`
8. `unknowns`

## contracts
Phase 1 の契約は次を含みます。

- 起動経路
- DI 登録
- View と ViewModel の対応
- ナビゲーション
- コマンド
- ダイアログ起動

契約は「何がどこに繋がっているか」を 1 行で説明する要約であり、詳細実装の全文列挙ではありません。

## impact / index
Index は契約を読む前に把握したい因果を短く並べます。

- View-ViewModel 対応
- ナビゲーション
- 選択から表示への因果
- ナビゲーション更新点
- 起動経路
- DI
- コマンド

## selected files
`selected files` は全文で読むべき小さめのファイル一覧です。主に次を残します。

- entry XAML / code-behind
- 起動経路
- DI 登録ファイル
- ルート binding の設定元
- DelegateCommand などの小さい補助実装

理由は `entry`、`startup`、`dependency-injection`、`root-binding`、`view-binding`、`data-template`、`command-support`、`navigation-update` などのコードで保持します。

## selected snippets
`selected snippets` は巨大な `*.cs` を全文選定の代わりに読むための行範囲付き抜粋です。各要素は次を持ちます。

- `path`
- `reason`
- `priority`
- `isRequired`
- `anchor`
- `startLine`
- `endLine`
- `content`

現在の Phase 1 では、主に次のアンカーを抜粋対象にします。

- `ShellViewModel(...)` のようなコンストラクタ
- `Select(...)` や `RestoreSelection(...)` のようなナビゲーション更新点
- 個別契約として残るコマンド execute メソッド
- 個別契約として残るダイアログ起動メソッド

## 置換ルール

- `*.cs` かつ `LineCount > 250` のファイルで snippet が 1 件以上採用された場合、そのファイルは `selected files` から除外します。
- 同一ファイルの snippet は優先度順に選び、`maxSnippetsPerFile` を上限にします。
- 重なり、または 3 行以内に接する snippet は 1 件にマージします。
- `selected files` の行数 budget には、除外した全文行数ではなく採用 snippet の行数を加算します。

## budget 優先順位
budget を超える場合は、次の順で削りやすいものから落とします。

1. `DataTemplate` 由来の二次文脈
2. 任意の補助サービス
3. 大きいファイルの全文選定

次は原則として残します。

- entry
- 主要 ViewModel の根拠
- 対応 View / code-behind
- 起動経路
- DI 登録
- `SelectedItem` / `CurrentPage` の更新根拠
