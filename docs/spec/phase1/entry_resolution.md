# Entry 解決仕様

## 目的

`entry` は、ユーザーが「どこから文脈を集め始めたいか」を指定する入口である。  
Phase 1 では、`WPF + .NET 8` に必要な入口を、曖昧さを残したままではなく、**解決結果つき**で扱う。

## 入力形式

- `file:<path>`
- `symbol:<qualifiedName>`
- `auto:<text>`

## file 解決

### 対象

- `*.xaml`
- `*.xaml.cs`
- `*.cs`
- `*.csproj`
- `*.sln`
- `Directory.Build.props`

### 解決ルール

1. ワークスペース相対パスと絶対パスの両方を受ける
2. 大文字小文字は Windows では無視して一致させる
3. 同名ファイルが複数ある場合は候補を返す
4. 存在しない場合は `unresolved` とする

### 解決結果

```json
{
  "input": "file:src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml",
  "resolvedKind": "file",
  "resolvedPath": "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml",
  "symbol": null,
  "confidence": "high",
  "candidates": []
}
```

## symbol 解決

### 対象

- クラス
- メソッド
- プロパティ
- `ICommand` プロパティ

### 解決ルール

1. 完全修飾名を優先する
2. クラス名のみの場合は候補を返す
3. ネストした型や partial class は同一シンボルとして統合する
4. メソッドは必要なら `Type.Method` まで扱う

### 解決結果

```json
{
  "input": "symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel",
  "resolvedKind": "symbol",
  "resolvedPath": "src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs",
  "symbol": "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel",
  "confidence": "high",
  "candidates": []
}
```

## auto 解決

`auto` は、ファイル名、型名、画面名、略称などを入力された場合に使う。

例:

- `auto:ShellView`
- `auto:ShellViewModel`
- `auto:設定画面`

### 解決手順

1. ファイル名一致
2. クラス名一致
3. View と ViewModel の命名規約一致
4. 部分一致

### 候補提示ルール

- 1件なら自動採用
- 2件以上なら候補一覧を返す
- 候補は `kind`, `path`, `symbol`, `reason` を含める

## WPF Phase 1 の優先的な解決対象

### 1. XAML

優先理由:

- 変更入口として最も自然
- ViewModel、Command、Dialog 契約に広がる

追加で解決すべきもの:

- 対応 code-behind
- 対応 ViewModel
- 対応 DataTemplate

### 2. ViewModel

優先理由:

- Command と依存サービスに到達しやすい

追加で解決すべきもの:

- 対応 View
- 注入サービス
- 公開 `ICommand`

### 3. Dialog 起動サービス

優先理由:

- 文脈が別 Window に飛ぶため、Pack の品質差が大きい

追加で解決すべきもの:

- 起動先 Window
- 起動先 ViewModel
- 呼び出し元 ViewModel

## 曖昧性の扱い

解決できない場合は勝手に決めず、次の形で保持する。

```json
{
  "input": "auto:SettingWindow",
  "resolvedKind": "unresolved",
  "resolvedPath": null,
  "symbol": null,
  "confidence": "low",
  "candidates": [
    {
      "kind": "window",
      "path": "src/.../Views/Settings/SettingWindow.xaml",
      "symbol": "AssessMeister.Presentation.Wpf.Views.Settings.SettingWindow",
      "reason": "file-name-match"
    },
    {
      "kind": "viewmodel",
      "path": "src/.../ViewModels/Settings/SettingWindowViewModel.cs",
      "symbol": "AssessMeister.Presentation.Wpf.ViewModels.Settings.SettingWindowViewModel",
      "reason": "class-name-match"
    }
  ]
}
```

## Pack 生成への橋渡し

解決済み entry から、Phase 1 では次の初期セットを作る。

- `file:xaml`
  対象 XAML + code-behind + ViewModel
- `symbol:viewmodel`
  対象 ViewModel + View + DI 登録
- `symbol:service`
  対象 Service + 起動 Window + 呼び出し元

## Phase 1 の非目標

- 自然言語だけで正確に画面名を完全解決すること
- 実行時のみ有効な ViewModel 差し替えの完全追跡
- すべてのメソッドオーバーロードの完全識別
