# 螳溯｣・ｨ育判

## 迴ｾ蝨ｨ縺ｮ蛻ｰ驕皮せ

Phase 1 縺ｯ `WPF + .NET 8` 繝ｯ繝ｼ繧ｯ繧ｹ繝壹・繧ｹ繧貞ｯｾ雎｡縺ｫ縲～scan -> resolve-entry -> pack` 繧剃ｸ騾壹ｊ騾壹○繧狗憾諷九〒縺吶ら樟蝨ｨ縺ｮ荳ｻ縺ｪ螳溯｣・ｸ医∩鬆・岼縺ｯ谺｡縺ｧ縺吶・
- `scan` 縺ｧ WPF 蝗ｺ譛峨・莠句ｮ溘ｒ謚ｽ蜃ｺ縺吶ｋ
- `resolve-entry` 縺ｧ `file` / `symbol` / `auto` 繧定ｧ｣豎ｺ縺吶ｋ
- `pack` 縺ｧ contracts / index / selected files / selected snippets / unknowns 繧堤函謌舌☆繧・- `pack --evidence-dir` 縺ｧ `manifest.json`縲～scan.json`縲～resolved-entry.json`縲～pack.md` 繧・bundle 縺ｨ縺励※菫晏ｭ倥☆繧・- `compare-evidence` 縺ｧ bundle 髢薙・ metadata / contracts / selected files / selected snippets 蟾ｮ蛻・ｒ謠冗判縺吶ｋ
- command 隧ｳ邏ｰ蛹悶↓蟇ｾ縺吶ｋ `goal` 譬ｹ諡繧・`decisionTraces` 縺ｨ縺励※ manifest 縺ｫ菫晏ｭ倥＠縲∝ｷｮ蛻・ｯ碑ｼ・↓蜷ｫ繧√ｋ
- `publish/*` 縺ｨ `*_wpftmp.csproj` 繧・scan 縺九ｉ髯､螟悶☆繧・- `file entry` 縺ｧ縺ｯ `DataTemplate` 繧定ｦ∫ｴ・･醍ｴ・→縺励※謇ｱ縺・- `SelectedItem -> CurrentPage` 縺ｮ navigation 蟆守ｷ壹ｒ螂醍ｴ・→ index 縺ｫ蜃ｺ縺・- root binding縲．I registration縲］avigation update 繧・snippet 縺ｨ縺励※驕ｸ螳壹☆繧・- `auto` entry 縺ｧ縺ｯ `root-data-context` / `view-data-context` 繧貞━蜈医＠縲～data-template` 逕ｱ譚･蛟呵｣懊ｒ豺ｷ縺懊☆縺弱↑縺・- dialog activation 縺ｯ caller / target window / activation kind / owner kind 繧・invocation 蜊倅ｽ阪〒謚ｽ蜃ｺ縺吶ｋ
- command 縺ｯ蟆第焚莉ｶ縺ｪ繧牙・莉ｶ隧ｳ邏ｰ蛹悶＠縲∝､壽焚莉ｶ縺ｪ繧・summary 繧堤ｶｭ謖√＠縺､縺､ `goal` 縺ｫ霑代＞ command 縺縺代ｒ隧ｳ邏ｰ蛹悶☆繧・- command 縺ｮ隧ｳ邏ｰ蛹悶〒縺ｯ `execute -> direct service/dialog` 縺縺代〒縺ｪ縺上∝酔荳 ViewModel 蜀・・ `private helper 1 hop` 繧・`execute -> helper -> service/dialog` 縺ｨ縺励※蜃ｺ縺・- 螟ｧ縺阪＞ `*.cs` 縺ｯ snippet 荳ｭ蠢・〒 Pack 縺ｫ蜷ｫ繧√ｋ

## 雋ｬ蜍吝・蜑ｲ

### Domain

- `Entry`
- `ResolvedEntry`
- `Budget`
- `ContextPack`
- `SelectedSnippet`
- `SourceSpan`

### Application

- `ScanWorkspaceUseCase`
- `ResolveEntryUseCase`
- `BuildContextPackUseCase`
- `IWorkspaceScanner`
- `IEntryResolver`
- `IContractExtractor`
- `IContextPackRenderer`

### Plugin / Infrastructure

- `Extraction/Context`
- `Extraction/Sections`
- `Extraction/Snippets`
- `Scanning/Context`
- `Scanning/Sections`

`WpfNet8ContractExtractor` 縺ｨ `WpfNet8WorkspaceScanner` 縺ｯ orchestration 縺ｫ逡吶ａ縲仝PF 蝗ｺ譛峨・謚ｽ蜃ｺ縺ｯ section / builder 縺ｫ髢峨§霎ｼ繧√ｋ譁ｹ驥昴〒縺吶・
## 繝・せ繝育憾豕・
### Domain

- budget 驟堺ｸ九〒縺ｮ file/snippet 驕ｸ螳・- snippet merge
- snippet 蜆ｪ蜈磯・- `SourceSpan` 縺ｮ謨ｴ蛻・
### Application

- `BuildContextPackUseCase`
- `MarkdownContextPackRenderer`
- evidence bundle writer / reader / diff renderer

### Plugin

- fixture scan
- `file entry` pack
- `symbol entry` pack
- `auto` entry 隗｣豎ｺ
- command summary 縺ｨ goal 騾｣蜍輔・隧ｳ邏ｰ蛹・- command helper 1 hop 霑ｽ霍｡
- direct service / dialog / non-dialog 縺ｮ command 蠖ｱ髻ｿ蜈・- root binding / registration snippet
- XAML navigation snippet
- generated / temp file 髯､螟・- dialog activation 縺ｮ `show` / `show-dialog` 蛻､螳・- Pack 縺ｮ豎ｺ螳壽ｧ
- evidence bundle 縺ｮ蜀榊茜逕ｨ縺ｨ revision 騾驕ｿ
- evidence bundle 蟾ｮ蛻・・豎ｺ螳夂噪陦ｨ遉ｺ
- command 隧ｳ邏ｰ蛹悶↓蟇ｾ縺吶ｋ `goal` 譬ｹ諡縺ｮ菫晏ｭ倥→豈碑ｼ・
## Phase 1 縺ｮ蜃ｺ蜿｣譚｡莉ｶ

- `plugins/rulixa/.codex-plugin/plugin.json` 縺・JSON 縺ｨ縺励※豁｣蟶ｸ縺ｫ隱ｭ繧√ｋ
- fixture 繝吶・繧ｹ縺ｮ `scan` / `resolve-entry` / `pack` 縺悟ｮ牙ｮ壹＠縺ｦ騾壹ｋ
- 蜷御ｸ蜈･蜉帙・ `pack --evidence-dir` 縺悟酔荳 bundle 蜷阪↓蜿弱∪繧翫∵里蟄・evidence 繧剃ｸ頑嶌縺咲ｴ螢翫＠縺ｪ縺・- `compare-evidence` 縺・metadata / contracts / selected files / selected snippets 縺ｮ蟾ｮ蛻・ｒ螳牙ｮ夊｡ｨ遉ｺ縺吶ｋ
- `goal` 繧貞､峨∴縺溘→縺阪↓ command 隧ｳ邏ｰ蛹匁ｹ諡縺ｮ蟾ｮ蛻・′ manifest / compare-evidence 縺ｧ霑ｽ縺医ｋ
- `auto` entry 縺ｧ莠梧ｬ｡譁・ц蛟呵｣懊′蜈磯ｭ縺ｫ豺ｷ縺悶ｊ縺吶℃縺ｪ縺・- dialog 螂醍ｴ・′ caller / target window / activation kind 繧貞ｮ牙ｮ壹＠縺ｦ蜷ｫ繧
- command 螂醍ｴ・′螳溽畑荳雁ｿ・ｦ√↑遽・峇縺ｧ `execute -> service/dialog` 縺ｾ縺溘・ `execute -> helper -> service/dialog` 繧堤､ｺ縺帙ｋ
- `dotnet test Rulixa.sln` 縺悟ｸｸ譎ゅげ繝ｪ繝ｼ繝ｳ

## 莉ｻ諢上せ繝｢繝ｼ繧ｯ讀懆ｨｼ

- `<modern-real-workspace>` 繧貞盾閠・Ρ繝ｼ繧ｯ繧ｹ繝壹・繧ｹ縺ｨ縺励※菴ｿ縺・- 譌｢螳壹〒縺ｯ繧ｹ繧ｭ繝・・縺励～RULIXA_RUN_ASSESSMEISTER_SMOKE=1` 縺ｮ縺ｨ縺阪□縺大ｮ溯｡後☆繧・- 蟇ｾ雎｡ entry 縺ｯ `symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel`
- 遒ｺ隱榊ｯｾ雎｡縺ｯ谺｡縺ｫ髯仙ｮ壹☆繧・  - resolved entry
  - Shell 蟆守ｷ壹・荳ｻ隕・contracts
  - `SelectedItem -> CurrentPage`
  - DI 隕∫ｴ・  - command helper 邨瑚ｷｯ繧貞性繧隧ｳ邏ｰ螂醍ｴ・′蟆代↑縺上→繧・1 莉ｶ縺ゅｋ縺薙→

