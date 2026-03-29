# IR 莉墓ｧ・
## 逶ｮ逧・Phase 1 縺ｮ IR 縺ｯ `scan` 縺ｮ蜃ｺ蜉帙→縺励※逕滓・縺励～resolve-entry` 縺ｨ `pack` 縺悟・譛峨☆繧倶ｸｭ髢楢｡ｨ迴ｾ縺ｧ縺吶・ 
蟇ｾ雎｡縺ｯ `WPF + .NET 8` 繝ｯ繝ｼ繧ｯ繧ｹ繝壹・繧ｹ縺ｧ縲∝､画峩髢句ｧ九↓蠢・ｦ√↑讒矩逧・ｺ句ｮ溘□縺代ｒ菫晄戟縺励∪縺吶・
## 險ｭ險域婿驥・
- JSON 縺ｫ逶ｴ蛻怜喧縺励ｄ縺吶＞蜊倡ｴ斐↑ shape 縺ｫ縺吶ｋ
- `pack` 縺ｧ蠢・ｦ√↑蝗譫懊→菴咲ｽｮ諠・ｱ繧剃ｿ晄戟縺吶ｋ
- 譖匁乂縺輔・ `confidence` 縺ｨ `candidates` 縺ｫ谿九☆
- 陦御ｽ咲ｽｮ縺ｯ Phase 1 縺ｧ縺ｯ `SourceSpan` 縺ｫ邨ｱ荳縺吶ｋ

## 繝ｫ繝ｼ繝・shape

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

- `startLine` 縺ｯ 1 莉･荳・- `endLine` 縺ｯ `startLine` 莉･荳・- 1 陦後□縺代・隕∫ｴ繧・`startLine == endLine` 縺ｧ陦ｨ縺・
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

`kind` 縺ｮ蛟､:

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

`kind` 縺ｮ蛟､:

- `class`
- `method`
- `property`
- `command`
- `window`
- `interface`

## viewModelBindings

View 縺ｨ ViewModel 縺ｮ髢｢騾｣繧定｡ｨ縺励∪縺吶・
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

`bindingKind` 縺ｮ蛟､:

- `root-data-context`
- `view-data-context`
- `data-template`

## navigationTransitions

ViewModel 蜀・・陦ｨ遉ｺ譖ｴ譁ｰ轤ｹ繧定｡ｨ縺励∪縺吶・
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

`lifetime` 縺ｮ蛟､:

- `singleton`
- `scoped`
- `transient`
- `factory`

## diagnostics

```json
{
  "code": "binding.viewmodel.ambiguous",
  "message": "ViewModel 蛟呵｣懊′隍・焚縺ゅｊ縺ｾ縺吶・,
  "filePath": "src/ReferenceWorkspace.Presentation.Wpf/Views/SomeView.xaml",
  "severity": "warning",
  "candidates": [
    "A.ViewModels.SomeViewModel",
    "B.ViewModels.SomeViewModel"
  ]
}
```

## 荳ｻ縺ｪ lookup key

- `files`: `path`
- `symbols`: `qualifiedName`, `filePath`, `startLine`
- `viewModelBindings`: `bindingKind`, `viewPath`, `viewModelSymbol`
- `navigationTransitions`: `sourceFilePath`, `sourceSpan.startLine`
- `commands`: `viewModelSymbol`, `propertyName`
- `windowActivations`: `callerSymbol`, `windowSymbol`
- `serviceRegistrations`: `serviceType`, `implementationType`

## Phase 1 縺ｮ蛻ｶ邏・
- AST 縺昴・繧ゅ・縺ｯ菫晄戟縺励↑縺・- 3rd party control 蝗ｺ譛峨・隧ｳ邏ｰ謚ｽ蜃ｺ縺ｯ謇ｱ繧上↑縺・- 隍・焚陦・`ServiceRegistration` 縺ｮ蜴ｳ蟇・span 縺ｯ谺｡谿ｵ縺ｧ諡｡蠑ｵ縺吶ｋ
- XAML 譛ｬ菴薙・ snippet 蛹悶・谺｡谿ｵ縺ｫ蝗槭☆


