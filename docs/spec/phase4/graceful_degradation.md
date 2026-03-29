# Graceful Degradation

## 蜴溷援

- `Rulixa` 縺ｯ unsupported construct 繧堤炊逕ｱ縺ｫ pack 蜈ｨ菴薙ｒ關ｽ縺ｨ縺輔↑縺・- 蜿悶ｌ繧・signal 縺ｯ霑斐＠縲∝叙繧後↑縺・Κ蛻・・ diagnostics 縺ｫ騾√ｋ
- 螟ｱ謨励ｒ髫縺輔↑縺・  縺溘□縺・exception text 繧偵◎縺ｮ縺ｾ縺ｾ隕九○繧九・縺ｧ縺ｯ縺ｪ縺上∝・鬘槭＠縺・failure reason 繧定ｿ斐☆

## degradation 縺ｮ遞ｮ鬘・
- `xaml.parse-degraded`
  - XAML 豁｣隕丞喧蠕後ｂ荳驛ｨ隕∫ｴ繧定ｧ｣驥医〒縺阪↑縺・- `root.resolve-degraded`
  - root candidate 繧堤｢ｺ菫｡縺ｧ縺阪↑縺・- `workflow.route-degraded`
  - command / event / helper 縺ｮ荳区ｵ√′遒ｺ螳壹＠縺ｪ縺・- `viewmodel.resolve-degraded`
  - DataContext 縺ｨ ViewModel 縺ｮ蟇ｾ蠢懊′驛ｨ蛻・噪
- `resource.resolve-degraded`
  - ResourceDictionary / alias 縺瑚ｧ｣豎ｺ縺励″繧後↑縺・
## 陦ｨ迴ｾ譁ｹ驥・
- pack 譛ｬ譁・↓縺ｯ partial 縺ｫ蛻・°縺｣縺・map 繧呈ｮ九☆
- unknowns / diagnostics 縺ｫ縺ｯ
  - 菴輔・蛻・°縺｣縺溘°
  - 縺ｩ縺薙〒 degraded 縺励◆縺・  - 谺｡縺ｫ隕九ｋ symbol / file 縺ｯ菴輔°
  繧貞・繧後ｋ

## 遖∵ｭ｢莠矩・
- parse failure 繧偵◎縺ｮ縺ｾ縺ｾ top-level failure 縺ｫ縺吶ｋ
- unsupported construct 繧・silently ignore 縺吶ｋ
- fallback 縺瑚ｵｰ縺｣縺溽炊逕ｱ繧帝國縺・

