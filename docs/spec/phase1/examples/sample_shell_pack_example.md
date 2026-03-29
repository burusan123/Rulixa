# RealWorkspace Shell Pack 萓・
## 蜈･蜉・
```text
entry=symbol:ReferenceWorkspace.Presentation.Wpf.ViewModels.ShellViewModel
goal=Shell 逕ｻ髱｢縺ｫ譁ｰ縺励＞繝壹・繧ｸ繧定ｿｽ蜉縺励◆縺・budget.maxFiles=8
budget.maxTotalLines=1600
budget.maxSnippetsPerFile=3
```

## 縺薙・ pack 縺ｧ蜈医↓隱ｭ繧√ｋ縺薙→

- `MainWindow.xaml.cs -> ShellViewModel` 縺ｮ root binding
- `ServiceRegistration.cs` 縺ｧ縺ｮ `ShellViewModel` 逋ｻ骭ｲ
- `SelectedItem = match` 縺ｨ `CurrentPage = item.PageViewModel` 縺ｮ蝗譫・- `ShellViewModel` 縺ｮ constructor 豕ｨ蜈･
- `DataTemplate` 縺ｯ隕∫ｴ・□縺代ｒ谿九＠縲∽ｺ梧ｬ｡譁・ц縺ｮ蠅玲ｮ悶ｒ謚代∴繧・
## 譛溷ｾ・☆繧・contracts

- `襍ｷ蜍慕ｵ瑚ｷｯ`
- `荳ｻ隕・ViewModel 縺ｮ逋ｻ骭ｲ`
- `逶ｴ謗･萓晏ｭ倥・繝ｩ繧､繝輔ち繧､繝`
- `繝ｫ繝ｼ繝・DataContext`
- `驕ｸ謚槭°繧芽｡ｨ遉ｺ縺ｸ縺ｮ蝗譫彖
- `ViewModel 譖ｴ譁ｰ轤ｹ`
- `DataTemplate 莠梧ｬ｡譁・ц`

## 譛溷ｾ・☆繧・selected snippets

```text
src/ReferenceWorkspace.Presentation.Wpf/Views/MainWindow.xaml.cs:6-9
- reason: root-binding-source
- anchor: 繝ｫ繝ｼ繝・DataContext

src/ReferenceWorkspace.Presentation.Wpf/ServiceRegistration.cs:8-10
- reason: dependency-injection
- anchor: ShellViewModel (Singleton)

src/ReferenceWorkspace.Presentation.Wpf/ViewModels/ShellViewModel.cs:19-51
- reason: dependency-injection / navigation-update
- anchor: ShellViewModel(...) / RestoreSelection(...) / Select(...)
```

縺薙・鬆・ｺ上〒縲｜inding -> DI -> navigation 縺ｮ隱ｭ隗｣鬆・ｒ邯ｭ謖√＠縺ｾ縺吶・
## 譛溷ｾ・☆繧・selected files

- `src/ReferenceWorkspace.Presentation.Wpf/Views/ShellView.xaml`
- `src/ReferenceWorkspace.Presentation.Wpf/Views/ShellView.xaml.cs`
- `src/ReferenceWorkspace.Presentation.Wpf/Views/MainWindow.xaml`
- `src/ReferenceWorkspace.Presentation.Wpf/Views/MainWindow.xaml.cs`
- `src/ReferenceWorkspace.Presentation.Wpf/App.xaml.cs`
- `src/ReferenceWorkspace.Presentation.Wpf/ServiceRegistration.cs`
- `src/ReferenceWorkspace.Presentation.Wpf/Common/DelegateCommand.cs`

`ShellViewModel.cs` 縺ｯ蟾ｨ螟ｧ繝輔ぃ繧､繝ｫ縺ｪ縺ｮ縺ｧ蜈ｨ譁・〒縺ｯ谿九＆縺壹《nippet 縺ｫ鄂ｮ縺肴鋤縺医∪縺吶・

