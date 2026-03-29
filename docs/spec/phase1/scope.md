# Phase 1 繧ｹ繧ｳ繝ｼ繝・
## 逶ｮ逧・
Phase 1 縺ｮ逶ｮ逧・・縲～WPF + .NET 8` 繧｢繝励Μ繧ｱ繝ｼ繧ｷ繝ｧ繝ｳ縺ｫ蟇ｾ縺励※縲、I 縺悟､画峩菴懈･ｭ繧帝幕蟋九☆繧九・縺ｫ蠢・ｦ√↑譛蟆上さ繝ｳ繝・く繧ｹ繝医ｒ縲・*豎ｺ螳夂噪**縺九▽**螳牙・**縺ｫ逕滓・縺ｧ縺阪ｋ繧医≧縺ｫ縺吶ｋ縺薙→縺ｧ縺吶・
蟇ｾ雎｡縺ｯ縲係PF 蜈ｨ闊ｬ縲阪〒縺ｯ縺ｪ縺上∵ｬ｡縺ｮ繧医≧縺ｪ蜈ｸ蝙区ｧ区・繧偵∪縺壽判逡･縺励∪縺吶・
- `App.xaml / App.xaml.cs` 縺ｧ襍ｷ蜍輔＆繧後ｋ蜊倅ｸ繝帙せ繝・- `MainWindow` 縺九ｉ `ShellViewModel` 繧定ｵｷ轤ｹ縺ｫ逕ｻ髱｢縺梧ｧ区・縺輔ｌ繧・- `DataTemplate` 縺ｫ繧医ｋ View 縺ｨ PageViewModel 縺ｮ蟇ｾ蠢懊▼縺・- `ObservableObject` / `ICommand` 繝吶・繧ｹ縺ｮ MVVM
- DI 縺ｫ繧医ｋ繧ｵ繝ｼ繝薙せ逋ｻ骭ｲ
- `ShowDialog()` 繧剃ｽｿ縺・挨繧ｦ繧｣繝ｳ繝峨え襍ｷ蜍・
## Phase 1 縺ｧ隗｣縺丞撫縺・
Rulixa 縺ｯ縲∝ｰ代↑縺上→繧よｬ｡縺ｮ蝠上＞縺ｫ遲斐∴繧峨ｌ繧句ｿ・ｦ√′縺ゅｊ縺ｾ縺吶・
- 縺薙・ XAML 縺ｯ縺ｩ縺ｮ ViewModel 縺ｫ謾ｯ驟阪＆繧後※縺・ｋ縺・- 縺薙・ ViewModel 縺九ｉ縺ｩ縺ｮ繝壹・繧ｸ/繧ｵ繝ｼ繝薙せ/繝繧､繧｢繝ｭ繧ｰ縺ｫ萓晏ｭ倥＠縺ｦ縺・ｋ縺・- 縺薙・ `ICommand` 繧・桃菴懊・縺ｩ縺ｮ繝ｦ繝ｼ繧ｹ繧ｱ繝ｼ繧ｹ縺ｫ郢九′繧九°
- 縺薙・螟画峩縺ｫ髢｢騾｣縺吶ｋ險ｭ螳壹．I 逋ｻ骭ｲ縲∬ｵｷ蜍慕ｵ瑚ｷｯ縺ｯ縺ｩ縺薙°
- 縺ｩ縺ｮ繝輔ぃ繧､繝ｫ繧・AI 縺ｫ隕九○繧後・縲∝､画峩髢句ｧ九↓蜊∝・縺・
## 蟇ｾ雎｡遽・峇

### 蟇ｾ雎｡縺ｫ蜷ｫ繧√ｋ

- `*.sln`, `*.csproj`, `Directory.Build.props`
- `App.xaml`, `App.xaml.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`
- `Views/**/*.xaml`
- `ViewModels/**/*.cs`
- `Services/**/*.cs`
- `Common/**/*.cs` 縺ｮ縺・■ MVVM 蝓ｺ逶､縺ｫ髢｢繧上ｋ繧ゅ・
- DI 逋ｻ骭ｲ繝輔ぃ繧､繝ｫ
- `ICommand`, `INotifyPropertyChanged`, `ObservableObject` 蛻ｩ逕ｨ邂・園
- `ShowDialog()` / `Show()` 縺ｫ繧医ｋ蛻･ Window 襍ｷ蜍・
### 蠕悟屓縺励↓縺吶ｋ

- 螳溯｡梧凾縺ｧ縺励°遒ｺ螳壹＠縺ｪ縺・`DataContext` 縺ｮ螳悟・隗｣豎ｺ
- 3rd party 繧ｳ繝ｳ繝医Ο繝ｼ繝ｫ蝗ｺ譛峨・隧ｳ邏ｰ莉墓ｧ・- Visual Tree 縺ｮ螳悟・蜀咲樟
- XAML Trigger / Behavior 縺ｮ螳悟・諢丞袖隗｣驥・- Roslyn 縺ｫ繧医ｋ螳悟・縺ｪ諢丞袖隗｣譫舌′蠢・医↑讖溯・
- 閾ｪ蜍穂ｿｮ豁｣繧・さ繝ｼ繝臥函謌・
## 譛溷ｾ・☆繧句・蜉・
Phase 1 縺ｧ縺ｯ縲∝ｰ代↑縺上→繧よｬ｡繧貞・蜉帙〒縺阪ｋ縺薙→繧堤岼讓吶↓縺励∪縺吶・
- `Contracts`
  View-ViewModel 蟇ｾ蠢懊，ommand 螂醍ｴ・．ialog 襍ｷ蜍募･醍ｴ・∬ｨｭ螳・DI 螂醍ｴ・- `Index`
  襍ｷ蜍慕ｵ瑚ｷｯ縲√・繝ｼ繧ｸ荳隕ｧ縲∽ｸｻ隕・ViewModel 荳隕ｧ縲√し繝ｼ繝薙せ萓晏ｭ倥仝indow 襍ｷ蜍慕せ
- `Context Pack`
  `entry + goal + budget` 縺ｫ蝓ｺ縺･縺乗怙蟆上ヵ繧｡繧､繝ｫ譚・
## 蜈･蜉・
### entry

- `file:<path>`
- `symbol:<qualifiedName>`
- `auto:<text>`

Phase 1 縺ｧ縺ｯ迚ｹ縺ｫ谺｡縺ｮ蜈･蜿｣繧帝㍾隕悶＠縺ｾ縺吶・
- XAML 繝輔ぃ繧､繝ｫ
- ViewModel 繧ｯ繝ｩ繧ｹ
- DI 逋ｻ骭ｲ繝輔ぃ繧､繝ｫ
- Window / Dialog 襍ｷ蜍輔し繝ｼ繝薙せ

### goal

萓・

- 縲後％縺ｮ逕ｻ髱｢縺ｫ繝懊ち繝ｳ繧定ｿｽ蜉縺励◆縺・・- 縲後％縺ｮ繝繧､繧｢繝ｭ繧ｰ襍ｷ蜍墓擅莉ｶ繧貞､峨∴縺溘＞縲・- 縲後％縺ｮ繝壹・繧ｸ縺ｮ菫晏ｭ伜・逅・ｒ霑ｽ縺・◆縺・・
### budget

- `maxFiles`
- `maxTotalLines`
- `maxSnippetsPerFile`

## 謌仙粥譚｡莉ｶ

- 蜷後§ `entry + goal + budget` 縺ｪ繧牙酔縺・Pack 縺檎函謌舌＆繧後ｋ
- XAML 螟画峩譎ゅ↓縲・未騾｣縺吶ｋ ViewModel 縺ｨ繧ｵ繝ｼ繝薙せ縺梧怙菴朱剞 Pack 縺ｫ蜈･繧・- ViewModel 螟画峩譎ゅ↓縲・未騾｣ View 縺ｨ荳ｻ隕∽ｾ晏ｭ倥し繝ｼ繝薙せ縺梧怙菴朱剞 Pack 縺ｫ蜈･繧・- `ShowDialog()` 邉ｻ螟画峩譎ゅ↓縲∝他縺ｳ蜃ｺ縺怜・ ViewModel/Service 縺ｨ蟇ｾ雎｡ Window/ViewModel 縺・Pack 縺ｫ蜈･繧・- 荳崎ｦ√↑繝輔ぃ繧､繝ｫ繧貞､ｧ驥上↓蜷ｫ繧√★縲、I 縺瑚ｪｭ繧√ｋ螟ｪ縺輔↓蜿弱∪繧・
## 髱樒岼讓・
- WPF 繧｢繝励Μ縺ｮ螳悟・逅・ｧ｣
- 繝・じ繧､繝贋ｺ呈鋤諤ｧ縺ｮ蜀咲樟
- 縺吶∋縺ｦ縺ｮ Binding 繧ｨ繝ｩ繝ｼ讀懷・
- 縺吶∋縺ｦ縺ｮ MVVM 繧ｹ繧ｿ繧､繝ｫ縺ｸ縺ｮ荳逋ｺ蟇ｾ蠢・
## 隕ｳ蟇溷ｯｾ雎｡繧ｳ繝ｼ繝峨・繝ｼ繧ｹ

Phase 1 縺ｮ蜈ｷ菴謎ｻ墓ｧ倥・縲～<modern-real-workspace>` 繧定ｦｳ蟇溘＠縺溽ｵ先棡繧剃ｸｻ隕√↑譬ｹ諡縺ｮ荳縺､縺ｨ縺吶ｋ縲・
迚ｹ縺ｫ谺｡縺ｮ迚ｹ蠕ｴ繧呈戟縺､繧ｳ繝ｼ繝峨・繝ｼ繧ｹ縺ｫ驕ｩ逕ｨ萓｡蛟､縺後≠繧九・
- 迢ｬ閾ｪ `ObservableObject` / `DelegateCommand`
- `ShellViewModel` 縺ｫ繝壹・繧ｸ蛻・ｊ譖ｿ縺郁ｲｬ蜍吶′髮・ｸｭ
- `ShellView.xaml` 縺ｮ `DataTemplate` 縺ｧ View 縺ｨ ViewModel 繧帝未騾｣縺･縺代ｋ
- 繧ｵ繝ｼ繝薙せ縺・`ShowDialog()` 縺ｧ蛻･ Window 繧定ｵｷ蜍輔☆繧・

