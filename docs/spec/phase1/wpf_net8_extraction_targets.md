# WPF + .NET 8 謚ｽ蜃ｺ蟇ｾ雎｡

## 逶ｮ逧・
Phase 1 縺ｧ縺ｯ `WPF + .NET 8` 繧｢繝励Μ繧ｱ繝ｼ繧ｷ繝ｧ繝ｳ縺九ｉ縲、I 縺悟､画峩髢句ｧ九↓蠢・ｦ√↑譛蟆城剞縺ｮ莠句ｮ溘□縺代ｒ謚ｽ蜃ｺ縺励∪縺吶・縺薙・譁・嶌縺ｧ縺ｯ `RealWorkspace` 縺ｧ螳滄圀縺ｫ萓｡蛟､縺碁ｫ倥°縺｣縺滓歓蜃ｺ蟇ｾ雎｡繧呈紛逅・＠縺ｾ縺吶・
## 1. 繝帙せ繝医→襍ｷ蜍慕ｵ瑚ｷｯ

譛蛻昴↓謚ｽ蜃ｺ縺吶∋縺榊ｯｾ雎｡:

- `App.xaml`
- `App.xaml.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- DI 逋ｻ骭ｲ繝輔ぃ繧､繝ｫ

謚ｽ蜃ｺ縺吶ｋ莠句ｮ・

- `OnStartup` 縺ｮ蟄伜惠
- `ServiceCollection` / `ServiceProvider` 縺ｮ讒区・
- `MainWindow` 縺ｮ隗｣豎ｺ譁ｹ豕・- `MainWindow.DataContext` 縺ｫ險ｭ螳壹＆繧後ｋ繝ｫ繝ｼ繝・ViewModel

## 2. DI

蟇ｾ雎｡:

- `ServiceRegistration.cs`
- `AddSingleton` / `AddScoped` / `AddTransient` / `AddFactory` 繧貞性繧繝輔ぃ繧､繝ｫ

謚ｽ蜃ｺ縺吶ｋ莠句ｮ・

- 逋ｻ骭ｲ繝輔ぃ繧､繝ｫ
- 繝ｩ繧､繝輔ち繧､繝
- `Interface -> Implementation`
- `ViewModel -> 豕ｨ蜈･繧ｵ繝ｼ繝薙せ`

## 3. View 縺ｨ ViewModel 縺ｮ蟇ｾ蠢・
蟇ｾ雎｡:

- `Views/**/*.xaml`
- `Views/**/*.xaml.cs`
- `ViewModels/**/*.cs`

謚ｽ蜃ｺ縺吶ｋ莠句ｮ・

- `x:Class`
- `DataContext = ...`
- `DataTemplate DataType`
- View 縺ｨ ViewModel 縺ｮ蟇ｾ蠢・
菫｡鬆ｼ蠎ｦ:

- `High`
  譏守､ｺ逧・↑ `DataContext` 險ｭ螳壹√∪縺溘・ `DataTemplate DataType`
- `Medium`
  code-behind 縺ｨ XAML 縺ｮ蟇ｾ蠢・- `Low`
  蜻ｽ蜷崎ｦ冗ｴ・□縺代↓繧医ｋ謗ｨ螳・
## 4. 繝翫ン繧ｲ繝ｼ繧ｷ繝ｧ繝ｳ

Phase 1 縺ｮ荳ｻ謌ｦ蝣ｴ縺ｧ縺吶ＡShellView.xaml` 縺ｮ binding 縺縺代〒縺ｯ縺ｪ縺上～ShellViewModel` 蛛ｴ縺ｮ譖ｴ譁ｰ轤ｹ縺ｾ縺ｧ謚ｽ蜃ｺ縺励∪縺吶・
蟇ｾ雎｡:

- `ShellView.xaml`
- `ShellViewModel.cs`
- `NavItemViewModel.cs`

謚ｽ蜃ｺ縺吶ｋ莠句ｮ・

- `ItemsSource`
- `SelectedItem`
- `Content`
- `SelectedItem` 縺ｮ譖ｴ譁ｰ蝨ｰ轤ｹ
- `CurrentPage` 縺ｮ譖ｴ譁ｰ蝨ｰ轤ｹ
- 莉｣蜈･繧貞性繧繝｡繧ｽ繝・ラ蜷・- 陦檎分蜿ｷ

Pack 縺ｫ谿九＠縺溘＞蟆守ｷ・

- `Items -> SelectedItem -> CurrentPage`
- `SelectedItem = match`
- `Select(...) -> CurrentPage = item.PageViewModel`

## 5. Command

蟇ｾ雎｡:

- `ICommand` 蜈ｬ髢九・繝ｭ繝代ユ繧｣
- `DelegateCommand` / `DelegateCommand<T>`
- XAML 縺ｮ `Command="{Binding ...}"`

謚ｽ蜃ｺ縺吶ｋ莠句ｮ・

- 繧ｳ繝槭Φ繝牙錐
- 螳溯｡後Γ繧ｽ繝・ラ
- `CanExecute`
- 繝舌う繝ｳ繝峨＆繧後ｋ View

## 6. Dialog / SubWindow

蟇ｾ雎｡:

- `Services/**/*.cs` 縺ｮ `ShowDialog()` / `Show()`
- `new XxxWindow(...)`
- `window.DataContext = ...`

謚ｽ蜃ｺ縺吶ｋ莠句ｮ・

- 蜻ｼ縺ｳ蜃ｺ縺怜・繧ｵ繝ｼ繝薙せ
- 襍ｷ蜍輔＆繧後ｋ Window
- 蟇ｾ蠢懊☆繧・DialogViewModel

## 7. 險ｭ螳壹→繝励Ο繧ｸ繧ｧ繧ｯ繝・
蟇ｾ雎｡:

- `*.csproj`
- `Directory.Build.props`
- `appsettings.*`
- `Properties/Settings.*`

謚ｽ蜃ｺ縺吶ｋ莠句ｮ・

- `TargetFramework`
- `UseWPF`
- 荳ｻ隕√↑險ｭ螳壹た繝ｼ繧ｹ

## 8. Pack 縺ｫ蜆ｪ蜈医＠縺ｦ蜈･繧後ｋ繝輔ぃ繧､繝ｫ

### XAML 襍ｷ轤ｹ

1. 蟇ｾ雎｡ XAML
2. 蟇ｾ蠢懊☆繧・code-behind
3. 蟇ｾ蠢懊☆繧・ViewModel
4. 襍ｷ蜍慕ｵ瑚ｷｯ
5. DI 逋ｻ骭ｲ

### ViewModel 襍ｷ轤ｹ

1. 蟇ｾ雎｡ ViewModel
2. 蟇ｾ蠢・View
3. 蟇ｾ蠢・code-behind
4. `ICommand` 縺ｨ螳溯｡後Γ繧ｽ繝・ラ
5. `SelectedItem` / `CurrentPage` 縺ｮ譖ｴ譁ｰ轤ｹ
6. 襍ｷ蜍慕ｵ瑚ｷｯ
7. DI 逋ｻ骭ｲ

## 9. Phase 1 縺ｧ謇ｱ繧上↑縺・ｂ縺ｮ

- 螳悟・縺ｪ繝・・繧ｿ繝輔Ο繝ｼ隗｣譫・- AttachedProperty / Behavior 縺ｮ螳悟・隗｣譫・- VisualState 縺ｮ螳悟・隗｣譫・- 3rd party control 蝗ｺ譛峨・隧ｳ邏ｰ隗｣譫・- 螳溯｡梧凾 DI 繧ｳ繝ｳ繝・リ迥ｶ諷九・螳悟・隗｣譫・

