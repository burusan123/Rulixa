# Phase 2 螳溯｣・ｨ育判

## 螳溯｣・婿驥・
Phase 2 縺ｯ荳諡ｬ螳溯｣・〒縺ｯ縺ｪ縺上￣ack 縺ｮ蜩∬ｳｪ繧貞｣翫＆縺ｪ縺・ｈ縺・↓谿ｵ髫主ｰ主・縺吶ｋ縲・譌｢蟄・`scan -> resolve-entry -> pack` 縺ｮ CLI 螂醍ｴ・・邯ｭ謖√＠縲∝・驛ｨ謾ｹ蝟・→縺励※騾ｲ繧√ｋ縲・
## 繧ｹ繝・ャ繝・
### Step 1. partial 邨ｱ蜷・
- partial symbol 縺ｮ髮・ｴ・・逅・ｒ霑ｽ蜉
- `pack` 縺ｮ relevant symbol 邂怜・繧・partial aware 縺ｫ螟画峩
- 譌｢蟄・WPF pack 縺ｮ蟾ｮ蛻・ユ繧ｹ繝医ｒ霑ｽ蜉

螳御ｺ・擅莉ｶ:

- partial class 縺ｮ莉｣陦ｨ繝輔ぃ繧､繝ｫ縺悟､峨ｏ縺｣縺ｦ繧ゅ∽ｸｻ隕・contracts 縺悟､ｧ縺阪￥縺ｶ繧後↑縺・
### Step 2. goal-driven expansion 縺ｮ蝓ｺ逶､

- `goal` 繧ｭ繝ｼ繝ｯ繝ｼ繝峨°繧牙━蜈郁ｻｸ繧呈ｱｺ繧√ｋ planner 繧定ｿｽ蜉
- 1 hop / 2 hop 縺ｮ candidate 謚ｽ蜃ｺ繧定ｿｽ蜉
- `unknowns` 縺ｨ `confidence` 繝｢繝・Ν繧貞ｰ主・

螳御ｺ・擅莉ｶ:

- `RealWorkspace` 縺ｮ `ShellViewModel` pack 縺ｧ HubObject / Persistence 蛟呵｣懊′諡ｾ縺医ｋ

### Step 3. high-signal sections

- Workflow
- Persistence
- ExternalAsset
- ArchitectureTest
- HubObject

縺ｮ鬆・〒 section builder 繧定ｿｽ蜉縺吶ｋ縲・
螳御ｺ・擅莉ｶ:

- `ShellViewModel` pack 縺ｫ `ProjectDocument`縲ヽepository縲∬ｨｭ螳壹√Ξ繝昴・繝亥・蜉帙′蜈･繧・- `DraftingWindowViewModel` pack 縺ｫ `Workflow -> PortAdapter -> Service -> WallAlgorithm` 縺ｮ荳ｻ骼悶′蜈･繧・
### Step 4. renderer / budget 隱ｿ謨ｴ

- section 縺ｮ蜆ｪ蜈亥ｺｦ蛻ｶ蠕｡
- file / snippet candidate score 縺ｮ隕狗峩縺・- `unknowns` 縺ｨ `confidence` 縺ｮ陦ｨ遉ｺ謾ｹ蝟・
螳御ｺ・擅莉ｶ:

- file 謨ｰ縺ｨ snippet 謨ｰ縺悟｢励∴縺吶℃縺壹￣ack 縺瑚ｪｭ縺ｿ繧・☆縺・
## 繝・せ繝郁ｨ育判

### fixture / unit

- partial symbol 髮・ｴ・- goal planner
- 2 hop expansion
- section builder 縺斐→縺ｮ謚ｽ蜃ｺ邨先棡
- unknowns 逕滓・譚｡莉ｶ

### smoke

蟇ｾ雎｡繝ｯ繝ｼ繧ｯ繧ｹ繝壹・繧ｹ:

- `<modern-real-workspace>`

遒ｺ隱・entry:

- `symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel`
- `symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.Drafting.DraftingWindowViewModel`

遒ｺ隱崎ｦｳ轤ｹ:

- partial 邨ｱ蜷・- workflow chain
- persistence chain
- external asset chain
- architecture test chain
- unknowns 縺ｮ螯･蠖捺ｧ

## 蜿励￠蜈･繧梧擅莉ｶ

- `Rulixa` 縺悟・譁・､懃ｴ｢縺ｮ莉｣繧上ｊ縺ｫ縺ｪ繧句ｿ・ｦ√・縺ｪ縺・- 縺溘□縺励梧怙蛻昴↓隱ｭ繧蝨ｰ蝗ｳ縲阪→縺励※蜈ｨ譁・､懃ｴ｢繧医ｊ譛牙茜縺ｪ蝣ｴ髱｢繧呈・遒ｺ縺ｫ蠅励ｄ縺・- `RealWorkspace` 縺ｧ縲∝・蜿｣縺縺代〒縺ｪ縺丞ｮ溘Θ繝ｼ繧ｹ繧ｱ繝ｼ繧ｹ縺ｮ霈ｪ驛ｭ縺ｾ縺ｧ 1 蝗槭・ pack 縺ｧ謗ｴ繧√ｋ

## backlog

### P1

- generic method / lambda 雜翫＠ helper 縺ｮ霑ｽ霍｡謾ｹ蝟・- score 險ｭ險医・隱ｿ謨ｴ
- `unknowns` 縺ｮ譁・ｨ謾ｹ蝟・
### P2

- `pack --mode map|drilldown` 縺ｮ霑ｽ蜉讀懆ｨ・- section 縺斐→縺ｮ evidence export 蠑ｷ蛹・
### P3

- WPF 莉･螟悶・ plugin 縺ｸ縺ｮ讓ｪ螻暮幕


