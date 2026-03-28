# Phase 1 スコープ

## 目的

Phase 1 の目的は、`WPF + .NET 8` アプリケーションに対して、AI が変更作業を開始するのに必要な最小コンテキストを、**決定的**かつ**安全**に生成できるようにすることです。

対象は「WPF 全般」ではなく、次のような典型構成をまず攻略します。

- `App.xaml / App.xaml.cs` で起動される単一ホスト
- `MainWindow` から `ShellViewModel` を起点に画面が構成される
- `DataTemplate` による View と PageViewModel の対応づけ
- `ObservableObject` / `ICommand` ベースの MVVM
- DI によるサービス登録
- `ShowDialog()` を使う別ウィンドウ起動

## Phase 1 で解く問い

Rulixa は、少なくとも次の問いに答えられる必要があります。

- この XAML はどの ViewModel に支配されているか
- この ViewModel からどのページ/サービス/ダイアログに依存しているか
- この `ICommand` や操作はどのユースケースに繋がるか
- この変更に関連する設定、DI 登録、起動経路はどこか
- どのファイルを AI に見せれば、変更開始に十分か

## 対象範囲

### 対象に含める

- `*.sln`, `*.csproj`, `Directory.Build.props`
- `App.xaml`, `App.xaml.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`
- `Views/**/*.xaml`
- `ViewModels/**/*.cs`
- `Services/**/*.cs`
- `Common/**/*.cs` のうち MVVM 基盤に関わるもの
- DI 登録ファイル
- `ICommand`, `INotifyPropertyChanged`, `ObservableObject` 利用箇所
- `ShowDialog()` / `Show()` による別 Window 起動

### 後回しにする

- 実行時でしか確定しない `DataContext` の完全解決
- 3rd party コントロール固有の詳細仕様
- Visual Tree の完全再現
- XAML Trigger / Behavior の完全意味解釈
- Roslyn による完全な意味解析が必須な機能
- 自動修正やコード生成

## 期待する出力

Phase 1 では、少なくとも次を出力できることを目標にします。

- `Contracts`
  View-ViewModel 対応、Command 契約、Dialog 起動契約、設定/DI 契約
- `Index`
  起動経路、ページ一覧、主要 ViewModel 一覧、サービス依存、Window 起動点
- `Context Pack`
  `entry + goal + budget` に基づく最小ファイル束

## 入力

### entry

- `file:<path>`
- `symbol:<qualifiedName>`
- `auto:<text>`

Phase 1 では特に次の入口を重視します。

- XAML ファイル
- ViewModel クラス
- DI 登録ファイル
- Window / Dialog 起動サービス

### goal

例:

- 「この画面にボタンを追加したい」
- 「このダイアログ起動条件を変えたい」
- 「このページの保存処理を追いたい」

### budget

- `maxFiles`
- `maxTotalLines`
- `maxSnippetsPerFile`

## 成功条件

- 同じ `entry + goal + budget` なら同じ Pack が生成される
- XAML 変更時に、関連する ViewModel とサービスが最低限 Pack に入る
- ViewModel 変更時に、関連 View と主要依存サービスが最低限 Pack に入る
- `ShowDialog()` 系変更時に、呼び出し元 ViewModel/Service と対象 Window/ViewModel が Pack に入る
- 不要なファイルを大量に含めず、AI が読める太さに収まる

## 非目標

- WPF アプリの完全理解
- デザイナ互換性の再現
- すべての Binding エラー検出
- すべての MVVM スタイルへの一発対応

## 観察対象コードベース

Phase 1 の具体仕様は、`D:\C#\AssessMeister` を観察した結果を主要な根拠の一つとする。

特に次の特徴を持つコードベースに適用価値がある。

- 独自 `ObservableObject` / `DelegateCommand`
- `ShellViewModel` にページ切り替え責務が集中
- `ShellView.xaml` の `DataTemplate` で View と ViewModel を関連づける
- サービスが `ShowDialog()` で別 Window を起動する
