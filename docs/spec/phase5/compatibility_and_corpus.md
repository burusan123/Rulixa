# Compatibility And Corpus

## 対応対象の拡張

Phase 5 では、Phase 4 で導入した legacy WPF compatibility を「一部の実例で通る」から「代表パターンを継続的に守れる」に引き上げる。

### 重点パターン

- `App.xaml StartupUri`
- `App.xaml.cs` startup handler
- `DataContext = this`
- `DataContext = new XxxViewModel()`
- `new Window()` / `ShowDialog()` / `Show()`
- code-behind event handler からの直接起動
- simple forwarding helper / adapter
- service locator 的な root resolution
- ResourceDictionary / merged dictionaries
- heavy comment / disabled XAML block

### 段階的に扱うパターン

- lambda / delegate 経由の簡易 forwarding
- partial class をまたぐ code-behind route
- static factory / singleton accessor 経由の window creation

## Acceptance Corpus

### Real Workspaces

- `<modern-real-workspace>`
- `<legacy-real-workspace>`

### Synthetic Fixtures

- modern DI-based root
- code-behind-heavy root
- dialog-heavy root
- `DataContext = this` root
- sibling ViewModel 중심 root
- service locator / manual new root
- ResourceDictionary-heavy XAML
- duplicate alias / commented XAML edge case

## Corpus 運用方針

- bug fix には必ず synthetic regression か real smoke のどちらかを追加する
- real workspace 固有に見える不具合も、可能なら最小 synthetic fixture に落とす
- `RealWorkspace` 系に依存した特殊分岐は実装しない

## Acceptance 最低ライン

- root entry で crash-free
- partial pack を含めて有用な map が返る
- unsupported / degraded は diagnostic として見える
- non-root entry の既存品質が壊れない
