# Compatibility Targets

## 蟇ｾ蠢懷ｯｾ雎｡

Phase 4 縺ｧ縺ｯ縲∵ｬ｡縺ｮ繧医≧縺ｪ迴ｾ螳溘・ WPF 讒区・繧剃ｸ谺｡蟇ｾ雎｡縺ｫ縺吶ｋ縲・
### 襍ｷ蜍輔ヱ繧ｿ繝ｼ繝ｳ

- `App.xaml` 縺ｮ `StartupUri` 縺ｧ MainWindow 繧帝幕縺・- `App.xaml.cs` 縺九ｉ `new MainWindow()` 縺吶ｋ
- `App.xaml.cs` 縺九ｉ service locator 縺ｧ Window 繧貞叙蠕励☆繧・- Window code-behind 縺ｧ `DataContext = new XxxViewModel()` 繧定ｨｭ螳壹☆繧・
### View / ViewModel 隗｣豎ｺ

- `DataContext` 縺ｮ逶ｴ謗･莉｣蜈･
- code-behind 蜀・`new`
- factory 邨檎罰逕滓・
- static resolver / locator 邨檎罰隗｣豎ｺ
- partial class 縺ｫ蛻・牡縺輔ｌ縺・ViewModel

### 逕ｻ髱｢驕ｷ遘ｻ

- dialog service
- `new Window().Show()` / `ShowDialog()`
- event handler 襍ｷ轤ｹ縺ｮ驕ｷ遘ｻ
- command 縺ｧ縺ｪ縺・button click

### XAML 讒区・

- merged ResourceDictionary
- custom local namespace alias
- duplicate alias 繧・尠譏ｧ alias
- 蜿､縺・嶌縺肴婿縺ｮ attached property
- code-behind 蜑肴署縺ｮ逕ｻ髱｢讒区・

## 蟇ｾ蠢懊Ξ繝吶Ν

### Green

- root / viewmodel / workflow / persistence 縺ｾ縺ｧ螳牙ｮ壽歓蜃ｺ縺ｧ縺阪ｋ
- system pack 縺梧・遶九☆繧・
### Amber

- root 縺ｨ荳ｻ隕・signal 縺ｯ謚ｽ蜃ｺ縺ｧ縺阪ｋ
- workflow / persistence 縺ｮ荳驛ｨ縺ｯ diagnostics 莉倥″ partial pack

### Red

- pack 縺ｯ霑斐ｋ縺後《ignal 縺ｯ髯仙ｮ夂噪
- diagnostics 縺ｨ蜈ｨ譁・､懃ｴ｢ handoff 縺御ｸｻ蠖ｹ縺ｫ縺ｪ繧・
## Product-grade 譚｡莉ｶ

- Red 縺ｧ繧・crash 縺励↑縺・- Amber 縺ｧ繧よｬ｡縺ｮ謗｢邏｢蛟呵｣懊′ deterministic
- Green / Amber / Red 縺ｮ蛻､螳壼渕貅悶′繝・せ繝医〒蝗ｺ螳壹＆繧後ｋ


