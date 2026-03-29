# Partial Symbol Unification

## 閭梧勹

WPF 讌ｭ蜍吶い繝励Μ縺ｧ縺ｯ蟾ｨ螟ｧ ViewModel 繧・code-behind 縺・partial class 縺ｫ蛻・牡縺輔ｌ繧九％縺ｨ縺悟､壹＞縲・Phase 1 縺ｯ entry 隗｣豎ｺ譎ゅ↓蜊倅ｸ繝輔ぃ繧､繝ｫ縺ｸ蛛上ｋ縺薙→縺後≠繧翫～DraftingWindowViewModel.Arc.cs` 縺ｮ繧医≧縺ｪ荳驛ｨ繝輔ぃ繧､繝ｫ縺縺代°繧牙・菴灘ワ繧定ｪ､隱阪＠繧・☆縺九▲縺溘・
## 逶ｮ逧・
蜷後§ fully-qualified symbol 縺ｫ螻槭☆繧・partial 螳｣險繧・1 縺､縺ｮ隲也炊繧ｷ繝ｳ繝懊Ν縺ｨ縺励※謇ｱ縺・・
## 蟇ｾ雎｡

- `partial class`
- `partial record`
- `partial struct`

Phase 2 縺ｧ縺ｯ縺ｾ縺・C# 縺ｮ縺ｿ繧貞ｯｾ雎｡縺ｫ縺吶ｋ縲・
## 莉墓ｧ・
### symbol aggregate

1 縺､縺ｮ symbol 縺ｫ蟇ｾ縺励※谺｡繧呈據縺ｭ繧九・
- 蜿ょ刈繝輔ぃ繧､繝ｫ荳隕ｧ
- constructor 荳隕ｧ
- command 螳夂ｾｩ蛟呵｣・- public / internal API
- event subscription
- high-signal method

### entry resolve

- `resolve-entry` 縺ｮ `resolvedPath` 縺ｯ莉｣陦ｨ繝輔ぃ繧､繝ｫ繧定ｿ斐＠縺ｦ繧医＞
- 縺溘□縺・`pack` 縺ｧ縺ｯ莉｣陦ｨ繝輔ぃ繧､繝ｫ縺縺代〒縺ｪ縺・symbol aggregate 蜈ｨ菴薙ｒ菴ｿ縺・- 蜃ｺ蜉帙↓縺ｯ縲継artial 邨ｱ蜷亥ｯｾ雎｡繝輔ぃ繧､繝ｫ謨ｰ縲阪ｒ霈峨○縺ｦ繧医＞

### snippet 驕ｸ螳・
partial symbol 縺九ｉ snippet 繧帝∈縺ｶ縺ｨ縺阪・谺｡繧貞━蜈医☆繧九・
1. constructor
2. command 螳溯｡檎せ
3. 迥ｶ諷区峩譁ｰ縺ｮ荳ｭ譬ｸ繝｡繧ｽ繝・ラ
4. 螟夜Κ service / repository 蜻ｼ縺ｳ蜃ｺ縺・
迚ｹ螳壹ヵ繧｡繧､繝ｫ縺縺代↓蟇・▲縺溯ｦ∫ｴ・・遖∵ｭ｢縺励↑縺・′縲￣ack 縺ｧ縺ｯ aggregate 縺ｮ隕ｳ轤ｹ繧貞､ｱ繧上↑縺・％縺ｨ縲・
### file selection

partial 蜿ょ刈繝輔ぃ繧､繝ｫ縺ｯ縺吶∋縺ｦ蠢・医↓縺励↑縺・・signal score 縺ｫ蝓ｺ縺･縺・1縲・ 繝輔ぃ繧､繝ｫ遞句ｺｦ繧呈治逕ｨ縺励∵ｮ九ｊ縺ｯ index 繧・･醍ｴ・↓蜿肴丐縺吶ｋ縲・
## unknowns 縺ｨ縺ｮ髢｢菫・
partial symbol 縺ｮ荳驛ｨ縺励° scan 縺ｧ縺阪※縺・↑縺・√∪縺溘・蜻ｽ蜷崎｡晉ｪ√〒邨ｱ蜷医〒縺阪↑縺・ｴ蜷医・ `unknowns` 縺ｫ蜃ｺ縺吶・
## 譛溷ｾ・柑譫・
- `DraftingWindowViewModel` 縺ｮ pack 縺・`.Arc.cs` 縺縺代↓蛛上ｉ縺ｪ縺・- constructor dependency 縺ｨ command 螳夂ｾｩ縺檎ｵｱ蜷医＆繧後ｋ
- 蟾ｨ螟ｧ ViewModel 縺ｮ縲後←縺ｮ雋ｬ蜍吶′縺ｩ縺ｮ partial 縺ｫ縺・ｋ縺九阪′隕九∴繧・

