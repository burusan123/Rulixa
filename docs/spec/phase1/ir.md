# IR 仕様

## 目的
Phase 1 の IR は `scan` の出力として生成し、`resolve-entry` と `pack` が共有する中間表現です。  
対象は `WPF + .NET 8` ワークスペースで、変更開始に必要な構造的事実だけを保持します。

## 設計方針

- JSON に直列化しやすい単純な shape にする
- `pack` で必要な因果と位置情報を保持する
- 曖昧さは `confidence` と `candidates` に残す
- 行位置は Phase 1 では `SourceSpan` に統一する

## ルート shape

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

## SourceSpan

```json
{
  "startLine": 8,
  "endLine": 10
}
```

- `startLine` は 1 以上
- `endLine` は `startLine` 以上
- 1 行だけの要素も `startLine == endLine` で表す

## projectSummary

```json
{
  "solutionFiles": ["RealWorkspace.sln"],
  "projectFiles": ["RealWorkspace.csproj"],
  "targetFrameworks": ["net8.0-windows"],
  "usesWpf": true,
  "entryPoints": ["App.xaml.cs", "MainWindow.xaml.cs"],
  "rootViewModels": ["ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel"]
}
```

## files

```json
{
  "path": "src/ReferenceWorkspace.Presentation.Wpf/Views/ShellView.xaml",
  "kind": "xaml",
  "project": "ReferenceWorkspace.Presentation.Wpf",
  "hash": "sha256:...",
  "lineCount": 120,
  "tags": ["view", "shell"]
}
```

`kind` の値:

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
  "qualifiedName": "ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel",
  "displayName": "ShellViewModel",
  "filePath": "src/ReferenceWorkspace.Presentation.Wpf/ViewModels/ShellViewModel.cs",
  "startLine": 52,
  "endLine": 1900,
  "tags": ["viewmodel", "shell"]
}
```

`kind` の値:

- `class`
- `method`
- `property`
- `command`
- `window`
- `interface`

## viewModelBindings

View と ViewModel の関連を表します。

```json
{
  "viewPath": "src/ReferenceWorkspace.Presentation.Wpf/Views/MainWindow.xaml",
  "viewSymbol": "ReferenceWorkspace.Presentation.Wpf.Views.MainWindow",
  "viewModelSymbol": "ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel",
  "bindingKind": "root-data-context",
  "sourcePath": "src/ReferenceWorkspace.Presentation.Wpf/Views/MainWindow.xaml.cs",
  "sourceSpan": {
    "startLine": 8,
    "endLine": 8
  },
  "confidence": "high",
  "candidates": []
}
```

`bindingKind` の値:

- `root-data-context`
- `view-data-context`
- `data-template`

## navigationTransitions

ViewModel 内の表示更新点を表します。

```json
{
  "viewModelSymbol": "ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel",
  "sourceFilePath": "src/ReferenceWorkspace.Presentation.Wpf/ViewModels/ShellViewModel.cs",
  "updateMethodName": "Select",
  "selectedItemPropertyName": "SelectedItem",
  "currentPagePropertyName": "CurrentPage",
  "updateExpressionSummary": "CurrentPage = item.PageViewModel",
  "sourceSpan": {
    "startLine": 41,
    "endLine": 41
  }
}
```

## commands

```json
{
  "viewModelSymbol": "ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel",
  "propertyName": "OpenDraftingCommand",
  "commandType": "DelegateCommand",
  "executeSymbol": "ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel.OpenDrafting",
  "canExecuteSymbol": "ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel.CanOpenDrafting",
  "boundViews": [
    "src/ReferenceWorkspace.Presentation.Wpf/Views/ShellView.xaml"
  ]
}
```

## windowActivations

```json
{
  "callerSymbol": "ReferenceWorkspace.Presentation.Wpf.Services.DraftingWindowService.OpenDraftingWindow",
  "serviceSymbol": "ReferenceWorkspace.Presentation.Wpf.Services.DraftingWindowService",
  "windowSymbol": "ReferenceWorkspace.Presentation.Wpf.Views.Drafting.DraftingWindow",
  "windowViewModelSymbol": null,
  "activationKind": "show-dialog",
  "ownerKind": "main-window"
}
```

## serviceRegistrations

```json
{
  "registrationFile": "src/ReferenceWorkspace.Presentation.Wpf/ServiceRegistration.cs",
  "serviceType": "ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel",
  "implementationType": "ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel",
  "lifetime": "singleton",
  "sourceSpan": {
    "startLine": 10,
    "endLine": 10
  }
}
```

`lifetime` の値:

- `singleton`
- `scoped`
- `transient`
- `factory`

## diagnostics

```json
{
  "code": "binding.viewmodel.ambiguous",
  "message": "ViewModel 候補が複数あります。",
  "filePath": "src/ReferenceWorkspace.Presentation.Wpf/Views/SomeView.xaml",
  "severity": "warning",
  "candidates": [
    "A.ViewModels.SomeViewModel",
    "B.ViewModels.SomeViewModel"
  ]
}
```

## 主な lookup key

- `files`: `path`
- `symbols`: `qualifiedName`, `filePath`, `startLine`
- `viewModelBindings`: `bindingKind`, `viewPath`, `viewModelSymbol`
- `navigationTransitions`: `sourceFilePath`, `sourceSpan.startLine`
- `commands`: `viewModelSymbol`, `propertyName`
- `windowActivations`: `callerSymbol`, `windowSymbol`
- `serviceRegistrations`: `serviceType`, `implementationType`

## Phase 1 の制約

- AST そのものは保持しない
- 3rd party control 固有の詳細抽出は扱わない
- 複数行 `ServiceRegistration` の厳密 span は次段で拡張する
- XAML 本体の snippet 化は次段に回す
