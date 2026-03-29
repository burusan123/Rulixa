# KPI And Quality Gates

## Core KPI

| KPI | 螳夂ｾｩ | 逶ｮ逧・|
|---|---|---|
| `pack_success_rate` | top-level failure 縺ｪ縺励↓ pack 繧定ｿ斐＠縺溷牡蜷・| 蝓ｺ譛ｬ蜩∬ｳｪ |
| `partial_pack_rate` | degraded 縺縺梧怏逕ｨ縺ｪ map 繧定ｿ斐＠縺溷牡蜷・| graceful degradation |
| `crash_free_rate` | 萓句､門●豁｢縺帙★邨ゆｺ・＠縺溷牡蜷・| release gate |
| `first_useful_map_time` | 譛蛻昴・譛臥畑縺ｪ map 縺ｾ縺ｧ縺ｮ譎る俣 | 菴馴ｨ灘刀雉ｪ |
| `unknown_guidance_hit_rate` | unknown candidate 縺九ｉ豁｣縺励＞谺｡謗｢邏｢縺ｫ郢九′縺｣縺溷牡蜷・| handoff quality |
| `false_confidence_rate` | 蛻・°縺｣縺溘・繧翫ｒ縺励◆ pack 縺ｮ蜑ｲ蜷・| 菫｡鬆ｼ諤ｧ |
| `deterministic_rate` | 蜷御ｸ蜈･蜉帙〒蜷御ｸ蜃ｺ蜉帙↓縺ｪ繧句牡蜷・| 蜀咲樟諤ｧ |

## Quality Gates

### 髢狗匱荳ｭ

- 譁ｰ縺励＞ compatibility 蟇ｾ蠢懊・ regression fixture 繧剃ｼｴ縺・- `dotnet test .\Rulixa.sln` 縺碁壹ｋ
- 蟇ｾ雎｡ workspace 縺ｮ optional smoke 縺碁壹ｋ

### Phase 5 螳御ｺ・愛螳・
- `RealWorkspace` 縺ｨ `LegacyRealWorkspace` 縺ｮ荳｡譁ｹ縺ｧ crash-free
- synthetic corpus 縺ｮ荳ｻ隕√ヱ繧ｿ繝ｼ繝ｳ縺ｧ partial pack 莉･荳翫ｒ霑斐☆
- false confidence 縺ｮ譁ｰ隕乗が蛹悶′縺ｪ縺・- compare-evidence 縺ｧ謾ｹ蝟・せ縺瑚ｪｬ譏主庄閭ｽ

## 貂ｬ螳夐°逕ｨ

- success / crash-free / deterministic 縺ｯ CI 蜷代￠
- partial / unknown guidance / false confidence 縺ｯ manual acceptance 縺ｨ golden review 蜷代￠
- first useful map time 縺ｯ benchmark 縺ｧ霑ｽ霍｡縺吶ｋ

## 謾ｹ蝟・愛螳壹・蝓ｺ貅・
- 莉ｶ謨ｰ蠅怜刈縺ｯ謾ｹ蝟・擅莉ｶ縺ｫ縺励↑縺・- representative chain 縺梧・遒ｺ縺ｫ縺ｪ繧九％縺ｨ
- unknown candidate 縺悟ｦ･蠖薙↓縺ｪ繧九％縺ｨ
- degraded reason 縺瑚ｪｬ譏主庄閭ｽ縺ｫ縺ｪ繧九％縺ｨ

繧呈隼蝟・→縺ｿ縺ｪ縺吶・

