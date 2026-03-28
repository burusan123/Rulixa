# IR 仕様

## 目的

Phase 1 の IR は、`scan` の結果をフロントや Pack 生成器から利用できるようにするための、**決定的で最小限の中間表現**である。

Phase 1 では、`WPF + .NET 8` の静的解析結果を表現するのに必要な情報だけを持つ。

## 設計方針

- JSON に直列化しやすい
- 同じ入力から同じ並び順で出力できる
- Pack 生成に必要な根拠を保持する
- 不確実な解決は `confidence` と `candidates` で表現する
- Phase 1 では過剰に一般化しない

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

各ファイルのメタデータを保持する。

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

`kind` の例:

- `xaml`
- `codebehind`
- `viewmodel`
- `service`
- `config`
- `project`
- `startup`

## symbols

Pack 生成や `entry=symbol` 解決に使う最小単位。

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

`kind` の例:

- `class`
- `method`
- `property`
- `command`
- `window`
- `interface`

## viewModelBindings

View と ViewModel の対応を表現する。

```json
{
  "viewPath": "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml",
  "viewSymbol": "AssessMeister.Presentation.Wpf.Views.ShellView",
  "viewModelSymbol": "AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel",
  "bindingKind": "data-template",
  "sourcePath": "src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml",
  "confidence": "high",
  "candidates": []
}
```

`bindingKind` の例:

- `constructor-datacontext`
- `codebehind-datacontext`
- `data-template`
- `naming-convention`

## commands

UI 操作とメソッドの接続を表現する。

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

別 Window / Dialog 起動の経路を表現する。

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

DI から依存関係を取得するための構造。

```json
{
  "registrationFile": "ServiceRegistration.cs",
  "serviceType": "AssessMeister.Presentation.Wpf.Services.IDraftingWindowService",
  "implementationType": "AssessMeister.Presentation.Wpf.Services.DraftingWindowService",
  "lifetime": "singleton"
}
```

`lifetime` は Phase 1 では次に限定する。

- `singleton`
- `scoped`
- `transient`
- `factory`

## diagnostics

解決不能や曖昧性を格納する。

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

決定性を保つため、配列は次でソートする。

- `files`: `path`
- `symbols`: `qualifiedName`, `filePath`, `startLine`
- `viewModelBindings`: `viewPath`, `viewModelSymbol`
- `commands`: `viewModelSymbol`, `propertyName`
- `windowActivations`: `callerSymbol`, `windowSymbol`
- `serviceRegistrations`: `serviceType`, `implementationType`

## Phase 1 の非目標

- 完全なコード意味解析結果の保存
- すべての AST ノードの保持
- 変更差分情報の保持
- ランタイム状態の再現
