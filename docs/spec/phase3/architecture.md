# Architecture

## 蝓ｺ譛ｬ譁ｹ驥・
`scan -> resolve-entry -> pack` 縺ｮ豬√ｌ縺ｯ邯ｭ謖√☆繧九・Phase 3 縺ｧ縺ｯ `pack` 縺ｮ蜀・Κ縺ｫ `system expansion` 繧定ｿｽ蜉縺励〉oot entry 縺ｮ蝣ｴ蜷医□縺・system-level candidate 繧貞ｺ・￡繧九・
## 霑ｽ蜉縺吶ｋ雋ｬ蜍・
### root candidate resolution

- 隗｣豎ｺ貂医∩ entry 縺・root seed 縺ｨ縺励※謇ｱ縺医ｋ縺九ｒ蛻､螳壹☆繧・- root data context / startup root ViewModel / 譏守､ｺ entry 繧堤ｵｱ荳逧・↓ root seed 縺ｨ縺励※謇ｱ縺・
### system expansion planning

- root seed 縺九ｉ first expansion 蟇ｾ雎｡繧呈ｱｺ繧√ｋ
- 蟇ｾ雎｡ family:
  - workflow
  - hub object
  - major service
  - dialog / window
  - sibling ViewModel

### sub-map aggregation

- expansion 縺ｧ蠕励◆螻謇 map 繧・1 縺､縺ｮ system map 縺ｫ譚溘・繧・- `Shell`, `Drafting`, `Settings`, `3D`, `Report/Export` 縺ｮ繧医≧縺ｪ family 蜊倅ｽ阪〒 sub-map 繧呈紛逅・☆繧・
### section-level compression

- 譌｢蟄・section 繧剃ｽｿ縺｣縺ｦ system-level representative 繧呈ｧ区・縺吶ｋ
- sub-map 縺斐→縺ｮ驥崎､・signal 繧定誠縺ｨ縺励《ection 蜀・〒 canonicalize 縺吶ｋ

### unknown guidance aggregation

- sub-map 蜊倅ｽ阪・ unknown 繧・system-level guidance 縺ｫ邨ｱ蜷医☆繧・- 蛟呵｣懊・ system-level 縺ｧ蜆ｪ蜈磯・ｽ阪▼縺代＠縲∵怙螟ｧ 3 莉ｶ縺ｫ蛻ｶ髯舌☆繧・
## 萓晏ｭ俶婿蜷・
- Domain / Application / Infrastructure / Plugin / CLI 縺ｮ雋ｬ蜍吝・蜑ｲ縺ｯ Phase 1 / 2 繧堤ｶｭ謖√☆繧・- Phase 3 縺ｮ霑ｽ蜉縺ｯ `Rulixa.Plugin.WpfNet8` 縺ｨ renderer 蜻ｨ霎ｺ縺ｫ髢峨§繧・- CLI 繧・evidence schema 縺ｮ螟画峩繧貞燕謠舌↓縺励↑縺・
## 譌｢蟄倥さ繝ｳ繝昴・繝阪Φ繝医・蜀榊茜逕ｨ

- `RelevantPackContext`
- `GoalExpansionProfile`
- `HighSignalSelectionSupport`
- `SectionCompressionSupport`
- 譌｢蟄・section builder 鄒､

Phase 3 縺ｯ縺薙ｌ繧峨・荳翫↓ `system expansion planner` 縺ｨ `sub-map aggregator` 繧定ｿｽ蜉縺吶ｋ險ｭ險医→縺吶ｋ縲・

