# Shell Pack 例

## 入力

```text
entry=symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel
goal=Shell 画面の全体像と主要な workflow を理解する
budget.maxFiles=8
budget.maxTotalLines=1600
budget.maxSnippetsPerFile=3
```

## 期待する読み方

- `MainWindow.xaml.cs -> ShellViewModel` の root binding が見える
- `ServiceRegistration.cs` での `ShellViewModel` 登録が見える
- `SelectedItem -> CurrentPage` の navigation 更新が読める
- `unknowns` が出た場合は、next candidates を全文検索の入口として使う

## representative contracts

- 画面遷移
- 依存性注入
- ルート DataContext
- 選択状態と表示更新
- ViewModel 更新フロー

## representative selected snippets

```text
src/ReferenceWorkspace.Presentation.Wpf/Views/MainWindow.xaml.cs:6-9
- reason: root-binding-source
- anchor: ルート DataContext

src/ReferenceWorkspace.Presentation.Wpf/ServiceRegistration.cs:8-10
- reason: dependency-injection
- anchor: ShellViewModel 登録

src/ReferenceWorkspace.Presentation.Wpf/ViewModels/ShellViewModel.cs:19-51
- reason: dependency-injection / navigation-update
- anchor: ShellViewModel(...) / RestoreSelection(...) / Select(...)
```

## 出力の見方

- `contracts` は、その pack で重要と判断された設計上の要点です
- `selected snippets` は、contracts を裏付ける最小限の根拠です
- `selected files` は、次に全文検索で深掘りするときの入口です
- `unknowns` がある場合は、未確定箇所を隠さず次候補を返していると読んでください
