# Shell Pack 例

## 入力

```text
entry=symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel
goal=Shell 画面の全体ハブと主要 workflow を把握する
budget.maxFiles=8
budget.maxTotalLines=1600
budget.maxSnippetsPerFile=3
```

## 期待する読み方

- `MainWindow.xaml.cs -> ShellViewModel` の root binding を最初に確認する
- `ServiceRegistration.cs` から、どの service と state が Shell に集まるかを見る
- `SelectedItem -> CurrentPage` の navigation update から、画面遷移の中心状態を読む
- `unknowns` がある場合は、next candidates を次の全文検索候補として使う

## 出力の見方

- `contracts`
  pack が保持する高密度な要約です。system map と主要 binding / workflow を読みます。
- `selected snippets`
  contracts を裏付ける根拠です。entry に近い signal から確認します。
- `selected files`
  次に読むコードの入口です。理由付きで採用されます。
- `unknowns`
  未確定事項です。candidate がある場合は次の調査順序に使います。
