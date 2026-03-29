# Phase 2

縺薙・繝輔か繝ｫ繝縺ｯ `Rulixa` 縺ｮ Phase 2 莉墓ｧ倥ｒ縺ｾ縺ｨ繧√ｋ縲・Phase 2 縺ｮ逶ｮ逧・・縲￣hase 1 縺ｮ縲悟・蜿｣繧堤ｴ譌ｩ縺乗雫繧縲榊ｼｷ縺ｿ繧堤ｶｭ謖√＠縺溘∪縺ｾ縲∝・譁・､懃ｴ｢縺ｫ雋縺代※縺・◆縲悟ｮ滄圀縺ｮ莉穂ｺ九・豬√ｌ縺ｮ蝨ｧ邵ｮ縲阪ｒ蠑ｷ蛹悶☆繧九％縺ｨ縺ｫ縺ゅｋ縲・
Phase 2 縺ｧ縺ｯ `pack` 繧貞腰縺ｪ繧句・蜿｣隕∫ｴ・〒縺ｯ縺ｪ縺上～goal` 縺ｫ蠢懊§縺ｦ驥崎ｦ√↑蝗譫憺事繧・2 hop 蜑榊ｾ後〒霎ｿ繧矩ｫ伜ｯ・ｺｦ繝槭ャ繝励∈騾ｲ蛹悶＆縺帙ｋ縲・縺薙％縺ｧ逶ｮ謖・☆縺ｮ縺ｯ縲梧､懃ｴ｢驥上・蠅怜刈縲阪〒縺ｯ縺ｪ縺上悟ｰ代↑縺・さ繝ｳ繝・く繧ｹ繝医〒驥崎ｦ∽ｺ句ｮ溘∈蛻ｰ驕斐〒縺阪ｋ蝨ｧ邵ｮ蜩∬ｳｪ縲阪〒縺ゅｋ縲・
## Phase 2 縺ｮ荳ｻ隕√ユ繝ｼ繝・
- `goal` 鬧・虚縺ｮ螟壽ｮｵ螻暮幕
- partial class / partial record 縺ｮ邨ｱ蜷・- 鬮倥す繧ｰ繝翫Ν section 縺ｮ霑ｽ蜉
- `unknowns` 縺ｨ `confidence` 縺ｮ蜴ｳ蟇・喧
- 縲梧ｬ｡縺ｫ隱ｭ繧縺ｹ縺咲ｮ・園縲阪ｒ霑斐○繧・Pack

## 繝峨く繝･繝｡繝ｳ繝井ｸ隕ｧ

- [scope.md](scope.md)
  Phase 2 縺ｮ蟇ｾ雎｡縲・撼蟇ｾ雎｡縲∵・蜉滓擅莉ｶ
- [architecture.md](architecture.md)
  Phase 2 縺ｮ雋ｬ蜍吝・蜑ｲ縺ｨ霑ｽ蜉繧ｳ繝ｳ繝昴・繝阪Φ繝・- [goal_driven_expansion.md](goal_driven_expansion.md)
  `goal` 繧剃ｽｿ縺｣縺・2 hop 螻暮幕縺ｨ unknowns / confidence 縺ｮ莉墓ｧ・- [partial_symbol_unification.md](partial_symbol_unification.md)
  partial 螳｣險繧・1 縺､縺ｮ繧ｷ繝ｳ繝懊Ν縺ｨ縺励※謇ｱ縺・ｻ墓ｧ・- [high_signal_sections.md](high_signal_sections.md)
  Workflow / Persistence / ExternalAsset / ArchitectureTest / HubObject 謚ｽ蜃ｺ
- [implementation_plan.md](implementation_plan.md)
  螳溯｣・・∵､懆ｨｼ縲∵ｮｵ髫弱Μ繝ｪ繝ｼ繧ｹ譯・
## 隱ｭ縺ｿ鬆・
1. [scope.md](scope.md)
2. [architecture.md](architecture.md)
3. [goal_driven_expansion.md](goal_driven_expansion.md)
4. [partial_symbol_unification.md](partial_symbol_unification.md)
5. [high_signal_sections.md](high_signal_sections.md)
6. [implementation_plan.md](implementation_plan.md)

## 閭梧勹

`RealWorkspace` 繧帝｡梧攝縺ｫ縺励◆豈碑ｼ・〒縺ｯ縲￣hase 1 縺ｮ `pack` 縺ｯ谺｡縺ｮ轤ｹ縺ｧ蠑ｷ縺九▲縺溘・
- `ShellViewModel` 繧・`DraftingWindowViewModel` 縺ｮ蜈･蜿｣迚ｹ螳壹′騾溘＞
- WPF 縺ｮ root binding / navigation / DI 縺ｮ隕∫ｴ・′騾溘＞

荳譁ｹ縺ｧ谺｡縺ｮ轤ｹ縺ｧ縺ｯ蜈ｨ譁・､懃ｴ｢縺ｫ雋縺代◆縲・
- 蜈･蜿｣縺ｮ蜈医↓縺ゅｋ Application / Infrastructure / Domain 縺ｮ蝗譫憺事繧貞香蛻・↓霎ｿ繧後↑縺・- partial class 蛻・牡縺輔ｌ縺溷ｷｨ螟ｧ ViewModel 縺ｮ蜈ｨ菴灘ワ繧貞悸邵ｮ縺ｧ縺阪↑縺・- 螳滄圀縺ｫ縺ｯ譛ｪ螻暮幕縺ｪ縺ｮ縺ｫ `unknowns` 縺檎ｩｺ縺ｫ縺ｪ繧・- 險ｭ螳壹∵ｰｸ邯壼喧縲∝､夜Κ雉・肇縲√い繝ｼ繧ｭ繝・け繝√Ε繝・せ繝医・繧医≧縺ｪ鬮倥す繧ｰ繝翫Ν諠・ｱ繧定誠縺ｨ縺・
Phase 2 縺ｯ縺薙・蟾ｮ蛻・ｒ蝓九ａ繧九・

