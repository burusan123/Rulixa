# AssessMeister Pack 例

## 目的

`AssessMeister` の `ShellView.xaml` を入口に、Phase 1 の Pack がどの程度の情報を返すべきかを例示する。

この文書は厳密な最終フォーマットではなく、**期待される情報量と選定理由の基準**を示す。

## 入力

```text
entry=file:src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml
goal=Shell 画面に新しいページを追加したい
budget.maxFiles=8
budget.maxTotalLines=1600
```

## 解決結果

- resolved entry:
  `src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml`
- entry kind:
  `xaml`
- confidence:
  `high`

## 契約

### View-ViewModel 契約

- `MainWindow.xaml.cs` は `ShellViewModel` を `DataContext` に設定する
- `ShellView.xaml` は `CurrentPage` を `ContentControl` に表示する
- `ShellView.xaml` には各 `PageViewModel` に対する `DataTemplate` が定義されている

### ナビゲーション契約

- `ShellViewModel.Items` がタブ一覧を表す
- `SelectedItem` 選択時に `CurrentPage` が切り替わる
- 新しいページ追加時は、タブ定義生成と `DataTemplate` の両方が関係する

### DI 契約

- `ShellViewModel` は DI で解決される
- 依存サービスは `ServiceRegistration.cs` に登録される

## 選定ファイル

1. `src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml`
   入口ファイル。`DataTemplate` と `ContentControl` がある。
2. `MainWindow.xaml.cs`
   `ShellViewModel` が `DataContext` に設定される根拠。
3. `src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs`
   タブ生成、ページ選択、`CurrentPage` 管理を持つ。
4. `src/AssessMeister.Presentation.Wpf/ViewModels/NavItemViewModel.cs`
   タブ項目の構造を持つ。
5. `ServiceRegistration.cs`
   `ShellViewModel` と関連サービスの DI 登録。
6. `App.xaml.cs`
   起動時に `MainWindow` を解決する。
7. `MainWindow.xaml`
   `ShellView` がメイン画面のルートである根拠。
8. `src/AssessMeister.Presentation.Wpf/Common/DelegateCommand.cs`
   `ShellViewModel` のコマンド実装の前提。

## Pack に入れるべき要約

### 起動経路

- `App.OnStartup`
- DI 構築
- `MainWindow` 解決
- `MainWindow.DataContext = ShellViewModel`

### Shell の責務

- 画面上部タブの構築
- 現在ページの切り替え
- 設定画面、作図画面、ライセンス画面などの起動起点

### 追加ページ時の変更候補

- `ShellView.xaml` の `DataTemplate`
- `ShellViewModel.GetTabDefinitions`
- `ShellViewModel.CreatePageViewModel`
- 対応する `PageViewModel`
- 対応する `PageView`

## Unknown / Candidates

- 新ページに必要な DI サービスが既存流儀に従うか
- 新ページが木造/非木造など条件付き表示か
- 3D 表示と連携が必要か

## この例が示すこと

Phase 1 の Pack は、単に入口ファイル周辺を集めるだけでは不足する。  
`WPF` では、少なくとも次の4系統を同時に含める必要がある。

- 起動経路
- View と ViewModel の対応
- タブ/ページ切り替えの責務
- DI とコマンド実装の根拠
