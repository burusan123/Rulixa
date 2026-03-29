# Entry 解決仕様

## 位置づけ

この文書は Phase 1 の `entry` 解決仕様です。
Rulixa 全体の一般仕様ではなく、`WPF + .NET 8` を具体攻略対象にしたときの優先解決ルールを定義します。

## 対応する入口

- `file:<path>`
- `symbol:<qualifiedName>`
- `auto:<text>`

## `file` 解決

対象:

- `*.xaml`
- `*.xaml.cs`
- `*.cs`
- `*.csproj`
- `*.sln`
- `Directory.Build.props`

ルール:

1. ワークスペース相対パスと絶対パスの両方を受ける
2. Windows では大文字小文字を区別しない
3. 同名ファイルが複数ある場合は候補を返す
4. 一意に定まらない場合は `unresolved` にする

## `symbol` 解決

対象:

- クラス
- メソッド
- プロパティ
- `ICommand` 公開プロパティ

ルール:

1. 完全修飾名を優先する
2. 同名クラスが複数ある場合は候補を返す
3. `partial class` は 1 つのシンボルとして扱う

## `auto` 解決

`auto` は補助入口です。Phase 1 では次の順で解決を試みます。

1. ファイル名一致
2. クラス名一致
3. View と ViewModel の命名規約一致
4. Window 名一致

複数候補が出る場合は、自動確定せず候補を返します。

## Phase 1 の `WPF + .NET 8` 優先規則

### XAML 起点

補完対象:

- 対応する code-behind
- 対応する ViewModel
- 対応する DataTemplate

### ViewModel 起点

補完対象:

- 対応 View
- 注入サービス
- `ICommand`
- `SelectedItem` / `CurrentPage` 更新点

### Dialog 起点

補完対象:

- 起動先 Window
- 起動先 ViewModel
- 呼び出し元サービス
