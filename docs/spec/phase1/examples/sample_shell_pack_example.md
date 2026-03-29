# Shell Pack 例

## 入力

```text
entry=symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel
goal=Shell 画面の全体ハブと主要 workflow を確認する
budget.maxFiles=8
budget.maxTotalLines=1600
budget.maxSnippetsPerFile=3
```

## 期待する読み方

- `MainWindow.xaml.cs -> ShellViewModel` の root binding を最初に確認します。
- `ServiceRegistration.cs` の登録から、どの service と state が Shell に繋がるかを見ます。
- `SelectedItem -> CurrentPage` の navigation update から、画面遷移の中心を確認します。
- `unknowns` がある場合は、next candidates を次の全文検索候補として使います。

## 出力の見方

- `contracts`
  pack が最初に伝えたい要点です。system map と主要な binding / workflow を読みます。
- `selected snippets`
  contracts を裏付ける根拠です。entry に近い signal から確認します。
- `selected files`
  次にコードを読むときの入口です。全文検索の起点として使います。
- `unknowns`
  未確定事項です。候補がある場合は next candidates をそのまま次の探索先に使います。
