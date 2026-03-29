# Architecture

## 讎りｦ・
Phase 4 縺ｧ縺ｯ `scan -> resolve-entry -> pack` 縺ｮ螟門ｽ｢縺ｯ邯ｭ謖√＠縲∝・驛ｨ縺ｫ `Compatibility Layer` 繧定ｿｽ蜉縺吶ｋ縲・
## 荳ｻ縺ｪ雋ｬ蜍・
- `XamlCompatibilityNormalizer`
  - namespace alias縲～x:Key`縲ヽesourceDictionary縲〕egacy markup 繧呈ｭ｣隕丞喧縺吶ｋ
- `LegacyRootResolver`
  - modern DI 蜑肴署縺ｫ萓晏ｭ倥○縺壹〕egacy 襍ｷ蜍慕ｵ瑚ｷｯ縺九ｉ root candidate 繧呈耳螳壹☆繧・- `FallbackRouteResolver`
  - constructor DI 縺縺代〒縺ｪ縺・manual `new`縲《ervice locator縲‘vent handler 繧・route 蛟呵｣懊→縺励※謇ｱ縺・- `PartialPackAssembler`
  - 騾比ｸｭ螟ｱ謨励′縺ゅ▲縺ｦ繧よｮ九○繧・signal 縺縺代〒 pack 繧呈ｧ狗ｯ峨☆繧・- `CompatibilityDiagnosticsBuilder`
  - failure reason縲‥egraded reason縲∵ｬ｡縺ｫ隕九ｋ蛟呵｣懊ｒ deterministic 縺ｫ霑斐☆

## 險ｭ險域婿驥・
- 譌｢蟄倥・謚ｽ蜃ｺ邨瑚ｷｯ繧呈昏縺ｦ縺ｪ縺・- modern path 縺ｯ縺昴・縺ｾ縺ｾ谿九＠縲〕egacy path 繧・fallback 縺ｨ縺励※雜ｳ縺・- fallback 縺瑚ｵｰ縺｣縺ｦ繧・`ContextPack` shape 縺ｯ螟峨∴縺ｪ縺・- unsupported construct 縺ｯ exception 縺ｫ縺励↑縺・  - classify
  - degrade
  - diagnose

## 萓晏ｭ倬未菫・
- normalizer 縺ｯ scan 縺ｮ逶ｴ蠕・- root resolver 縺ｯ resolve-entry 縺ｨ pack 縺ｮ蜑肴ｮｵ
- fallback route resolver 縺ｯ existing planner 縺ｮ陬懷勧縺ｨ縺励※蜍輔￥
- diagnostics builder 縺ｯ renderer 縺ｮ逶ｴ蜑阪〒髮・ｴ・☆繧・

