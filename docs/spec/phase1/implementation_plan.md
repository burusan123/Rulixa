# 実装計画

## 位置づけ

この文書は Phase 1 の実装計画兼実装メモです。
`AssessMeister` を題材にした具体計画を含むため、製品全体の正本ではありません。

## 方針

Phase 1 は、巨大コードベース全体を理解することではなく、`WPF + .NET 8` アプリケーションに対して AI が変更開始に必要な最小 Context Pack を決定的に生成することを目標にします。

実装順は次のとおりです。

1. Domain
2. Application
3. Plugin / Infrastructure
4. Frontend

## 現在の実装状況

### Domain

- `Entry`
- `ResolvedEntry`
- `Budget`
- `ContextPack`
- `Contract`
- `IndexSection`
- `NavigationTransition`
- Pack 選定ルール

### Application

- `ScanWorkspaceUseCase`
- `ResolveEntryUseCase`
- `BuildContextPackUseCase`
- `IWorkspaceScanner`
- `IEntryResolver`
- `IContractExtractor`
- `IContextPackRenderer`

### Plugin / Infrastructure

- `WPF + .NET 8` 走査
- View-ViewModel 抽出
- Command 抽出
- Dialog 抽出
- DI 抽出
- Markdown 出力
- `SelectedItem` / `CurrentPage` の更新点抽出

### Frontend

- `Rulixa.Cli`
- Codex Plugin `rulixa`

## 受け入れ済みユースケース

### file entry

```text
entry=file:src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml
goal=Shell 画面に新しいページを追加したい
```

### symbol entry

```text
entry=symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel
goal=Shell 画面に新しいページを追加したい
```

`symbol` 起点では少なくとも次を Pack に含めます。

- `ShellViewModel.cs`
- `ShellView.xaml`
- `ShellView.xaml.cs`
- `MainWindow.xaml.cs`
- `App.xaml.cs`
- `ServiceRegistration.cs`
- `DelegateCommand.cs`

また、出力には少なくとも次を含めます。

- `Items=Items, SelectedItem=SelectedItem, Content=CurrentPage`
- `SelectedItem = match`
- `Select(...) -> CurrentPage = item.PageViewModel`

## scan pipeline

1. ワークスペース列挙
2. solution / csproj 読み取り
3. WPF 関連ファイル抽出
4. Symbol 抽出
5. View-ViewModel 抽出
6. NavigationTransition 抽出
7. Command 抽出
8. Dialog 抽出
9. DI 抽出
10. IR 生成

## テスト方針

### Domain

- Pack 選定順位
- budget 超過時の削減順

### Application

- `entry=file/symbol/auto` 解決
- Pack 生成の受け入れ

### Plugin / Infrastructure

- `AssessMeister` 風 fixture に対する走査
- `SelectedItem` / `CurrentPage` 更新点の抽出
- Markdown 出力

## 改善バックログ

### P1

- `DataContext` の由来を `root / view / code-behind` でさらに明示化する
- `SelectedItem` と `CurrentPage` の因果関係を、Pack の契約としてより短く強く表現する

### P2

- DI ライフタイムを Pack に表示する
- コマンドの影響対象を 1 段深く出す
- 選定ファイル理由の粒度をさらに上げる

### P3

- 巨大 ViewModel のスニペット抽出
- 行番号の利用強化
- `DataTemplate` を「省略した二次文脈」として明示する

## 成功条件

- Pack だけ見て、次に開くべきファイルと行が分かる
- 直接確認が必要な範囲を 3〜5 箇所まで狭められる
- `symbol` 起点の Pack が既定 budget で安定して同じ結果を返す
