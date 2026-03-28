# AssessMeister Shell Pack 例

## 入力

```text
entry=file:src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml
goal=Shell 画面に新しいページを追加したい
budget.maxFiles=8
budget.maxTotalLines=1600
budget.maxSnippetsPerFile=3
```

## 期待する pack の性質

- `MainWindow.xaml.cs -> ShellViewModel` の root binding が読める
- `SelectedItem = match` と `CurrentPage = item.PageViewModel` の因果が先頭で読める
- `ShellViewModel` の DI lifetime が分かる
- `DataTemplate` は個別列挙せず、二次文脈の要約として出る
- 巨大な `ShellViewModel.cs` は全文ではなく snippet で出る

## 代表的な contracts

- `起動経路`
- `主要 ViewModel の登録`
- `直接依存のライフタイム`
- `ルート DataContext`
- `選択から表示への因果`
- `ViewModel 更新点`
- `DataTemplate 二次文脈`

## 代表的な index

- `ナビゲーション`
- `選択から表示への因果`
- `ナビゲーション更新点`
- `View-ViewModel`
- `起動経路`
- `DI`
- `コマンド`

## 代表的な selected snippets

```text
src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs:21-55
- reason: navigation-update
- anchor: ShellViewModel(...) / RestoreSelection(...) / Select(...)
```

この snippet には少なくとも次が含まれることを期待します。

- `ShellViewModel(...)`
- `SelectedItem = match`
- `CurrentPage = item.PageViewModel`

## 代表的な selected files

- `src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml`
- `src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml.cs`
- `src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml`
- `src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml.cs`
- `src/AssessMeister.Presentation.Wpf/App.xaml.cs`
- `src/AssessMeister.Presentation.Wpf/ServiceRegistration.cs`
- `src/AssessMeister.Presentation.Wpf/Common/DelegateCommand.cs`

`ShellViewModel.cs` が巨大な場合は、この一覧から外れて `selected snippets` 側に寄ります。