## CI 險ｭ險医Γ繝｢

縺薙・谿ｵ髫弱〒縺ｯ CI 閾ｪ菴薙・譛ｪ螳溯｣・〒縺吶よｬ｡谿ｵ縺ｧ閾ｪ蜍募喧縺吶ｋ蟇ｾ雎｡縺ｯ谺｡縺ｧ縺吶・
- `dotnet restore`
- `dotnet build`
- `dotnet test Rulixa.sln`
- plugin manifest 螯･蠖捺ｧ遒ｺ隱・
螟夜Κ繝ｯ繝ｼ繧ｯ繧ｹ繝壹・繧ｹ萓晏ｭ倥・ optional smoke 縺ｯ CI 蟇ｾ雎｡螟悶→縺励∪縺吶・
## 谺｡縺ｮ backlog

### P1

- Pack 譁・擇縺ｨ renderer 縺ｫ谿九▲縺ｦ縺・ｋ譁・ｭ怜喧縺代・謨ｴ逅・- `goal` 縺ｫ繧医ｋ command 隧ｳ邏ｰ蛹悶・譬ｹ諡陦ｨ遉ｺ
- compare-evidence 縺ｮ蟾ｮ蛻・ｲ貞ｺｦ繝輔ぅ繝ｫ繧ｿ

### P2

- selected file reason / snippet reason 縺ｮ邊貞ｺｦ謨ｴ逅・- command helper 邨瑚ｷｯ縺ｮ隱ｬ譏取枚縺ｮ逎ｨ縺崎ｾｼ縺ｿ
- dialog activation 縺ｮ owner / activation kind 縺ｮ陦ｨ迴ｾ邨ｱ荳

### P3

- `goal` 縺ｫ蠢懊§縺・snippet 蜆ｪ蜈亥ｺｦ縺ｮ邏ｰ縺九＞隱ｿ謨ｴ
- 驟榊ｸ・plugin 蜷代￠縺ｮ蟆主・隱ｬ譏弱→驕狗畑蟆守ｷ壹・謨ｴ蛯・

