# Implementation Plan

## 実装順

### 1. XAML 正規化

- duplicate alias や local namespace の揺れを吸収する
- ResourceDictionary / merged dictionaries を partial support にする
- parse error を classify して diagnostics に送る

### 2. legacy root resolution

- `App.xaml StartupUri`
- code-behind `new MainWindow()`
- `DataContext = new XxxViewModel()`
- service locator / static resolver
  から root candidate を拾う

### 3. fallback route resolution

- constructor DI 以外に
  - direct `new`
  - forwarding helper
  - event handler
  - locator resolved service
  を limited support する

### 4. partial pack assembly

- extraction のどこかが degraded しても pack を返す
- degraded reason を diagnostics にまとめる

### 5. acceptance matrix

- `RealWorkspace`
- `LegacyRealWorkspace`
- synthetic legacy fixtures
  で smoke / regression を構築する

### 6. compare-evidence / supportability

- pack failure ではなく degraded pack になったことが diff で見えるようにする
- diagnostics の改善が evidence で比較できるようにする

## Acceptance

### modern

- `<modern-real-workspace>` では既存の system pack 品質を落とさない

### legacy

- `<legacy-real-workspace>` で pack 実行が crash しない
- root candidate か中心状態か主要 workflow 候補のいずれかが返る
- diagnostics で degraded reason と next candidates が返る

### product-quality

- unsupported construct を含む workspace でも落ちない
- diagnostics が deterministic
- regression で modern / legacy の両方を守る

## Backlog

- `map / drilldown` の mode 分離
- legacy route の deeper drilldown
- WPF 以外への互換性拡張
