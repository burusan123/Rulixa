# IR 仕様

## 目的

Phase 1 の IR は、`scan` の結果を `resolve-entry` と `pack` に渡すための決定的な中間表現です。
Phase 1 では `WPF + .NET 8` の静的解析で取得できる事実だけを表現し、実行時依存の完全解析は扱いません。

## 設計原則

- JSON に直列化しやすい
- 同じ入力から同じ順序で出力できる
- Pack 組み立てに必要な事実だけを持つ
- あいまいな解決結果は `confidence` と `candidates` で表す

## ルート構造

```json
{
  "schemaVersion": "phase1.v1",
  "workspaceRoot": "D:/repo",
  "generatedAtUtc": "2026-03-28T00:00:00Z",
  "projectSummary": {},
  "files": [],
  "symbols": [],
  "viewModelBindings": [],
  "navigationTransitions": [],
  "commands": [],
  "windowActivations": [],
  "serviceRegistrations": [],
  "diagnostics": []
}
```

## projectSummary

```json
{
  "solutionFiles": ["AssessMeister.sln"],
  "projectFiles": ["AssessMeister.csproj"],
  "targetFrameworks": ["net8.0-windows"],
  "usesWpf": true,
  "entryPoints": ["App.xaml.cs", "MainWindow.xaml.cs"],
  "rootViewModels": ["AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel"]
}
```

## files

```json
{
  "path": "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml",
  "kind": "xaml",
  "project": "AssessMeister.Presentation.Wpf",
  "hash": "sha256:...",
  "lineCount": 120,
  "tags": ["view", "shell"]
}
```

`kind` の候補:

- `solution`
- `project`
- `startup`
- `xaml`
- `codebehind`
- `viewmodel`
- `service`
- `config`
- `command-support`
- `csharp`

## symbols

```json
{
  "id": "sym-001",
  "kind": "class",
  "qualifiedName": "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel",
  "displayName": "ShellViewModel",
  "filePath": "src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs",
  "startLine": 52,
  "endLine": 1900,
  "tags": ["viewmodel", "shell"]
}
```

`kind` の候補:

- `class`
- `method`
- `property`
- `command`
- `window`
- `interface`

## viewModelBindings

View と ViewModel の対応を表します。

```json
{
  "viewPath": "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml",
  "viewSymbol": "AssessMeister.Presentation.Wpf.Views.ShellView",
  "viewModelSymbol": "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel",
  "bindingKind": "root-data-context",
  "sourcePath": "src/AssessMeister.Presentation.Wpf/Views/MainWindow.xaml.cs",
  "confidence": "high",
  "candidates": []
}
```

`bindingKind` の候補:

- `root-data-context`
- `view-data-context`
- `data-template`

## navigationTransitions

ViewModel 側のナビゲーション更新点を表します。
Phase 1 では完全な制御フロー解析ではなく、典型的な代入パターンを構文ベースで抽出します。

```json
{
  "viewModelSymbol": "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel",
  "sourceFilePath": "src/AssessMeister.Presentation.Wpf/ViewModels/ShellViewModel.cs",
  "updateMethodName": "Select",
  "selectedItemPropertyName": "SelectedItem",
  "currentPagePropertyName": "CurrentPage",
  "updateExpressionSummary": "CurrentPage = item.PageViewModel",
  "startLine": 3595
}
```

想定する抽出例:

- `CurrentPage = item.PageViewModel`
- `SelectedItem = match`
- `CurrentPage` の初期化

## commands

```json
{
  "viewModelSymbol": "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel",
  "propertyName": "OpenDraftingCommand",
  "commandType": "DelegateCommand",
  "executeSymbol": "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel.OpenDrafting",
  "canExecuteSymbol": "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel.CanOpenDrafting",
  "boundViews": [
    "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml"
  ]
}
```

## windowActivations

```json
{
  "callerSymbol": "AssessMeister.Presentation.Wpf.Services.DraftingWindowService.OpenDraftingWindow",
  "serviceSymbol": "AssessMeister.Presentation.Wpf.Services.DraftingWindowService",
  "windowSymbol": "AssessMeister.Presentation.Wpf.Views.Drafting.DraftingWindow",
  "windowViewModelSymbol": null,
  "activationKind": "show-dialog",
  "ownerKind": "main-window"
}
```

## serviceRegistrations

```json
{
  "registrationFile": "ServiceRegistration.cs",
  "serviceType": "AssessMeister.Presentation.Wpf.Services.IDraftingWindowService",
  "implementationType": "AssessMeister.Presentation.Wpf.Services.DraftingWindowService",
  "lifetime": "singleton"
}
```

`lifetime` の候補:

- `singleton`
- `scoped`
- `transient`
- `factory`

## diagnostics

```json
{
  "code": "binding.viewmodel.ambiguous",
  "message": "ViewModel 候補が複数あります。",
  "filePath": "src/AssessMeister.Presentation.Wpf/Views/SomeView.xaml",
  "severity": "warning",
  "candidates": [
    "A.ViewModels.SomeViewModel",
    "B.ViewModels.SomeViewModel"
  ]
}
```

## 並び順ルール

- `files`: `path`
- `symbols`: `qualifiedName`, `filePath`, `startLine`
- `viewModelBindings`: `bindingKind`, `viewPath`, `viewModelSymbol`
- `navigationTransitions`: `sourceFilePath`, `startLine`
- `commands`: `viewModelSymbol`, `propertyName`
- `windowActivations`: `callerSymbol`, `windowSymbol`
- `serviceRegistrations`: `serviceType`, `implementationType`

## Phase 1 の制約

- 動的コード生成の完全解析はしない
- すべての AST ノードを保持しない
- 制御フローの完全解析はしない
- 3rd party control 固有の内部契約は扱わない
