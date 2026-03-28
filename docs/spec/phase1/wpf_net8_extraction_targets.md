# WPF + .NET 8 抽出対象

## 目的

Phase 1 では `WPF + .NET 8` アプリケーションから、AI が変更開始に必要な最小限の事実だけを抽出します。
この文書では `AssessMeister` で実際に価値が高かった抽出対象を整理します。

## 1. ホストと起動経路

最初に抽出すべき対象:

- `App.xaml`
- `App.xaml.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- DI 登録ファイル

抽出する事実:

- `OnStartup` の存在
- `ServiceCollection` / `ServiceProvider` の構成
- `MainWindow` の解決方法
- `MainWindow.DataContext` に設定されるルート ViewModel

## 2. DI

対象:

- `ServiceRegistration.cs`
- `AddSingleton` / `AddScoped` / `AddTransient` / `AddFactory` を含むファイル

抽出する事実:

- 登録ファイル
- ライフタイム
- `Interface -> Implementation`
- `ViewModel -> 注入サービス`

## 3. View と ViewModel の対応

対象:

- `Views/**/*.xaml`
- `Views/**/*.xaml.cs`
- `ViewModels/**/*.cs`

抽出する事実:

- `x:Class`
- `DataContext = ...`
- `DataTemplate DataType`
- View と ViewModel の対応

信頼度:

- `High`
  明示的な `DataContext` 設定、または `DataTemplate DataType`
- `Medium`
  code-behind と XAML の対応
- `Low`
  命名規約だけによる推定

## 4. ナビゲーション

Phase 1 の主戦場です。`ShellView.xaml` の binding だけではなく、`ShellViewModel` 側の更新点まで抽出します。

対象:

- `ShellView.xaml`
- `ShellViewModel.cs`
- `NavItemViewModel.cs`

抽出する事実:

- `ItemsSource`
- `SelectedItem`
- `Content`
- `SelectedItem` の更新地点
- `CurrentPage` の更新地点
- 代入を含むメソッド名
- 行番号

Pack に残したい導線:

- `Items -> SelectedItem -> CurrentPage`
- `SelectedItem = match`
- `Select(...) -> CurrentPage = item.PageViewModel`

## 5. Command

対象:

- `ICommand` 公開プロパティ
- `DelegateCommand` / `DelegateCommand<T>`
- XAML の `Command="{Binding ...}"`

抽出する事実:

- コマンド名
- 実行メソッド
- `CanExecute`
- バインドされる View

## 6. Dialog / SubWindow

対象:

- `Services/**/*.cs` の `ShowDialog()` / `Show()`
- `new XxxWindow(...)`
- `window.DataContext = ...`

抽出する事実:

- 呼び出し元サービス
- 起動される Window
- 対応する DialogViewModel

## 7. 設定とプロジェクト

対象:

- `*.csproj`
- `Directory.Build.props`
- `appsettings.*`
- `Properties/Settings.*`

抽出する事実:

- `TargetFramework`
- `UseWPF`
- 主要な設定ソース

## 8. Pack に優先して入れるファイル

### XAML 起点

1. 対象 XAML
2. 対応する code-behind
3. 対応する ViewModel
4. 起動経路
5. DI 登録

### ViewModel 起点

1. 対象 ViewModel
2. 対応 View
3. 対応 code-behind
4. `ICommand` と実行メソッド
5. `SelectedItem` / `CurrentPage` の更新点
6. 起動経路
7. DI 登録

## 9. Phase 1 で扱わないもの

- 完全なデータフロー解析
- AttachedProperty / Behavior の完全解析
- VisualState の完全解析
- 3rd party control 固有の詳細解析
- 実行時 DI コンテナ状態の完全解析
