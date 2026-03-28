# Context Pack ルール

## 位置づけ

Phase 1 の `Context Pack` は、`WPF + .NET 8` の変更対象に対して AI に渡す最小限の構造化コンテキストです。  
`entry=file` と `entry=symbol` を中心に、必要な構造事実と根拠 snippet を優先してまとめます。

Rulixa 全体の中では、Context Pack は継続生成される成果物群の一部です。  
上位には `Contracts` や `Index / Map` のような設計成果物があり、運用上は PR レビューや監査に使う差分成果物も扱います。Context Pack はその中で、AI に変更開始用の最小束を渡す役割を持ちます。

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

Phase 1 で扱う契約は次です。

- 起動経路
- DI 登録
- View と ViewModel の対応
- navigation
- command
- dialog activation

補足:

- 変更作業に効く事実を優先し、説明だけの情報は増やしすぎない
- `file entry` の `DataTemplate` は要約契約として扱う
- DI は関係する登録だけを要約する

### command 契約

command 契約は `goal` と command 件数に応じて出し方を変える。

- command 数が少ない場合は全件を個別契約として出す
- command 数が多い場合は summary を維持しつつ、`goal` に近い command だけを追加で詳細化する
- 詳細化対象では `execute -> direct service/dialog` だけでなく、同一 ViewModel 内の `private helper 1 hop` を `execute -> helper -> service/dialog` として表現する
- helper は `private` / `internal` / `protected` の instance method のみ対象とし、2 hop 以上は追わない

例:

- `OpenSettingCommand は ShellViewModel.OpenSetting(...) を実行し、ISettingWindowService.OpenSettingWindow(...) を呼び出します。`
- `NewProjectCommand は ShellViewModel.NewProject(...) を実行し、ShellViewModel.LoadPagesFromProjectDocument(...) を経由して ... を呼び出します。`

dialog に接続できる場合は末尾に `最終的に XxxWindow が show-dialog で起動されます。` を付ける。

## impact / index

Index は Pack の確認用で、主要な経路だけを短く表示する。

- `View-ViewModel`
- `Navigation`
- `起動経路`
- `DI`
- `Command`

### command index

- 少数 command: `Command -> Execute -> Service/Dialog`
- helper 経由: `Command -> Execute -> Helper -> Service/Dialog`
- 多数 command: summary 行 + `goal` に関連する詳細行

## selected snippets

`selected snippets` は全文ではなく、判断に必要な断片を優先する。  
大きい `*.cs` は snippet で扱い、必要最小限の範囲に切り出す。

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

- `*.cs` かつ `LineCount > 250` のファイルでは snippet を優先する
- 同一ファイルの snippet は `maxSnippetsPerFile` までに抑える
- 近接 snippet は 1 つに merge する
- 行数よりも変更理由の説明力を優先する

### Phase 1 で優先する snippet

- constructor
- navigation update
- root binding の code-behind
- `ServiceRegistration.cs` の関連 DI 登録
- command execute
- command helper
- dialog service / dialog activation method

### snippet の順序

1. root binding
2. DI registration
3. DI constructor
4. navigation update
5. command execute / helper / dialog method

## selected files

`selected files` は全文で読む価値があるファイルを優先する。主に次を含める。

- entry XAML / code-behind
- 起動経路
- `ServiceRegistration.cs`
- root view を支える関連ファイル
- command support の補助クラス
- command 影響先として直接呼ばれる service / dialog 実装

## budget 優先順位

予算超過時は次の順で削る。

1. `DataTemplate` 由来の二次文脈
2. 補助サービス
3. 大きいファイルの全文

次は残す。

- entry
- 主対象 ViewModel の導線
- root binding
- 起動経路
- 関連 DI 登録
- `SelectedItem` / `CurrentPage` の更新
- `goal` に関連する command の execute / helper / service 導線
