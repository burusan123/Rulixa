# AssessMeister Shell Pack 例

## 入力

```text
entry=symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel
goal=Shell 画面に新しいページを追加したい
budget.maxFiles=8
budget.maxTotalLines=1600
budget.maxSnippetsPerFile=3
```

## この pack で先に読めること

- `MainWindow.xaml.cs -> ShellViewModel` の root binding
- `ServiceRegistration.cs` での `ShellViewModel` 登録
- `SelectedItem = match` と `CurrentPage = item.PageViewModel` の因果
- `ShellViewModel` の constructor 注入
- `DataTemplate` は要約だけを残し、二次文脈の増殖を抑える

## 期待する contracts

- `起動経路`
- `主要 ViewModel の登録`
- `直接依存のライフタイム`
- `ルート DataContext`
- `選択から表示への因果`
- `ViewModel 更新点`
- `DataTemplate 二次文脈`

## 期待する selected snippets

```text
src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml.cs:6-9
- reason: root-binding-source
- anchor: ルート DataContext

src/AssessMeister.Presentation.Wpf/ServiceRegistration.cs:8-10
- reason: dependency-injection
- anchor: ShellViewModel (Singleton)

src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs:19-51
- reason: dependency-injection / navigation-update
- anchor: ShellViewModel(...) / RestoreSelection(...) / Select(...)
```

この順序で、binding -> DI -> navigation の読解順を維持します。

## 期待する selected files

- `src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml`
- `src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml.cs`
- `src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml`
- `src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml.cs`
- `src/AssessMeister.Presentation.Wpf/App.xaml.cs`
- `src/AssessMeister.Presentation.Wpf/ServiceRegistration.cs`
- `src/AssessMeister.Presentation.Wpf/Common/DelegateCommand.cs`

`ShellViewModel.cs` は巨大ファイルなので全文では残さず、snippet に置き換えます。
