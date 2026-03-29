# Phase 1

縺薙・繝輔か繝ｫ繝縺ｯ `Rulixa` 縺ｮ Phase 1 莉墓ｧ倥ｒ縺ｾ縺ｨ繧√◆蜈･蜿｣縺ｧ縺吶・ 
`Rulixa` 縺ｯ縲∬ｨｭ險育衍縺ｮ謌先棡迚ｩ繧堤ｶ咏ｶ夂函謌舌＠縲￣R 繝ｬ繝薙Η繝ｼ縲∫屮譟ｻ縲∝ｷｮ蛻・｢ｺ隱阪√ラ繝ｪ繝輔ヨ讀懃衍縺ｫ菴ｿ縺医ｋ迥ｶ諷九ｒ菫昴▽縺溘ａ縺ｮ蝓ｺ逶､縺ｧ縺吶・ 
Context Pack 縺ｯ縺昴・荳ｭ縺ｧ AI 螟画峩髢句ｧ九↓菴ｿ縺・㍾隕√↑謌先棡迚ｩ縺ｧ縺吶′縲∬｣ｽ蜩∝・菴薙・荳ｻ蠖ｹ縺ｧ縺ｯ縺ゅｊ縺ｾ縺帙ｓ縲・ 
Phase 1 縺ｯ縲√◎縺ｮ蜈ｨ菴捺ｧ区Φ縺ｮ譛蛻昴・蜈ｷ菴捺判逡･蟇ｾ雎｡縺ｨ縺励※ `Windows` 荳翫・ `WPF + .NET 8` 繧呈桶縺・～scan -> resolve-entry -> pack` 繧貞ｮ牙ｮ壼喧縺吶ｋ谿ｵ髫弱〒縺吶・
荳贋ｽ肴婿驥昴・ [polaris.md](/D:/C#/Rulixa/docs/polaris.md) 縺ｨ [project_full_spec.md](/D:/C#/Rulixa/docs/project_full_spec.md) 繧呈ｭ｣譛ｬ縺ｨ縺励√％縺ｮ驟堺ｸ九・ Phase 1 縺ｮ蜈ｷ菴謎ｻ墓ｧ倥ｒ謇ｱ縺・∪縺吶・
## 迴ｾ蝨ｨ縺ｮ螳溯｣・ｯ・峇

- 繝励Ο繧ｸ繧ｧ繧ｯ繝域ｧ区・縺ｯ `Rulixa.Domain`縲～Rulixa.Application`縲～Rulixa.Infrastructure`縲～Rulixa.Plugin.WpfNet8`縲～Rulixa.Cli`
- CLI 縺ｯ `scan`縲～resolve-entry`縲～pack`縲～compare-evidence`
- `entry=file`縲～entry=symbol`縲～entry=auto` 繧貞・逅・- `SelectedItem -> CurrentPage` 縺ｮ navigation 蟆守ｷ壹ｒ謚ｽ蜃ｺ
- root binding縲」iew binding縲～DataTemplate` 隕∫ｴ・．I縲‥ialog activation 繧・Pack 縺ｫ蜿肴丐
- `auto` entry 縺ｯ `root-data-context` / `view-data-context` 繧貞━蜈医＠縲～data-template` 逕ｱ譚･蛟呵｣懊ｒ蜉｣蠕・- command 縺ｯ蟆第焚莉ｶ縺ｪ繧牙・莉ｶ隧ｳ邏ｰ蛹悶＠縲∝､壽焚莉ｶ縺ｪ繧・summary 繧堤ｶｭ謖√＠縺､縺､ `goal` 縺ｫ霑代＞ command 縺縺代ｒ隧ｳ邏ｰ蛹・- command 隧ｳ邏ｰ蛹悶〒縺ｯ `execute -> direct service/dialog` 縺ｫ蜉縺医※縲∝酔荳 ViewModel 蜀・・ `private helper 1 hop` 繧・`execute -> helper -> service/dialog` 縺ｨ縺励※陦ｨ遉ｺ
- dialog 謚ｽ蜃ｺ縺ｯ invocation 蜊倅ｽ阪〒 `show` / `show-dialog` / `owner kind` 繧貞愛螳・- 螟ｧ縺阪＞ `*.cs` 縺ｯ蜈ｨ譁・〒縺ｯ縺ｪ縺・snippet 蜆ｪ蜈医〒 Pack 縺ｫ蜈･繧後ｋ
- `pack --evidence-dir` 縺ｧ `manifest.json`縲～scan.json`縲～resolved-entry.json`縲～pack.md` 繧呈ｱｺ螳夂噪縺ｪ bundle 縺ｨ縺励※菫晏ｭ倥☆繧・- `compare-evidence` 縺ｧ bundle 2 縺､縺ｮ metadata / contracts / selected files / selected snippets 縺ｮ蟾ｮ蛻・ｒ遒ｺ隱阪☆繧・- optional smoke 縺ｨ縺励※ `<modern-real-workspace>` 繧剃ｽｿ縺・ｮ溘Ρ繝ｼ繧ｯ繧ｹ繝壹・繧ｹ讀懆ｨｼ繧堤畑諢・
## 隱ｭ繧鬆・分

- [scope.md](scope.md)
  Phase 1 縺ｮ蟇ｾ雎｡縺ｨ髱槫ｯｾ雎｡
- [architecture.md](architecture.md)
  Domain / Application / Plugin / Infrastructure 縺ｮ雋ｬ蜍吝・蜑ｲ
- [ir.md](ir.md)
  Phase 1 縺ｮ IR
- [entry_resolution.md](entry_resolution.md)
  `entry=file/symbol/auto` 縺ｮ隗｣豎ｺ隕丞援
- [wpf_net8_extraction_targets.md](wpf_net8_extraction_targets.md)
  WPF 蝗ｺ譛峨・謚ｽ蜃ｺ蟇ｾ雎｡
- [context_pack_rules.md](context_pack_rules.md)
  Pack 縺ｮ讒区・縺ｨ snippet / file 驕ｸ螳夊ｦ丞援
- [implementation_plan.md](implementation_plan.md)
  迴ｾ蝨ｨ縺ｮ螳溯｣・憾諷九∝・蜿｣譚｡莉ｶ縲∵ｬ｡縺ｮ backlog
- [examples/sample_shell_pack_example.md](examples/sample_shell_pack_example.md)
  `RealWorkspace` 繧帝｡梧攝縺ｫ縺励◆ Pack 萓・
## 螳溯｡御ｾ・
### file entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <modern-real-workspace> `
  --entry file:src/ReferenceWorkspace.Presentation.Wpf/Views/ShellView.xaml `
  --goal "Shell 逕ｻ髱｢縺ｫ譁ｰ縺励＞繝壹・繧ｸ繧定ｿｽ蜉縺励◆縺・
```

### symbol entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <modern-real-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "險ｭ螳夂判髱｢繧帝幕縺阪◆縺・
```

### command helper 蟆守ｷ壹ｒ遒ｺ隱阪＠縺溘＞蝣ｴ蜷・
```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <modern-real-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "project"
```

縺薙・蝣ｴ蜷医￣ack 縺ｮ `Command` 螂醍ｴ・→ index 縺ｧ `execute -> helper -> service/dialog` 縺ｮ邨瑚ｷｯ縺瑚ｩｳ邏ｰ蛹悶＆繧後∪縺吶・
### evidence bundle 繧呈ｮ九＠縺溘＞蝣ｴ蜷・
```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <modern-real-workspace> `
  --entry symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "project" `
  --evidence-dir artifacts\evidence
```

### evidence bundle 繧呈ｯ碑ｼ・＠縺溘＞蝣ｴ蜷・
```powershell
dotnet run --project src\Rulixa.Cli -- compare-evidence `
  --base artifacts\evidence\<base-bundle> `
  --target artifacts\evidence\<target-bundle>
```



