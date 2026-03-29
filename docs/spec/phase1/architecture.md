# Phase 1 繧｢繝ｼ繧ｭ繝・け繝√Ε

## 菴咲ｽｮ縺･縺・
縺薙・譁・嶌縺ｯ Phase 1 縺ｮ蜈ｷ菴薙い繝ｼ繧ｭ繝・け繝√Ε莉墓ｧ倥〒縺吶・Rulixa 蜈ｨ菴薙・豁｣譛ｬ縺ｧ縺ｯ縺ｪ縺上～WPF + .NET 8` 繧呈判逡･蟇ｾ雎｡縺ｫ縺励◆縺ｨ縺阪・繝励Ο繧ｸ繧ｧ繧ｯ繝亥・蜑ｲ縺ｨ雋ｬ蜍吝・髮｢繧貞ｮ夂ｾｩ縺励∪縺吶・
## 蝓ｺ譛ｬ譁ｹ驥・
- Frontend 縺ｨ Core 繧貞・髮｢縺吶ｋ
- 蜈･蜿｣縺ｮ驛ｽ蜷医ｒ Domain / Application 縺ｫ謖√■霎ｼ縺ｾ縺ｪ縺・- `WPF + .NET 8` 蝗ｺ譛芽ｧ｣譫舌・ Plugin 縺ｫ髢峨§霎ｼ繧√ｋ
- Infrastructure 縺ｫ繝峨Γ繧､繝ｳ繝ｫ繝ｼ繝ｫ繧堤ｽｮ縺九↑縺・
## 螻､讒矩

1. Frontend
2. Plugin / Infrastructure
3. Application
4. Domain

萓晏ｭ俶婿蜷代・螟門・縺九ｉ蜀・・縺縺代↓蜷代￠縺ｾ縺吶・
## 繝励Ο繧ｸ繧ｧ繧ｯ繝亥・蜑ｲ

### `Rulixa.Domain`

- `Entry`
- `ResolvedEntry`
- `Budget`
- `ContextPack`
- `Contract`
- `IndexSection`
- `NavigationTransition`
- Pack 驕ｸ螳壹Ν繝ｼ繝ｫ

遖∵ｭ｢:

- Roslyn 萓晏ｭ・- WPF 萓晏ｭ・- 繝輔ぃ繧､繝ｫ I/O
- CLI 驛ｽ蜷医・蝙・
### `Rulixa.Application`

- `ScanWorkspaceUseCase`
- `ResolveEntryUseCase`
- `BuildContextPackUseCase`
- 繝昴・繝亥ｮ夂ｾｩ

蠖ｹ蜑ｲ:

- Domain 繧剃ｽｿ縺｣縺ｦ繝ｦ繝ｼ繧ｹ繧ｱ繝ｼ繧ｹ繧堤ｵ・∩遶九※繧・- 螟夜Κ I/O 縺ｯ謚ｽ雎｡縺ｫ萓晏ｭ倥☆繧・
### `Rulixa.Plugin.WpfNet8`

- XAML 隗｣譫・- View-ViewModel 謚ｽ蜃ｺ
- NavigationTransition 謚ｽ蜃ｺ
- Command 謚ｽ蜃ｺ
- Dialog 謚ｽ蜃ｺ
- DI 謚ｽ蜃ｺ
- Pack 螂醍ｴ・・邨・∩遶九※
- Scan IR 縺ｮ邨・∩遶九※

蠖ｹ蜑ｲ:

- Phase 1 縺ｮ蜈ｷ菴捺判逡･蟇ｾ雎｡繧・Core 縺九ｉ蛻・屬縺吶ｋ

蜀・Κ讒区・:

- `Extraction/Context`
  Pack 逕滓・譎ゅ↓菴ｿ縺・未騾｣ ViewModel 鄒､繧・ｦ冗ｴ・・繝ｼ繧ｹ隗｣豎ｺ繧偵∪縺ｨ繧√ｋ
- `Extraction/Sections`
  `DI`縲～Navigation`縲～ViewBinding`縲～Command`縲～Dialog` 縺斐→縺ｮ Pack 繧ｻ繧ｯ繧ｷ繝ｧ繝ｳ繧堤ｵ・∩遶九※繧・- `Scanning/Context`
  scan 螳溯｡梧凾縺ｮ繝輔ぃ繧､繝ｫ蜀・ｮｹ縺ｨ `ScanFile` 荳隕ｧ繧偵∪縺ｨ繧√ｋ
- `Scanning/Sections`
  繝ｯ繝ｼ繧ｯ繧ｹ繝壹・繧ｹ蛻玲嫌縲ヾymbol 謚ｽ蜃ｺ縲￣rojectSummary 邨・∩遶九※繧定ｲｬ蜍吶＃縺ｨ縺ｫ蛻・屬縺吶ｋ

險ｭ險域э蝗ｳ:

- `WpfNet8ContractExtractor` 縺ｨ `WpfNet8WorkspaceScanner` 縺ｯ隱ｿ蛛懷ｽｹ縺ｫ逡吶ａ繧・- WPF 蝗ｺ譛峨・謚ｽ蜃ｺ遏･隴倥・ section builder / context builder 縺ｫ髢峨§霎ｼ繧√ｋ
- 蟾ｨ螟ｧ繧ｯ繝ｩ繧ｹ蛹悶ｒ驕ｿ縺代∵ｩ溯・霑ｽ蜉譎ゅ・螟画峩逅・罰縺斐→縺ｫ繝輔ぃ繧､繝ｫ繧貞・縺代ｋ

### `Rulixa.Infrastructure`

- 繝輔ぃ繧､繝ｫ繧ｷ繧ｹ繝・Β
- 繝上ャ繧ｷ繝･
- Markdown renderer
- entry 隗｣豎ｺ陬懷勧

蠖ｹ蜑ｲ:

- Application 縺ｮ繝昴・繝医ｒ螳溯｣・☆繧・
### `Rulixa.Cli`

- `scan`
- `resolve-entry`
- `pack`

蠖ｹ蜑ｲ:

- 繝ｦ繝ｼ繧ｶ繝ｼ蜈･蜉帙ｒ蜿励￠縺ｦ UseCase 繧貞他縺ｶ
- 蜃ｺ蜉帙ｒ謨ｴ蠖｢縺吶ｋ

## 蛻､譁ｭ蝓ｺ貅・
霑ｷ縺｣縺溘ｉ谺｡縺ｧ蛻､譁ｭ縺励∪縺吶・
- 陬ｽ蜩∝・菴薙〒蜀榊茜逕ｨ縺輔ｌ繧九Ν繝ｼ繝ｫ縺ｯ Core
- `WPF + .NET 8` 縺ｫ髢峨§繧倶ｺ句ｮ滓歓蜃ｺ縺ｯ Plugin
- 蜈･蜃ｺ蜉帙∬｡ｨ遉ｺ縲∝ｮ溯｡檎腸蠅・ｾ晏ｭ倥・ Frontend / Infrastructure
- Plugin 蜀・〒繧ゅ《can 縺ｨ pack 縺ｧ螟画峩逅・罰縺碁＆縺・ｂ縺ｮ縺ｯ蛻･繝輔か繝ｫ繝縺ｫ蛻・￠繧・

