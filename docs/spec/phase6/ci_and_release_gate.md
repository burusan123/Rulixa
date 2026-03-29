# CI and Release Gate

## 逶ｮ逧・
local quality gate 繧偵碁幕逋ｺ閠・′謇句・縺ｧ蝗槭○繧九咲憾諷九°繧峨√檎ｶ咏ｶ夂噪縺ｫ蝗槭ｊ縲〉elease 蛻､螳壹↓菴ｿ縺医ｋ縲咲憾諷九∈謖√■荳翫￡繧九・
## 螳溯｡悟玄蛻・
### Required Gate
- synthetic corpus
- deterministic regression
- weak-signal corpus

縺薙・蛹ｺ蛻・・ CI 縺ｧ蟶ｸ譎ょｮ溯｡後＠縲｝ass/fail 繧・release gate 縺ｫ逶ｴ謗･菴ｿ縺・・
### Observed Only
- modern real workspace smoke
- legacy real workspace smoke
- large workspace benchmark

縺薙・蛹ｺ蛻・・ artifact 縺ｫ縺ｯ谿九☆縺後’ail 縺励※繧ら峩縺｡縺ｫ release fail 縺ｫ縺ｯ縺励↑縺・・ 
縺溘□縺・warning 縺ｨ縺励※ summary 縺ｫ蠑ｷ縺剰｡ｨ遉ｺ縺吶ｋ縲・
## Gate 譚｡莉ｶ

- `crash_free_rate = 100%`
- `pack_success_rate = 100%` on required root cases
- `deterministic_rate = 100%`
- `false_confidence_rate = 0%`

## Advisory 謖・ｨ・
- `partial_pack_rate`
- `first_useful_map_time_ms`
- `unknown_guidance_case_count`
- `unknown_guidance_family_count`
- `degraded_reason_count`

縺薙ｌ繧峨・ release gate 縺ｮ蜿り・､縺ｨ縺励※險倬鹸縺励∝叉 fail 譚｡莉ｶ縺ｫ縺ｯ縺励↑縺・・
## Release 蛻､螳・
release 蜿ｯ蜷ｦ縺ｯ谺｡縺ｮ鬆・〒豎ｺ繧√ｋ縲・
1. required gate 縺・pass
2. benchmark 縺ｫ驥榊､ｧ騾陦後′縺ｪ縺・3. handoff quality 縺ｫ blocking warning 縺後↑縺・4. optional smoke 縺ｮ fail 縺梧里遏･萓句､悶→縺励※謨ｴ逅・ｸ医∩

## 逕滓・迚ｩ

- `kpi.json`
- `gate.json`
- `summary.md`
- benchmark 豈碑ｼ・畑 artifact
- CI 螳溯｡後Ο繧ｰ縺ｸ縺ｮ繝ｪ繝ｳ繧ｯ


