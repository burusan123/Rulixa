# Compatibility And Corpus

## 蟇ｾ蠢懷ｯｾ雎｡縺ｮ諡｡蠑ｵ

Phase 5 縺ｧ縺ｯ縲￣hase 4 縺ｧ蟆主・縺励◆ legacy WPF compatibility 繧偵御ｸ驛ｨ縺ｮ螳滉ｾ九〒騾壹ｋ縲阪°繧峨御ｻ｣陦ｨ繝代ち繝ｼ繝ｳ繧堤ｶ咏ｶ夂噪縺ｫ螳医ｌ繧九阪↓蠑輔″荳翫￡繧九・
### 驥咲せ繝代ち繝ｼ繝ｳ

- `App.xaml StartupUri`
- `App.xaml.cs` startup handler
- `DataContext = this`
- `DataContext = new XxxViewModel()`
- `new Window()` / `ShowDialog()` / `Show()`
- code-behind event handler 縺九ｉ縺ｮ逶ｴ謗･襍ｷ蜍・- simple forwarding helper / adapter
- service locator 逧・↑ root resolution
- ResourceDictionary / merged dictionaries
- heavy comment / disabled XAML block

### 谿ｵ髫守噪縺ｫ謇ｱ縺・ヱ繧ｿ繝ｼ繝ｳ

- lambda / delegate 邨檎罰縺ｮ邁｡譏・forwarding
- partial class 繧偵∪縺溘＄ code-behind route
- static factory / singleton accessor 邨檎罰縺ｮ window creation

## Acceptance Corpus

### Real Workspaces

- `<modern-real-workspace>`
- `<legacy-real-workspace>`

### Synthetic Fixtures

- modern DI-based root
- code-behind-heavy root
- dialog-heavy root
- `DataContext = this` root
- sibling ViewModel ・卓峡 root
- service locator / manual new root
- ResourceDictionary-heavy XAML
- duplicate alias / commented XAML edge case

## Corpus 驕狗畑譁ｹ驥・
- bug fix 縺ｫ縺ｯ蠢・★ synthetic regression 縺・real smoke 縺ｮ縺ｩ縺｡繧峨°繧定ｿｽ蜉縺吶ｋ
- real workspace 蝗ｺ譛峨↓隕九∴繧倶ｸ榊・蜷医ｂ縲∝庄閭ｽ縺ｪ繧画怙蟆・synthetic fixture 縺ｫ關ｽ縺ｨ縺・- `RealWorkspace` 邉ｻ縺ｫ萓晏ｭ倥＠縺溽音谿雁・蟯舌・螳溯｣・＠縺ｪ縺・
## Acceptance 譛菴弱Λ繧､繝ｳ

- root entry 縺ｧ crash-free
- partial pack 繧貞性繧√※譛臥畑縺ｪ map 縺瑚ｿ斐ｋ
- unsupported / degraded 縺ｯ diagnostic 縺ｨ縺励※隕九∴繧・- non-root entry 縺ｮ譌｢蟄伜刀雉ｪ縺悟｣翫ｌ縺ｪ縺・

