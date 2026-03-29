# Implementation Plan

## 螳溯｣・・
1. CI 縺九ｉ local quality gate 縺ｨ蜷後§螳溯｡檎ｵ瑚ｷｯ繧貞他縺ｹ繧九ｈ縺・↓縺吶ｋ
2. run artifact 繧・release gate 蜷代￠縺ｫ髮・ｴ・☆繧・3. handoff quality warning 繧・summary 縺ｨ gate 縺ｫ霑ｽ蜉縺吶ｋ
4. benchmark / telemetry 縺ｮ邯咏ｶ夊ｦｳ貂ｬ繧定ｿｽ蜉縺吶ｋ
5. product readiness 縺ｨ release 蛻､螳壹Ν繝ｼ繝ｫ繧呈磁邯壹☆繧・
## 譛蛻昴・繧ｹ繝ｩ繧､繧ｹ

譛蛻昴↓逹謇九☆繧九・縺ｯ谺｡縺ｮ 3 轤ｹ縺ｫ髯仙ｮ壹☆繧九・
- CI 逕ｨ runner 縺ｮ霑ｽ蜉
- release gate JSON 縺ｮ謨ｴ蛯・- `summary.md` 縺ｮ handoff warning 蠑ｷ蛹・
逅・罰縺ｯ縲￣hase 5 縺ｮ雉・肇繧呈怙繧ょｰ代↑縺・､画峩縺ｧ驕狗畑縺ｫ荵励○繧峨ｌ繧九◆繧√〒縺ゅｋ縲・
## 繝・せ繝域婿驥・
- existing acceptance matrix 繧堤ｶｭ謖√☆繧・- existing quality artifact tests 繧堤ｶｭ謖√☆繧・- CI runner 縺ｯ synthetic corpus 縺ｮ縺ｿ縺ｧ pass/fail 繧貞崋螳壹☆繧・- optional smoke 縺ｯ skip / fail / pass 繧・artifact 縺ｫ谿九☆
- benchmark 蛟､縺ｯ exact match 縺ｧ縺ｯ縺ｪ縺冗ｯ・峇縺ｾ縺溘・ presence 縺ｧ遒ｺ隱阪☆繧・
## 蜿励￠蜈･繧梧擅莉ｶ

- CI 縺ｧ required gate 縺悟ｮ溯｡後〒縺阪ｋ
- release gate 縺・JSON 縺ｨ markdown 縺ｧ遒ｺ隱阪〒縺阪ｋ
- handoff warning 縺・summary 縺ｫ蜃ｺ繧・- benchmark 隕ｳ貂ｬ蛟､縺・artifact 縺ｫ谿九ｋ
- `RealWorkspace` 縺ｨ `LegacyRealWorkspace` 縺瑚ｦｳ貂ｬ蟇ｾ雎｡縺ｨ縺励※邯ｭ謖√＆繧後ｋ

## 谺｡繝輔ぉ繝ｼ繧ｺ縺ｫ騾√ｋ繧ゅ・

- `unknown_guidance_hit_rate` 縺ｮ蜴ｳ蟇・・蜍墓治轤ｹ
- mode 蛻・屬
- deep drilldown
- 3 hop 莉･荳翫・荳闊ｬ謗｢邏｢

Phase 6 縺ｯ product hardening 縺ｮ驕狗畑螳梧・繧剃ｸｻ鬘後↓縺励｝ack 閾ｪ菴薙・讖溯・鬮伜ｺｦ蛹悶・谺｡縺ｸ騾√ｋ縲・

