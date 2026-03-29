# Context Pack 繝ｫ繝ｼ繝ｫ

## 菴咲ｽｮ縺･縺・
Phase 1 縺ｮ `Context Pack` 縺ｯ縲～WPF + .NET 8` 縺ｮ螟画峩蟇ｾ雎｡縺ｫ蟇ｾ縺励※ AI 縺ｫ貂｡縺呎怙蟆城剞縺ｮ讒矩蛹悶さ繝ｳ繝・く繧ｹ繝医〒縺吶・ 
`entry=file` 縺ｨ `entry=symbol` 繧剃ｸｭ蠢・↓縲∝ｿ・ｦ√↑讒矩莠句ｮ溘→譬ｹ諡 snippet 繧貞━蜈医＠縺ｦ縺ｾ縺ｨ繧√∪縺吶・
Rulixa 蜈ｨ菴薙・荳ｭ縺ｧ縺ｯ縲，ontext Pack 縺ｯ邯咏ｶ夂函謌舌＆繧後ｋ謌先棡迚ｩ鄒､縺ｮ荳驛ｨ縺ｧ縺吶・ 
荳贋ｽ阪↓縺ｯ `Contracts` 繧・`Index / Map` 縺ｮ繧医≧縺ｪ險ｭ險域・譫懃黄縺後≠繧翫・°逕ｨ荳翫・ PR 繝ｬ繝薙Η繝ｼ繧・屮譟ｻ縺ｫ菴ｿ縺・ｷｮ蛻・・譫懃黄繧よ桶縺・∪縺吶・ontext Pack 縺ｯ縺昴・荳ｭ縺ｧ縲、I 縺ｫ螟画峩髢句ｧ狗畑縺ｮ譛蟆乗據繧呈ｸ｡縺吝ｽｹ蜑ｲ繧呈戟縺｡縺ｾ縺吶・
## Pack 縺ｮ蝓ｺ譛ｬ讒区・

1. `goal`
2. `entry`
3. `resolved entry`
4. `contracts`
5. `impact / index`
6. `selected snippets`
7. `selected files`
8. `unknowns`

## contracts

Phase 1 縺ｧ謇ｱ縺・･醍ｴ・・谺｡縺ｧ縺吶・
- 襍ｷ蜍慕ｵ瑚ｷｯ
- DI 逋ｻ骭ｲ
- View 縺ｨ ViewModel 縺ｮ蟇ｾ蠢・- navigation
- command
- dialog activation

陬懆ｶｳ:

- 螟画峩菴懈･ｭ縺ｫ蜉ｹ縺丈ｺ句ｮ溘ｒ蜆ｪ蜈医＠縲∬ｪｬ譏弱□縺代・諠・ｱ縺ｯ蠅励ｄ縺励☆縺弱↑縺・- `file entry` 縺ｮ `DataTemplate` 縺ｯ隕∫ｴ・･醍ｴ・→縺励※謇ｱ縺・- DI 縺ｯ髢｢菫ゅ☆繧狗匳骭ｲ縺縺代ｒ隕∫ｴ・☆繧・
### command 螂醍ｴ・
command 螂醍ｴ・・ `goal` 縺ｨ command 莉ｶ謨ｰ縺ｫ蠢懊§縺ｦ蜃ｺ縺玲婿繧貞､峨∴繧九・
- command 謨ｰ縺悟ｰ代↑縺・ｴ蜷医・蜈ｨ莉ｶ繧貞句挨螂醍ｴ・→縺励※蜃ｺ縺・- command 謨ｰ縺悟､壹＞蝣ｴ蜷医・ summary 繧堤ｶｭ謖√＠縺､縺､縲～goal` 縺ｫ霑代＞ command 縺縺代ｒ霑ｽ蜉縺ｧ隧ｳ邏ｰ蛹悶☆繧・- 隧ｳ邏ｰ蛹門ｯｾ雎｡縺ｧ縺ｯ `execute -> direct service/dialog` 縺縺代〒縺ｪ縺上∝酔荳 ViewModel 蜀・・ `private helper 1 hop` 繧・`execute -> helper -> service/dialog` 縺ｨ縺励※陦ｨ迴ｾ縺吶ｋ
- helper 縺ｯ `private` / `internal` / `protected` 縺ｮ instance method 縺ｮ縺ｿ蟇ｾ雎｡縺ｨ縺励・ hop 莉･荳翫・霑ｽ繧上↑縺・
萓・

- `OpenSettingCommand 縺ｯ ShellViewModel.OpenSetting(...) 繧貞ｮ溯｡後＠縲！SettingWindowService.OpenSettingWindow(...) 繧貞他縺ｳ蜃ｺ縺励∪縺吶Ａ
- `NewProjectCommand 縺ｯ ShellViewModel.NewProject(...) 繧貞ｮ溯｡後＠縲ヾhellViewModel.LoadPagesFromProjectDocument(...) 繧堤ｵ檎罰縺励※ ... 繧貞他縺ｳ蜃ｺ縺励∪縺吶Ａ

