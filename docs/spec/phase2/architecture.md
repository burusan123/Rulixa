# Phase 2 繧｢繝ｼ繧ｭ繝・け繝√Ε

## 譁ｹ驥・
Phase 2 縺ｧ縺ｯ `BuildContextPackUseCase` 縺ｮ雋ｬ蜍吶ｒ螢翫＆縺壹～contractExtractor` 縺ｮ蜀・Κ蠑ｷ蛹悶→縺励※螳溯｣・☆繧九・荳ｭ蠢・婿驥昴・縲茎can 貂医∩ IR 繧貞・蛻ｩ逕ｨ縺励▽縺､縲“oal 縺ｨ symbol aggregation 繧剃ｽｿ縺｣縺ｦ鬮倥す繧ｰ繝翫Ν section 繧貞｢励ｄ縺吶阪％縺ｨ縺ｫ縺ゅｋ縲・
## 霑ｽ蜉縺吶ｋ雋ｬ蜍・
### 1. Goal-driven expansion

`resolvedEntry` 繧定ｵｷ轤ｹ縺ｫ縲～goal` 縺ｫ蠢懊§縺ｦ relevant context 繧貞ｺ・￡繧玖ｲｬ蜍吶・Phase 1 縺ｮ `RelevantPackContextFactory` 縺ｯ View / ViewModel / Navigation 縺ｫ蠑ｷ縺・′縲￣hase 2 縺ｧ縺ｯ縺薙ｌ縺ｫ縲悟屏譫憺事蛟呵｣懊阪・蜿朱寔繧定ｿｽ蜉縺吶ｋ縲・
蛟呵｣應ｾ・

- Command 螳溯｡悟・
- private helper 1 hop
- Application service / UseCase
- Repository / Query / Saver
- 險ｭ螳・Query / 繝槭せ繧ｿ隱ｭ霎ｼ
- 螟夜Κ雉・肇蜿ら・
- 繝・せ繝医↓繧医ｋ萓晏ｭ俶婿蜷台ｿ晁ｨｼ

### 2. Symbol aggregation

`partial` 螳｣險繧・1 縺､縺ｮ symbol unit 縺ｨ縺励※謇ｱ縺・ｲｬ蜍吶・scan 譎らせ縺ｾ縺溘・ pack 譎らせ縺ｧ縲∝酔縺・fully-qualified symbol 縺ｫ螻槭☆繧玖､・焚繝輔ぃ繧､繝ｫ繧呈據縺ｭ繧九・
### 3. High-signal sections

WPF 蝗ｺ譛・section 縺ｫ蜉縺医※縲∵ｬ｡縺ｮ section 繧・plugin 蛛ｴ縺ｫ霑ｽ蜉縺吶ｋ縲・
- WorkflowPackSectionBuilder
- PersistencePackSectionBuilder
- ExternalAssetPackSectionBuilder
- ArchitectureTestPackSectionBuilder
- HubObjectPackSectionBuilder

## 謗ｨ螂ｨ讒区・

### Application

- `BuildContextPackUseCase`
  譌｢蟄倥・ orchestrator縲ょ､ｧ縺阪￥縺ｯ螟峨∴縺ｪ縺・
### Plugin.WpfNet8

- `Extraction/Context/RelevantPackContextFactory`
  譌｢蟄俶僑蠑ｵ轤ｹ縲１hase 2 縺ｮ髢｢騾｣蛟呵｣懊ヮ繝ｼ繝牙庶髮・ｒ霑ｽ蜉
- `Extraction/Context/GoalDrivenExpansionPlanner`
  `goal` 縺九ｉ蜆ｪ蜈医ヮ繝ｼ繝臥ｨｮ蛻･繧呈ｱｺ繧√ｋ
- `Extraction/Context/SymbolAggregateResolver`
  partial 螳｣險繧偵∪縺ｨ繧√ｋ
- `Extraction/Sections/*`
  high-signal section 繧定ｿｽ蜉

### Domain

譁ｰ縺励＞隍・尅縺ｪ蝙九・蠅励ｄ縺励☆縺弱↑縺・・縺溘□縺・section 髢薙〒蜈ｱ譛峨☆繧区怙蟆丞腰菴阪→縺励※谺｡繧定ｿｽ蜉縺励※繧医＞縲・
- `SymbolAggregate`
- `ExpansionHint`
- `UnknownItem`
- `PackConfidence`

## 繝・・繧ｿ繝輔Ο繝ｼ

1. `scan` 邨先棡繧貞・蜉・2. `resolvedEntry` 繧貞叙蠕・3. `SymbolAggregateResolver` 縺・relevant symbol 繧堤ｵｱ蜷・4. `GoalDrivenExpansionPlanner` 縺・goal 縺九ｉ蜆ｪ蜈域爾邏｢霆ｸ繧剃ｽ懊ｋ
5. `RelevantPackContextFactory` 縺梧里蟄・relevant context 縺ｫ蝗譫憺事蛟呵｣懊ｒ霑ｽ蜉
6. 蜷・section builder 縺・contracts / index / snippets / files / unknowns 繧定ｿｽ蜉
7. renderer 縺梧怙邨・Markdown 縺ｫ謨ｴ蠖｢

## 萓晏ｭ倬未菫・
- section builder 縺ｯ scan IR 縺ｨ `RelevantPackContext` 縺ｫ萓晏ｭ倥＠縺ｦ繧医＞
- section builder 蜷悟｣ｫ縺ｯ萓晏ｭ倥＠縺ｪ縺・- partial 邨ｱ蜷医Ο繧ｸ繝・け縺ｯ scan 縺ｾ縺溘・ extraction context 縺ｫ髢峨§霎ｼ繧√ｋ
- goal 隗｣譫舌・ renderer 縺ｫ謖√■霎ｼ縺ｾ縺ｪ縺・
## 險ｭ險亥次蜑・
- 讀懃ｴ｢驥上〒縺ｯ縺ｪ縺城ｫ倥す繧ｰ繝翫Ν蜆ｪ蜈亥ｺｦ縺ｧ蜍昴▽
- 1 縺､縺ｮ section 縺悟ｷｨ螟ｧ縺ｪ雋ｬ蜍吶ｒ謖√◆縺ｪ縺・- 蜃ｺ蜉帙〒縺阪↑縺・ｂ縺ｮ縺ｯ `unknowns` 縺ｨ縺励※谿九☆
- budget 蛻ｶ蠕｡縺ｯ譛蠕後〒縺ｯ縺ｪ縺・candidate 逕滓・譎らせ縺ｧ繧よэ隴倥☆繧・

