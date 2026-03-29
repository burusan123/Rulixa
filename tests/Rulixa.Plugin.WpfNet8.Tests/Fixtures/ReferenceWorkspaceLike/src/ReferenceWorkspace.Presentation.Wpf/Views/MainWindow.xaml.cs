using ReferenceWorkspace.Presentation.Wpf.ViewModels;

namespace ReferenceWorkspace.Presentation.Wpf.Views;

public partial class MainWindow
{
    public MainWindow(ShellViewModel shellViewModel)
    {
        DataContext = shellViewModel;
    }
}