dialog 縺ｫ謗･邯壹〒縺阪ｋ蝣ｴ蜷医・譛ｫ蟆ｾ縺ｫ `譛邨ら噪縺ｫ XxxWindow 縺・show-dialog 縺ｧ襍ｷ蜍輔＆繧後∪縺吶Ａ 繧剃ｻ倥￠繧九・
## impact / index

Index 縺ｯ Pack 縺ｮ遒ｺ隱咲畑縺ｧ縲∽ｸｻ隕√↑邨瑚ｷｯ縺縺代ｒ遏ｭ縺剰｡ｨ遉ｺ縺吶ｋ縲・
- `View-ViewModel`
- `Navigation`
- `襍ｷ蜍慕ｵ瑚ｷｯ`
- `DI`
- `Command`

### command index

- 蟆第焚 command: `Command -> Execute -> Service/Dialog`
- helper 邨檎罰: `Command -> Execute -> Helper -> Service/Dialog`
- 螟壽焚 command: summary 陦・+ `goal` 縺ｫ髢｢騾｣縺吶ｋ隧ｳ邏ｰ陦・
## selected snippets

`selected snippets` 縺ｯ蜈ｨ譁・〒縺ｯ縺ｪ縺上∝愛譁ｭ縺ｫ蠢・ｦ√↑譁ｭ迚・ｒ蜆ｪ蜈医☆繧九・ 
螟ｧ縺阪＞ `*.cs` 縺ｯ snippet 縺ｧ謇ｱ縺・∝ｿ・ｦ∵怙蟆城剞縺ｮ遽・峇縺ｫ蛻・ｊ蜃ｺ縺吶・
菫晄戟鬆・岼:

- `path`
- `reason`
- `priority`
- `isRequired`
- `anchor`
- `startLine`
- `endLine`
- `content`

### snippet 驕ｸ螳夊ｦ丞援

- `*.cs` 縺九▽ `LineCount > 250` 縺ｮ繝輔ぃ繧､繝ｫ縺ｧ縺ｯ snippet 繧貞━蜈医☆繧・- 蜷御ｸ繝輔ぃ繧､繝ｫ縺ｮ snippet 縺ｯ `maxSnippetsPerFile` 縺ｾ縺ｧ縺ｫ謚代∴繧・- 霑第磁 snippet 縺ｯ 1 縺､縺ｫ merge 縺吶ｋ
- 陦梧焚繧医ｊ繧ょ､画峩逅・罰縺ｮ隱ｬ譏主鴨繧貞━蜈医☆繧・
### Phase 1 縺ｧ蜆ｪ蜈医☆繧・snippet

- constructor
- navigation update
- root binding 縺ｮ code-behind
- `ServiceRegistration.cs` 縺ｮ髢｢騾｣ DI 逋ｻ骭ｲ
- command execute
- command helper
- dialog service / dialog activation method

### snippet 縺ｮ鬆・ｺ・
1. root binding
2. DI registration
3. DI constructor
4. navigation update
5. command execute / helper / dialog method

## selected files

`selected files` 縺ｯ蜈ｨ譁・〒隱ｭ繧萓｡蛟､縺後≠繧九ヵ繧｡繧､繝ｫ繧貞━蜈医☆繧九ゆｸｻ縺ｫ谺｡繧貞性繧√ｋ縲・
- entry XAML / code-behind
- 襍ｷ蜍慕ｵ瑚ｷｯ
- `ServiceRegistration.cs`
- root view 繧呈髪縺医ｋ髢｢騾｣繝輔ぃ繧､繝ｫ
- command support 縺ｮ陬懷勧繧ｯ繝ｩ繧ｹ
- command 蠖ｱ髻ｿ蜈医→縺励※逶ｴ謗･蜻ｼ縺ｰ繧後ｋ service / dialog 螳溯｣・
## budget 蜆ｪ蜈磯・ｽ・
莠育ｮ苓ｶ・℃譎ゅ・谺｡縺ｮ鬆・〒蜑翫ｋ縲・
1. `DataTemplate` 逕ｱ譚･縺ｮ莠梧ｬ｡譁・ц
2. 陬懷勧繧ｵ繝ｼ繝薙せ
3. 螟ｧ縺阪＞繝輔ぃ繧､繝ｫ縺ｮ蜈ｨ譁・
谺｡縺ｯ谿九☆縲・
- entry
- 荳ｻ蟇ｾ雎｡ ViewModel 縺ｮ蟆守ｷ・- root binding
- 襍ｷ蜍慕ｵ瑚ｷｯ
- 髢｢騾｣ DI 逋ｻ骭ｲ
- `SelectedItem` / `CurrentPage` 縺ｮ譖ｴ譁ｰ
- `goal` 縺ｫ髢｢騾｣縺吶ｋ command 縺ｮ execute / helper / service 蟆守ｷ・

