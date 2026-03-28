# WPF + .NET 8 抽出対象

## 目的

Phase 1 では、`WPF + .NET 8` アプリケーションから、AI が変更作業に必要な構造を把握できる最小限の契約と索引を抽出する。

ここでの抽出対象は、`AssessMeister` で実際に確認した構造を踏まえて定義する。

## 1. ホスト起動契約

最初に抽出すべきなのは、アプリの起動からメイン画面表示までの経路である。

対象:

- `App.xaml`
- `App.xaml.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- DI 登録ファイル

抽出したい情報:

- `OnStartup` の有無と主要処理
- `ServiceCollection` / `ServiceProvider` の構築
- `MainWindow` の解決方法
- `MainWindow.DataContext` に設定される ViewModel

期待契約:

- `Application -> MainWindow -> RootViewModel`

## 2. DI 契約

WPF では、XAML だけでは依存関係が見えないため、DI 登録は Pack の重要素材になる。

対象:

- `ServiceRegistration.cs`
- `*.cs` のうち `AddSingleton`, `AddScoped`, `AddTransient` を含むもの

抽出したい情報:

- 登録インターフェース
- 実装型
- ライフタイム
- Window / Dialog / Service / ViewModel の登録

期待契約:

- `Interface -> Implementation`
- `ViewModel -> injected services`
- `Service -> downstream dependency`

## 3. View と ViewModel の対応契約

WPF の主要な暗黙仕様は、View と ViewModel の接続にある。

対象:

- `Views/**/*.xaml`
- `Views/**/*.xaml.cs`
- `ViewModels/**/*.cs`
- `ShellView.xaml` のような `DataTemplate` 定義ファイル

抽出したい情報:

- `x:Class`
- `DataTemplate DataType`
- `ContentControl Content`
- `DataContext = ...`
- View 名と ViewModel 名の命名対応

期待契約:

- `View <-> ViewModel`
- `Window <-> ViewModel`
- `PageViewModel <-> DataTemplate`

確信度:

- `High`
  明示的 `DataContext` 設定、または `DataTemplate DataType`
- `Medium`
  命名規約一致のみ
- `Low`
  間接参照のみ

## 4. ナビゲーション契約

Phase 1 の対象では、画面遷移よりも「シェルがどの PageViewModel を切り替えるか」が重要である。

対象:

- `ShellViewModel`
- `NavItemViewModel`
- タブ定義を構築する箇所
- `CurrentPage`, `SelectedItem` を扱う箇所

抽出したい情報:

- ページ一覧
- 選択条件
- `CurrentPage` に流し込まれる ViewModel
- `ContentControl` での表示先

期待契約:

- `Shell -> PageViewModel list`
- `SelectedItem -> CurrentPage`
- `CurrentPage -> ContentControl`

## 5. Command 契約

AI が WPF で誤りやすいのは、UI 操作がどのメソッドに到達するかの把握である。

対象:

- `ICommand` 公開プロパティ
- `DelegateCommand` / `DelegateCommand<T>` の生成箇所
- XAML の `Command="{Binding ...}"`

抽出したい情報:

- コマンド名
- 実行メソッド
- `CanExecute` 条件
- バインド元 View / ViewModel

期待契約:

- `Button/MenuItem/etc -> ICommand property -> execute method`

## 6. Dialog / SubWindow 契約

WPF は別 Window に文脈が飛びやすいため、`ShowDialog()` 系は最初から抽出対象に含める。

対象:

- `Services/**/*.cs` の `ShowDialog()` / `Show()`
- `new XxxWindow(...)`
- `window.DataContext = ...`

抽出したい情報:

- 呼び出し元サービス
- 起動される Window
- 対応する DialogViewModel
- Owner 設定の有無

期待契約:

- `CallerService -> DialogWindow -> DialogViewModel`

## 7. 設定・プロジェクト契約

WPF 画面だけではなく、設定やプロジェクト状態が UI に流れ込む経路も重要である。

対象:

- `*.csproj`
- `Directory.Build.props`
- `appsettings.*`
- `Properties/Settings.*`
- プロジェクト読み書きサービス

抽出したい情報:

- ターゲットフレームワーク
- `UseWPF`
- 主要 Content / Resource
- 設定読み込みサービス
- 保存/エクスポート/インポート処理

期待契約:

- `Project settings source -> Application service -> ViewModel/UI`

## 8. Pack に優先的に含めるべきファイル

### XAML を入口にした場合

1. 対象 XAML
2. 対応 code-behind
3. 対応 ViewModel
4. その ViewModel が使う主要サービス
5. 対応 `DataTemplate` / Shell 構成

### ViewModel を入口にした場合

1. 対象 ViewModel
2. 対応 View
3. `ICommand` 定義と主要メソッド
4. 注入サービス
5. DI 登録箇所

### Dialog 起動サービスを入口にした場合

1. 対象 Service
2. 起動先 Window
3. 起動先 ViewModel
4. 呼び出し元 ViewModel
5. 関連する XAML

## 9. Phase 1 で明示的に切るもの

- Binding の完全評価
- AttachedProperty / Behavior の完全意味解釈
- VisualState の完全追跡
- 3rd party control の詳細解釈
- 実行時 DI 分岐の完全再現
