# Architecture

Phase 5 縺ｧ縺ｯ螟ｧ縺阪↑ public interface 縺ｯ蠅励ｄ縺輔★縲∵里蟄倥・ `scan -> resolve-entry -> pack` 繝代う繝励Λ繧､繝ｳ縺ｮ荳ｭ縺ｫ product hardening 逕ｨ縺ｮ雋ｬ蜍吶ｒ霑ｽ蜉縺吶ｋ縲・
## 雋ｬ蜍吝・蜑ｲ

### 1. Compatibility Layer

- legacy / modern 縺ｮ讒区枚蟾ｮ繧貞精蜿弱☆繧・- unsupported construct 縺ｧ fail-fast 縺帙★ degraded signal 縺ｫ關ｽ縺ｨ縺・- extractors 縺斐→縺ｮ tolerant parsing 繧堤ｵｱ荳繝昴Μ繧ｷ繝ｼ縺ｧ謇ｱ縺・
### 2. Corpus Validation Layer

- synthetic fixture
- real workspace optional smoke
- golden / regression

繧剃ｸ雋ｫ縺励◆ acceptance matrix 縺ｨ縺励※謇ｱ縺・・
### 3. Quality Measurement Layer

- pack success rate
- partial pack rate
- crash-free rate
- unknown guidance hit rate
- false confidence rate
- deterministic rate

繧貞ｮ夂ｾｩ縺励，I繝ｻoptional smoke繝ｻmanual acceptance 縺ｮ縺・★繧後〒貂ｬ繧九°繧貞崋螳壹☆繧九・
### 4. Handoff Quality Layer

- unknown guidance
- diagnostics
- compare-evidence
- representative chain

縺ｮ蜩∬ｳｪ繧偵梧ｬ｡縺ｮ謗｢邏｢縺ｫ縺ｩ縺・ｹ九′繧九°縲阪〒隧穂ｾ｡縺吶ｋ縲・
## 蠅・阜

- scanner / extractor 縺ｯ compatibility 縺ｨ diagnostics 繧呈球縺・- pack builder / renderer 縺ｯ map usefulness 縺ｨ handoff quality 繧呈球縺・- tests / corpus 縺ｯ acceptance 縺ｨ KPI 縺ｮ莠句ｮ滓ｺ舌↓縺ｪ繧・
## 險ｭ險亥次蜑・
- unsupported construct 繧剃ｾ句､悶〒豁｢繧√↑縺・- degraded 縺ｮ逅・罰縺ｯ蠢・★ observable 縺ｫ縺吶ｋ
- modern WPF 縺ｮ蜩∬ｳｪ繧定誠縺ｨ縺輔★縺ｫ legacy 蟇ｾ蠢懊ｒ蜉縺医ｋ
- product quality 縺ｯ螳溯｣・・─隕壹〒縺ｯ縺ｪ縺・corpus 縺ｨ KPI 縺ｧ蛻､譁ｭ縺吶ｋ


