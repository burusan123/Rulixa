# Compatibility Targets

## 対応対象

Phase 4 では、次のような現実の WPF 構成を一次対象にする。

### 起動パターン

- `App.xaml` の `StartupUri` で MainWindow を開く
- `App.xaml.cs` から `new MainWindow()` する
- `App.xaml.cs` から service locator で Window を取得する
- Window code-behind で `DataContext = new XxxViewModel()` を設定する

### View / ViewModel 解決

- `DataContext` の直接代入
- code-behind 内 `new`
- factory 経由生成
- static resolver / locator 経由解決
- partial class に分割された ViewModel

### 画面遷移

- dialog service
- `new Window().Show()` / `ShowDialog()`
- event handler 起点の遷移
- command でない button click

### XAML 構成

- merged ResourceDictionary
- custom local namespace alias
- duplicate alias や曖昧 alias
- 古い書き方の attached property
- code-behind 前提の画面構成

## 対応レベル

### Green

- root / viewmodel / workflow / persistence まで安定抽出できる
- system pack が成立する

### Amber

- root と主要 signal は抽出できる
- workflow / persistence の一部は diagnostics 付き partial pack

### Red

- pack は返るが、signal は限定的
- diagnostics と全文検索 handoff が主役になる

## Product-grade 条件

- Red でも crash しない
- Amber でも次の探索候補が deterministic
- Green / Amber / Red の判定基準がテストで固定される
