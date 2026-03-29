# Implementation Plan

## 螳溯｣・・
### 1. root seed / system expansion planner

- root seed 蛻､螳壹ｒ霑ｽ蜉縺吶ｋ
- root 縺九ｉ first expansion 縺吶ｋ planner 繧定ｿｽ蜉縺吶ｋ
- family 縺斐→縺ｮ sub-map candidate 繧貞庶髮・☆繧・
### 2. sub-map aggregation

- family 蜊倅ｽ阪〒螻謇 map 繧呈據縺ｭ繧・- root / sibling / dialog / sub-window 繧呈ｨｪ譁ｭ縺励◆ route 繧・canonicalize 縺吶ｋ

### 3. section compression / unknown aggregation

- 譌｢蟄・section 縺ｫ system-level representative 繧定ｼ峨○繧・- sub-map unknown 繧・system-level guidance 縺ｫ髮・ｴ・☆繧・
### 4. renderer / compare-evidence

- system-level summary 繧・renderer 縺ｫ霑ｽ蜉縺吶ｋ
- compare-evidence 縺ｧ sub-map representative 縺ｮ霑ｽ蜉縺瑚ｪｭ縺ｿ蜿悶ｌ繧九ｈ縺・↓縺吶ｋ

### 5. fixture / smoke / regression

- root 縺九ｉ sibling sub-map 縺檎ｵｱ蜷医＆繧後ｋ fixture 繧定ｿｽ蜉縺吶ｋ
- optional smoke 縺ｧ `RealWorkspace` 縺ｮ `ShellViewModel` system pack 繧呈､懆ｨｼ縺吶ｋ

## Acceptance

### fixture / unit

- root ViewModel 縺九ｉ sibling sub-map 縺檎ｵｱ蜷医＆繧後ｋ
- dialog / window 邨檎罰縺ｧ荳ｻ隕√し繝悶す繧ｹ繝・Β縺梧鏡繧上ｌ繧・- system-level 縺ｧ hub object / persistence / external asset / architecture test 縺碁㍾隍・○縺壼悸邵ｮ縺輔ｌ繧・- system-level unknown 縺・sub-map unknown 繧帝寔邏・＠縲∝呵｣懊′ 3 莉ｶ莉･蜀・〒蜃ｺ繧・- 蜷後§ workspace 縺ｧ deterministic 縺ｫ蜷後§ pack 縺悟・繧・
### optional smoke

- `RealWorkspace` 縺ｮ `ShellViewModel` pack 縺ｧ `Shell + Drafting + Settings/Report + Architecture` 縺瑚ｪｭ繧√ｋ
- `ProjectDocument` 縺御ｸｭ蠢・憾諷九→縺励※谿九ｋ
- drafting 邉ｻ縺・direct chain 縺・guided unknown 縺ｮ縺ｩ縺｡繧峨°縺ｧ蜷ｫ縺ｾ繧後ｋ

### compare-evidence

- Phase2 pack 縺ｨ豈碑ｼ・＠縺ｦ縲∽ｻｶ謨ｰ蠅怜刈縺ｧ縺ｯ縺ｪ縺・system-level representative 縺ｮ霑ｽ蜉縺瑚ｪｭ繧√ｋ

## Public Interfaces

- 譁ｰ縺励＞ CLI 繧ｳ繝槭Φ繝峨・霑ｽ蜉縺励↑縺・- `pack` 繧堤ｶｭ謖√＠縲〉oot entry 縺ｮ縺ｨ縺阪↓蜀・Κ縺ｧ system expansion 繧呈怏蜉ｹ蛹悶☆繧・- `ContextPack` 縺ｨ evidence manifest 縺ｮ shape 縺ｯ螟画峩縺励↑縺・- `pack --mode map|drilldown` 縺ｯ future backlog 縺ｨ縺励※謇ｱ縺・
## Backlog

- `pack --mode map|drilldown`
- helper / lambda / adapter 繧偵∪縺溘＄荳闊ｬ deep drilldown 蠑ｷ蛹・- WPF 莉･螟悶・ plugin 讓ｪ螻暮幕


