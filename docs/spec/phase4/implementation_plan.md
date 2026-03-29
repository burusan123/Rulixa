# Implementation Plan

## 螳溯｣・・
### 1. XAML 豁｣隕丞喧

- duplicate alias 繧・local namespace 縺ｮ謠ｺ繧後ｒ蜷ｸ蜿弱☆繧・- ResourceDictionary / merged dictionaries 繧・partial support 縺ｫ縺吶ｋ
- parse error 繧・classify 縺励※ diagnostics 縺ｫ騾√ｋ

### 2. legacy root resolution

- `App.xaml StartupUri`
- code-behind `new MainWindow()`
- `DataContext = new XxxViewModel()`
- service locator / static resolver
  縺九ｉ root candidate 繧呈鏡縺・
### 3. fallback route resolution

- constructor DI 莉･螟悶↓
  - direct `new`
  - forwarding helper
  - event handler
  - locator resolved service
  繧・limited support 縺吶ｋ

### 4. partial pack assembly

- extraction 縺ｮ縺ｩ縺薙°縺・degraded 縺励※繧・pack 繧定ｿ斐☆
- degraded reason 繧・diagnostics 縺ｫ縺ｾ縺ｨ繧√ｋ

### 5. acceptance matrix

- `RealWorkspace`
- `LegacyRealWorkspace`
- synthetic legacy fixtures
  縺ｧ smoke / regression 繧呈ｧ狗ｯ峨☆繧・
### 6. compare-evidence / supportability

- pack failure 縺ｧ縺ｯ縺ｪ縺・degraded pack 縺ｫ縺ｪ縺｣縺溘％縺ｨ縺・diff 縺ｧ隕九∴繧九ｈ縺・↓縺吶ｋ
- diagnostics 縺ｮ謾ｹ蝟・′ evidence 縺ｧ豈碑ｼ・〒縺阪ｋ繧医≧縺ｫ縺吶ｋ

## Acceptance

### modern

- `<modern-real-workspace>` 縺ｧ縺ｯ譌｢蟄倥・ system pack 蜩∬ｳｪ繧定誠縺ｨ縺輔↑縺・
### legacy

- `<legacy-real-workspace>` 縺ｧ pack 螳溯｡後′ crash 縺励↑縺・- root candidate 縺倶ｸｭ蠢・憾諷九°荳ｻ隕・workflow 蛟呵｣懊・縺・★繧後°縺瑚ｿ斐ｋ
- diagnostics 縺ｧ degraded reason 縺ｨ next candidates 縺瑚ｿ斐ｋ

### product-quality

- unsupported construct 繧貞性繧 workspace 縺ｧ繧り誠縺｡縺ｪ縺・- diagnostics 縺・deterministic
- regression 縺ｧ modern / legacy 縺ｮ荳｡譁ｹ繧貞ｮ医ｋ

## Backlog

- `map / drilldown` 縺ｮ mode 蛻・屬
- legacy route 縺ｮ deeper drilldown
- WPF 莉･螟悶∈縺ｮ莠呈鋤諤ｧ諡｡蠑ｵ


