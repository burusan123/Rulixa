# Architecture

Phase 6 縺ｧ縺ｯ縲∵里蟄倥・ `pack` 邉ｻ螳溯｣・↓螟ｧ縺阪↑雋ｬ蜍呵ｿｽ蜉縺ｯ陦後ｏ縺壹〈uality 驕狗畑螻､繧呈僑蠑ｵ縺吶ｋ縲・
## 雋ｬ蜍吝・蜑ｲ

### 1. Pack / Scan 螳溯｡悟ｱ､
- workspace 繧・scan 縺吶ｋ
- entry 繧・resolve 縺吶ｋ
- pack 繧堤函謌舌☆繧・- diagnostics / unknown guidance / representative chain 繧定ｿ斐☆

縺薙・螻､縺ｮ雋ｬ蜍吶・ Phase 5 縺ｨ蜷後§縺ｧ縲￣hase 6 縺ｧ縺ｯ蜴溷援縺ｨ縺励※蜀榊茜逕ｨ縺吶ｋ縲・
### 2. Quality Artifact 螻､
- case 蜊倅ｽ・artifact 繧堤函謌舌☆繧・- run 蜊倅ｽ・artifact 繧帝寔邏・☆繧・- benchmark 縺ｨ handoff observation 繧貞刈邂励☆繧・- CI / 繝ｭ繝ｼ繧ｫ繝ｫ縺ｮ荳｡譁ｹ縺ｧ蜷後§ schema 繧剃ｽｿ縺・
### 3. Gate Evaluation 螻､
- required corpus 縺ｫ蟇ｾ縺励※ success / crash-free / deterministic / false-confidence 繧貞愛螳壹☆繧・- handoff quality 繧・warning 縺ｾ縺溘・ advisory 縺ｨ縺励※謇ｱ縺・- release gate 縺ｮ pass/fail 繧・JSON 縺ｨ markdown 縺ｮ荳｡譁ｹ縺ｧ蜃ｺ縺・
### 4. Benchmark / Telemetry 螻､
- `first useful map time` 繧・run 縺斐→縺ｫ險倬鹸縺吶ｋ
- representative chain 謨ｰ縲「nknown guidance 謨ｰ縲‥egraded reason 謨ｰ繧帝寔險医☆繧・- 騾陦梧､懃衍縺ｫ菴ｿ縺医ｋ豈碑ｼ・腰菴阪ｒ菴懊ｋ

### 5. CI / Release Integration 螻､
- local runner 縺ｨ蜷後§ quality 螳溯｡後ｒ CI 縺九ｉ蜻ｼ縺ｶ
- artifact 繧剃ｿ晏ｭ倥☆繧・- gate 縺ｫ螟ｱ謨励＠縺・run 繧・release 荳榊庄縺ｨ縺励※謇ｱ縺・
## 萓晏ｭ俶婿蜷・
- `pack` 螳溯｡悟ｱ､縺ｯ quality / CI 繧堤衍繧峨↑縺・- quality artifact 螻､縺ｯ `pack` 邨先棡繧定ｪｭ繧縺後・・ｾ晏ｭ倥・菴懊ｉ縺ｪ縺・- CI / release 螻､縺ｯ quality artifact 繧定ｪｭ繧縺後～pack` 譛ｬ菴薙・繝ｭ繧ｸ繝・け繧呈戟縺溘↑縺・
縺薙・蛻・屬縺ｫ繧医ｊ縲｝ack 縺ｮ蜩∬ｳｪ蜷台ｸ翫→ release 驕狗畑繧堤峡遶九↓騾ｲ繧√ｉ繧後ｋ繧医≧縺ｫ縺吶ｋ縲・

