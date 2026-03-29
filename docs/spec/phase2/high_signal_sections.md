# High-signal Sections

## 譁ｹ驥・
Phase 2 縺ｧ縺ｯ UI 蜻ｨ霎ｺ section 縺ｫ蜉縺医※縲√す繧ｹ繝・Β逅・ｧ｣縺ｫ逶ｴ邨舌☆繧・section 繧定ｿｽ蜉縺吶ｋ縲・縺薙％縺ｧ縺ｮ逶ｮ逧・・縲後ヵ繧｡繧､繝ｫ謨ｰ繧貞｢励ｄ縺吶阪％縺ｨ縺ｧ縺ｯ縺ｪ縺上√悟ｰ代↑縺・ヨ繝ｼ繧ｯ繝ｳ縺ｧ蝨ｰ蝗ｳ縺ｨ縺励※萓｡蛟､縺碁ｫ倥＞螂醍ｴ・ｒ蠅励ｄ縺吶阪％縺ｨ縺ｫ縺ゅｋ縲・
## 霑ｽ蜉 section

### 1. Workflow

蟇ｾ雎｡:

- Command 螳溯｡悟・
- helper 1 hop
- Application service / UseCase
- Port / Adapter

蜃ｺ蜉・

- `A -> B -> C` 縺ｮ遏ｭ縺・屏譫憺事
- 螳溯｡悟・蜿｣縺ｨ蜑ｯ菴懃畑蠅・阜
- 荳ｻ隕・snippet

### 2. Persistence

蟇ｾ雎｡:

- Repository
- Query / Saver
- `ProjectDocument` 縺ｮ繧医≧縺ｪ荳ｭ蠢・憾諷・- 繝輔ぃ繧､繝ｫ蠖｢蠑上ｄ繧ｨ繝ｳ繝医Μ蜷・
蜃ｺ蜉・

- 隱ｭ縺ｿ霎ｼ縺ｿ蜈・∽ｿ晏ｭ伜・縲∽ｸｻ隕・entry / DTO
- 縲後←縺薙〒迥ｶ諷九′謖√◆繧後ｋ縺九阪・螂醍ｴ・- 豌ｸ邯壼喧蠅・阜繧堤､ｺ縺・snippet

### 3. ExternalAsset

蟇ｾ雎｡:

- Excel master
- report template
- ONNX model
- JSON config
- PDF template / image asset

蜃ｺ蜉・

- 縺ｩ縺ｮ service 縺後←縺ｮ雉・肇繧定ｪｭ繧縺・- fallback 繝ｫ繝ｼ繝ｫ
- 螳溯｡梧凾縺ｫ蠢・ｦ√↑螟夜Κ繝輔ぃ繧､繝ｫ蛟呵｣・
### 4. ArchitectureTest

蟇ｾ雎｡:

- layer guard
- golden test
- regression test

蜃ｺ蜉・

- 繧ｷ繧ｹ繝・Β縺御ｽ輔ｒ螢翫＠縺溘￥縺ｪ縺・°
- 萓晏ｭ俶婿蜷代・譏守､ｺ
- 荳ｻ隕√ユ繧ｹ繝医ヵ繧｡繧､繝ｫ

### 5. HubObject

蟇ｾ雎｡:

- 繧ｷ繧ｹ繝・Β縺ｮ荳ｭ蠢・憾諷・- 隍・焚繝ｦ繝ｼ繧ｹ繧ｱ繝ｼ繧ｹ縺九ｉ蜈ｱ譛峨＆繧後ｋ繧ｪ繝悶ず繧ｧ繧ｯ繝・
萓・

- `ProjectDocument`
- `SettingsDocument`
- `DraftingState`

蜃ｺ蜉・

- 繧ｪ繝悶ず繧ｧ繧ｯ繝医・雋ｬ蜍・- 隱ｰ縺梧峩譁ｰ縺励∬ｪｰ縺瑚ｪｭ繧縺・- dirty state縲《napshot縲（dentity 縺ｮ譛臥┌

## section 蜆ｪ蜈亥ｺｦ

`goal` 縺後す繧ｹ繝・Β逅・ｧ｣蟇・ｊ縺ｪ繧峨￣hase 1 縺ｮ `Dialog` 繧医ｊ `HubObject` 縺ｨ `Persistence` 繧貞━蜈医＠縺ｦ繧医＞縲・`goal` 縺・UI 謫堺ｽ懆ｪｬ譏主ｯ・ｊ縺ｪ繧画里蟄・section 繧貞━蜈医☆繧九・
## file/snippet 驕ｸ螳壹Ν繝ｼ繝ｫ

- Workflow 縺ｯ chain 縺ｮ蜷・hop 蜈ｨ驛ｨ縺ｧ縺ｯ縺ｪ縺上∝・蜿｣繝ｻ荳ｭ邯吶・蠅・阜縺ｮ 3 轤ｹ繧貞━蜈・- Persistence 縺ｯ Query/Saver 縺ｮ蟇ｾ繧貞━蜈・- ExternalAsset 縺ｯ雉・肇縺昴・繧ゅ・縺ｧ縺ｯ縺ｪ縺上∬ｳ・肇繧定ｧ｣豎ｺ縺吶ｋ繧ｳ繝ｼ繝峨ｒ蜆ｪ蜈・- ArchitectureTest 縺ｯ譛繧よ鋸譚溷鴨縺ｮ蠑ｷ縺・ユ繧ｹ繝医ｒ蜆ｪ蜈・- HubObject 縺ｯ螳夂ｾｩ縺ｨ莉｣陦ｨ逧・峩譁ｰ轤ｹ繧貞━蜈・
## RealWorkspace 縺ｧ縺ｮ譛溷ｾ・ｾ・
- Workflow
  `ShellViewModel -> ProjectWorkspaceFlowService -> ProjectWorkspaceService`
- Persistence
  `ProjectDocument <- AsmProjectRepository`
- ExternalAsset
  `ExcelSettingsQuery -> ProductSetting_R*.xlsx`
  `ReportExportService -> ProductReport_*.xlsx`
  `DraftingAiDiagramAnalysisService -> ONNX model`
- ArchitectureTest
  `LayerGuardTests`
- HubObject
  `ProjectDocument`


