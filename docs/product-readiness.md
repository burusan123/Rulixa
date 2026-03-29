# Rulixa Product Readiness

## 逶ｮ逧・
縺薙・譁・嶌縺ｯ縲～Rulixa` 繧偵瑚ｩｦ菴懊阪°繧峨悟ｸりｲｩ縺ｧ縺阪ｋ繝ｬ繝吶Ν縲阪∈蠑輔″荳翫￡繧九◆繧√・蜃ｺ闕ｷ蛻､螳壹メ繧ｧ繝・け繝ｪ繧ｹ繝医〒縺ゅｋ縲・ 
Phase 1縲・ 縺ｮ莉墓ｧ俶嶌縺ｯ讖溯・騾ｲ蛹悶ｒ謇ｱ縺・′縲∵悽譖ｸ縺ｯ **陬ｽ蜩√→縺励※菴輔′謠・▲縺ｦ縺・ｌ縺ｰ繧医＞縺・* 繧堤｢ｺ隱阪☆繧九◆繧√↓菴ｿ縺・・
## 蜑肴署

- `Rulixa` 縺ｮ荳ｻ萓｡蛟､縺ｯ縲悟・譁・､懃ｴ｢縺ｮ莉｣譖ｿ縲阪〒縺ｯ縺ｪ縺・- 荳ｻ萓｡蛟､縺ｯ縲鍬LM 縺檎洒縺・さ繝ｳ繝・く繧ｹ繝医〒逅・ｧ｣繧貞ｧ九ａ繧峨ｌ繧矩ｫ伜ｯ・ｺｦ縺ｪ蝨ｰ蝗ｳ繧定ｿ斐☆縺薙→縲・- 縺昴・縺溘ａ縲∝・闕ｷ蛻､螳壹〒縺ｯ雉｢縺輔□縺代〒縺ｪ縺・  - 莠呈鋤諤ｧ
  - 螳牙ｮ壽ｧ
  - 險ｺ譁ｭ諤ｧ
  - 蜀咲樟諤ｧ
  - 繧ｵ繝昴・繝亥庄閭ｽ諤ｧ
  繧貞酔遲峨↓謇ｱ縺・
## 蜃ｺ闕ｷ蛻､螳壹・蜴溷援

- `modern WPF 縺ｧ蠑ｷ縺Я 縺縺代〒縺ｯ荳榊庄
- `legacy WPF 縺ｧ繧り誠縺｡縺ｪ縺Я 縺薙→縺悟ｿ・ｦ・- 譛ｪ蟇ｾ蠢懈ｧ区・縺ｧ繧・crash 縺帙★縲｝artial pack 縺ｨ diagnostics 繧定ｿ斐○繧九％縺ｨ
- 蜷後§蜈･蜉帙〒蜷後§ pack 縺ｨ diagnostics 縺悟・繧九％縺ｨ
- 縲悟・縺九ｉ縺ｪ縺・・縺ｫ蛻・°縺｣縺溘・繧翫阪ｒ縺励↑縺・％縺ｨ

## KPI

| 謖・ｨ・| 逶ｮ逧・| 譛菴取擅莉ｶ | 險ｼ霍｡ |
|---|---|---|---|
| `pack success rate` | pack 縺瑚ｿ斐ｋ邇・| modern / legacy corpus 縺ｮ荳｡譁ｹ縺ｧ邯咏ｶ夊ｨ域ｸｬ縺輔ｌ縺ｦ縺・ｋ | smoke / regression 繝ｬ繝昴・繝・|
| `partial pack rate` | degraded 縺ｧ繧よ怏逕ｨ縺ｪ蜃ｺ蜉帙′霑斐ｋ邇・| failure 縺ｨ蛹ｺ蛻･縺励※險域ｸｬ縺輔ｌ縺ｦ縺・ｋ | diagnostics / evidence |
| `crash-free rate` | 萓句､悶〒關ｽ縺｡縺ｪ縺・紫 | release gate 縺ｫ蜷ｫ縺ｾ繧後※縺・ｋ | CI / optional smoke |
| `first useful map time` | 蛻晏虚逅・ｧ｣縺ｮ騾溘＆ | 螟ｧ隕乗ｨ｡ workspace 縺ｧ繧りｨｱ螳ｹ譎る俣蜀・| benchmark |
| `unknown guidance hit rate` | handoff 蜩∬ｳｪ | unknown 縺九ｉ蜈ｨ譁・､懃ｴ｢縺ｧ豁｣遲斐↓螻翫￥縺九ｒ讀懆ｨｼ | acceptance |
| `false confidence rate` | 蛻・°縺｣縺溘・繧翫・謚大宛 | 菴弱＞縺薙→繧堤｢ｺ隱阪☆繧・| golden review |
| `deterministic rate` | 蜀咲樟諤ｧ | 蜷後§蜈･蜉帙〒蜷後§邨先棡縺悟・繧・| regression |

## 繝√ぉ繝・け繝ｪ繧ｹ繝・
### 1. 謚陦・
- [ ] modern WPF + DI 讒区・縺ｧ system pack 縺悟ｮ牙ｮ壹＠縺ｦ霑斐ｋ
- [ ] legacy WPF + code-behind 讒区・縺ｧ crash 縺励↑縺・- [ ] `App.xaml StartupUri` 繧・root 隗｣豎ｺ縺ｫ菴ｿ縺医ｋ
- [ ] `DataContext = new XxxViewModel()` 繧・root / binding 隗｣豎ｺ縺ｫ菴ｿ縺医ｋ
- [ ] service locator / static resolver 繧・limited support 縺ｧ縺阪ｋ
- [ ] `new Window()` / `ShowDialog()` 繧・route 蛟呵｣懊→縺励※諡ｾ縺医ｋ
- [ ] ResourceDictionary / merged dictionaries 縺ｧ parse failure 縺ｫ縺ｪ繧翫↓縺上＞
- [ ] partial class / partial record 縺ｮ邨ｱ蜷医′螳牙ｮ壹＠縺ｦ縺・ｋ
- [ ] unsupported construct 縺ｧ top-level failure 縺ｧ縺ｯ縺ｪ縺・degraded pack 繧定ｿ斐○繧・- [ ] diagnostics 縺・failure reason 縺ｨ next candidates 繧定ｿ斐○繧・- [ ] false confidence 繧呈椛蛻ｶ縺ｧ縺阪※縺・ｋ
- [ ] compare-evidence 縺ｧ謾ｹ蝟・・螳ｹ繧定ｿｽ縺医ｋ

### 2. 蜩∬ｳｪ菫晁ｨｼ

- [ ] modern corpus 縺後≠繧・- [ ] legacy corpus 縺後≠繧・- [ ] synthetic fixture 縺・root / dialog / service locator / code-behind 繧貞性繧
- [ ] `RealWorkspace` 縺ｮ acceptance 縺後≠繧・- [ ] `LegacyRealWorkspace` 縺ｮ acceptance 縺後≠繧・- [ ] pack 譛ｬ譁・・ golden test 縺後≠繧・- [ ] diagnostics 縺ｮ golden test 縺後≠繧・- [ ] decision trace 縺ｮ regression test 縺後≠繧・- [ ] deterministic regression 縺後≠繧・- [ ] failure taxonomy 縺悟崋螳壹＆繧後※縺・ｋ
- [ ] release 縺斐→縺ｫ KPI 繧定ｨ倬鹸縺ｧ縺阪ｋ

### 3. UX

- [ ] plugin 隱ｬ譏弱′譌･譛ｬ隱槭〒荳雋ｫ縺励※縺・ｋ
- [ ] `pack -> 蠢・ｦ∵凾縺ｮ縺ｿ蜈ｨ譁・､懃ｴ｢` 縺ｮ菴ｿ縺・・縺代′譏守｢ｺ
- [ ] `entry=file` / `entry=symbol` 縺ｮ驕ｸ縺ｳ譁ｹ縺瑚ｪｬ譏弱＆繧後※縺・ｋ
- [ ] root entry 縺ｧ縺ｯ system map 縺悟・縺ｫ隱ｭ繧√ｋ
- [ ] unknowns 繧偵梧ｬ｡縺ｮ謗｢邏｢繧ｬ繧､繝峨阪→縺励※隱ｭ繧√ｋ
- [ ] diagnostics 縺・exception text 縺ｧ縺ｯ縺ｪ縺剰ｪｬ譏取枚縺ｫ縺ｪ縺｣縺ｦ縺・ｋ
- [ ] 蛻晏屓繝ｦ繝ｼ繧ｶ繝ｼ縺・`ShellViewModel` 繧・main screen 繧定ｵｷ轤ｹ縺ｫ驕ｸ縺ｳ繧・☆縺・- [ ] 謌仙粥譎ゅ・譛溷ｾ・､縺梧ｭ｣縺励￥莨昴ｏ繧・  - 縲悟・螳溯｣・ｒ隱ｬ譏弱☆繧九阪〒縺ｯ縺ｪ縺上碁ｫ伜ｯ・ｺｦ縺ｪ蝨ｰ蝗ｳ繧定ｿ斐☆縲・
### 4. 繧ｵ繝昴・繝磯°逕ｨ

- [ ] issue template 縺後≠繧・- [ ] 蜀咲樟縺ｫ蠢・ｦ√↑蜈･蜉帙′螳夂ｾｩ縺輔ｌ縺ｦ縺・ｋ
  - workspace
  - entry
  - goal
  - diagnostics
  - evidence bundle
- [ ] diagnostics 縺九ｉ蜀咲樟隱ｿ譟ｻ繧貞ｧ九ａ繧峨ｌ繧・- [ ] degraded pack 繧偵し繝昴・繝域凾縺ｫ隱ｬ譏弱〒縺阪ｋ
- [ ] evidence bundle 縺ｮ菫晏・譁ｹ驥昴′縺ゅｋ
- [ ] 蟇ｾ蠢懈ｸ医∩ / 驛ｨ蛻・ｯｾ蠢・/ 譛ｪ蟇ｾ蠢懊・莠呈鋤諤ｧ陦ｨ縺後≠繧・- [ ] release note 縺ｧ莠呈鋤諤ｧ謾ｹ蝟・ｒ霑ｽ縺医ｋ

## 繝輔ぉ繝ｼ繧ｺ縺ｨ縺ｮ髢｢菫・
- Phase 1
  蝓ｺ逶､縺ｨ evidence
- Phase 2
  鬮倥す繧ｰ繝翫Ν謚ｽ蜃ｺ
- Phase 3
  system pack
- Phase 4
  legacy WPF compatibility

譛ｬ譖ｸ縺ｯ Phase 4 縺ｮ谺｡縺ｫ蜿ら・縺吶ｋ繧ゅ・縺ｧ縺ｯ縺ｪ縺上￣hase 4 繧貞ｮ溯｣・＠縺ｪ縺後ｉ邯咏ｶ夂噪縺ｫ譖ｴ譁ｰ縺吶ｋ縲・
## 蜆ｪ蜈磯・ｽ・
1. crash-free
2. legacy compatibility
3. diagnostics quality
4. acceptance corpus
5. UX / supportability
6. deeper drilldown

## Phase 6 縺ｧ螳御ｺ・＠縺滄・岼

- [x] local quality gate 繧・GitHub Actions 縺ｫ霈峨○縺・- [x] required gate 繧・`pull_request` 縺ｨ `main` push 縺ｧ螳溯｡後☆繧句燕謠舌ｒ蝗ｺ螳壹＠縺・- [x] `gate.json` 繧・release gate 縺ｮ豁｣譛ｬ縺ｨ縺励※謇ｱ縺・°逕ｨ繧貞・繧後◆
- [x] `kpi.json` / `gate.json` / `summary.md` 繧・artifact 縺ｨ縺励※菫晏ｭ倥☆繧句燕謠舌ｒ謨ｴ縺医◆
- [x] optional smoke 繧・gate 縺ｧ縺ｯ縺ｪ縺・observation-only 縺ｨ縺励※蛻・屬縺励◆
- [x] handoff warning 繧・`summary.md` 縺ｧ遒ｺ隱阪〒縺阪ｋ繧医≧縺ｫ縺励◆

## Phase 6 螳御ｺ・擅莉ｶ

- GitHub 荳翫〒 required gate 縺悟虚縺・- artifact 縺ｨ step summary 縺檎｢ｺ隱阪〒縺阪ｋ
- required gate 縺ｮ fail 縺・workflow fail 縺ｫ蜿肴丐縺輔ｌ繧・- optional smoke 縺・fail 縺励※繧・required gate 繧定誠縺ｨ縺輔↑縺・- release review 縺ｧ `summary.md` 繧呈ｭ｣譛ｬ縺ｨ縺励※菴ｿ縺医ｋ

## 繝｡繝｢

- `mode 蛻・屬` 繧・`deep drilldown` 縺ｯ驥崎ｦ√□縺後｝roduct readiness 縺ｮ隕ｳ轤ｹ縺ｧ縺ｯ crash-free 縺ｨ莠呈鋤諤ｧ縺ｮ蠕後↓譚･繧・- 蟶りｲｩ繝ｬ繝吶Ν繧堤岼謖・☆縺ｪ繧峨√∪縺壹御ｽｿ縺医ｋ workspace 縺ｮ遽・峇縲阪→縲悟｣翫ｌ縺溘→縺阪・隱ｬ譏主庄閭ｽ諤ｧ縲阪ｒ螳梧・縺輔○繧・

