# Diagnostics And Acceptance

## diagnostics 縺ｮ逶ｮ逧・
- `Rulixa` 縺ｮ蠑ｱ縺・ｴ謇繧帝國縺輔★遉ｺ縺・- 繝ｦ繝ｼ繧ｶ繝ｼ縺悟・譁・､懃ｴ｢縺ｸ遘ｻ繧九∋縺阪°繧貞愛譁ｭ縺ｧ縺阪ｋ繧医≧縺ｫ縺吶ｋ
- 繧ｵ繝昴・繝域凾縺ｫ縲後←縺薙′譛ｪ蟇ｾ蠢懊°縲阪ｒ蜀咲樟蜿ｯ閭ｽ縺ｫ縺吶ｋ

## diagnostics 縺ｫ蠢・医・隕∫ｴ

- category
  - parse
  - root resolution
  - workflow route
  - viewmodel binding
  - resource resolution
- severity
  - info
  - warning
  - degraded
- known-good signal
  - 菴輔∪縺ｧ縺ｯ蜿悶ｌ縺溘°
- next candidates
  - 谺｡縺ｫ隕九ｋ symbol / file 繧・3 莉ｶ莉･蜀・〒霑斐☆

## acceptance 蟇ｾ雎｡

### real workspace

- `<modern-real-workspace>`
  - modern WPF acceptance
- `<legacy-real-workspace>`
  - legacy WPF acceptance

### synthetic workspace

- code-behind startup
- `new ViewModel()` 逶ｴ邨・- service locator
- dialog-heavy navigation
- ResourceDictionary heavy

## acceptance 譚｡莉ｶ

- modern workspace 縺ｧ縺ｯ Phase 3 逶ｸ蠖薙・ system pack 蜩∬ｳｪ繧堤ｶｭ謖√☆繧・- legacy workspace 縺ｧ縺ｯ crash 縺帙★ partial pack 莉･荳翫ｒ霑斐☆
- `LegacyRealWorkspace` 縺ｮ繧医≧縺ｪ譌ｧ讒区・縺ｧ繧ゅ〉oot縲∽ｸｭ蠢・憾諷九∽ｸｻ隕・workflow 蛟呵｣懊・縺・★繧後°縺悟・繧・- unsupported construct 縺後≠繧句ｴ蜷医〒繧・diagnostics 縺悟ｮ牙ｮ壹＠縺ｦ蜃ｺ繧・
## 蟶りｲｩ繝ｬ繝吶Ν縺ｮ蜩∬ｳｪ譚｡莉ｶ

- 蜷後§蜈･蜉帙〒蜷後§ diagnostics 縺悟・繧・- unsupported workspace 縺ｧ繧ゅし繝昴・繝亥庄閭ｽ縺ｪ隱ｬ譏弱′谿九ｋ
- 螳溘Ρ繝ｼ繧ｯ繧ｹ繝壹・繧ｹ 2 邉ｻ邨ｱ + synthetic fixture 鄒､縺ｧ蝗槫ｸｰ繧貞崋螳壹☆繧・

