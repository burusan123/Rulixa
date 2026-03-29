# Phase 2 繧ｹ繧ｳ繝ｼ繝・
## 逶ｮ逧・
Phase 2 縺ｮ逶ｮ逧・・縲～pack` 繧偵悟・蜿｣縺ｮ隱ｬ譏弱阪°繧峨後す繧ｹ繝・Β逅・ｧ｣縺ｮ縺溘ａ縺ｮ譛遏ｭ繝槭ャ繝励阪∈蠑ｷ蛹悶☆繧九％縺ｨ縺ｫ縺ゅｋ縲・蟇ｾ雎｡縺ｯ縲∝ｰ代↑縺・ヨ繝ｼ繧ｯ繝ｳ縺ｧ LLM 縺梧ｬ｡縺ｮ謗ｨ隲悶ｒ騾ｲ繧√ｉ繧後ｋ縺薙→繧帝㍾隕悶＠縺溯ｨｭ險域隼蝟・〒縺ゅｋ縲・
## 隗｣豎ｺ縺励◆縺・撫鬘・
Phase 1 縺ｮ `pack` 縺ｯ谺｡縺ｮ蝠城｡後ｒ谿九＠縺ｦ縺・ｋ縲・
- 蜈･蜿｣縺九ｉ 1 hop 蜈医∪縺ｧ縺ｯ蠑ｷ縺・′縲∝ｮ溘Θ繝ｼ繧ｹ繧ｱ繝ｼ繧ｹ繧・ｰｸ邯壼喧蠅・阜縺ｾ縺ｧ縺ｮ豬√ｌ縺瑚埋縺・- partial class 縺ｫ蛻・牡縺輔ｌ縺溷梛縺ｮ莉｣陦ｨ繝輔ぃ繧､繝ｫ縺縺代ｒ隕九※蜈ｨ菴薙ｒ隱､隱阪＠繧・☆縺・- goal 縺後後す繧ｹ繝・Β逅・ｧ｣縲阪〒繧・UI 蟇・ｊ section 縺ｫ蛛上ｊ繧・☆縺・- 豺ｱ謗倥ｊ荳崎ｶｳ縺ｧ繧・`unknowns` 縺檎ｩｺ縺ｫ縺ｪ繧翫ｄ縺吶＞
- 縲梧ｬ｡縺ｫ縺ｩ縺薙ｒ謗倥ｋ縺ｹ縺阪°縲阪′ Pack 縺九ｉ蛻・°繧翫↓縺上＞

## Phase 2 縺ｫ蜷ｫ繧√ｋ繧ゅ・

- `goal` 縺ｫ蠢懊§縺ｦ髢｢騾｣繝弱・繝峨ｒ 2 hop 蜑榊ｾ後〒螻暮幕縺吶ｋ莉慕ｵ・∩
- partial 螳｣險繧・1 繧ｷ繝ｳ繝懊Ν縺ｨ縺励※譚溘・繧倶ｻ慕ｵ・∩
- 譁ｰ縺励＞鬮倥す繧ｰ繝翫Ν section builder
  - Workflow
  - Persistence
  - ExternalAsset
  - ArchitectureTest
  - HubObject
- `unknowns` 縺ｨ `confidence` 縺ｮ蜀榊ｮ夂ｾｩ
- `pack` 蜃ｺ蜉帙・蜆ｪ蜈亥ｺｦ莉倥￠謾ｹ蝟・- `RealWorkspace` 繧剃ｽｿ縺｣縺・smoke / fixture / acceptance 縺ｮ譖ｴ譁ｰ

## Phase 2 縺ｫ蜷ｫ繧√↑縺・ｂ縺ｮ

- 豎守畑繧ｳ繝ｼ繝画､懃ｴ｢繧ｨ繝ｳ繧ｸ繝ｳ蛹・- 辟｡蛻ｶ髯仙・蟶ｰ縺ｮ call graph 螻暮幕
- Roslyn 繝吶・繧ｹ縺ｮ螳悟・縺ｪ interprocedural analysis
- LLM 閾ｪ菴薙↓繧医ｋ隕∫ｴ・函謌舌・蝓九ａ霎ｼ縺ｿ
- WPF 莉･螟悶・譁ｰ隕・plugin 蟇ｾ蠢・- 譌｢蟄・`scan` IR 縺ｮ蜈ｨ髱｢蛻ｷ譁ｰ

## 謌仙粥譚｡莉ｶ

- `ShellViewModel` 縺ｮ pack 縺ｧ縲∬ｵｷ蜍慕ｵ瑚ｷｯ縺縺代〒縺ｪ縺・`ProjectDocument`縲ヽepository縲∬ｨｭ螳壹√Ξ繝昴・繝亥・蜉帙∪縺ｧ隕矩壹○繧・- `DraftingWindowViewModel` 縺ｮ pack 縺ｧ縲∝ｰ代↑縺上→繧・`Workflow -> PortAdapter -> Service -> WallAlgorithm` 縺ｮ荳ｻ蝗譫憺事縺悟・繧・- partial class 縺ｮ蜈･蜿｣縺後←縺ｮ `.cs` 縺ｫ resolve 縺輔ｌ縺ｦ繧ゅ∝酔縺伜・菴灘ワ縺悟ｾ励ｉ繧後ｋ
- 譛ｪ隗｣豎ｺ邂・園縺後≠繧九→縺阪・ `unknowns` 縺ｫ譏守､ｺ縺輔ｌ縲∵ｬ｡縺ｫ隱ｭ繧縺ｹ縺榊呵｣懊ヵ繧｡繧､繝ｫ縺悟・繧・- 霑ｽ蜉 section 縺・budget 繧貞､ｧ縺阪￥螢翫＆縺壹￣hase 1 縺ｨ蜷檎ｭ峨・螳溯｡碁溷ｺｦ諢溘ｒ螟ｧ縺阪￥謳阪↑繧上↑縺・
## Phase 2 縺ｮ萓｡蛟､謖・ｨ・
- `pack` 1 蝗槭〒蠕励ｉ繧後ｋ鬮倥す繧ｰ繝翫Ν螂醍ｴ・焚
- `unknowns` 縺ｮ螯･蠖捺ｧ
- `selected files` 縺ｮ邏榊ｾ玲─
- 蜷後§ entry 縺ｫ蟇ｾ縺吶ｋ蜀咲樟諤ｧ
- 蜈ｨ譁・､懃ｴ｢縺ｪ縺励〒繧よｬ｡縺ｮ謗｢邏｢譁ｹ驥昴′遶九▽蜑ｲ蜷・

