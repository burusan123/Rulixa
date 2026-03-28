using AssessMeister.Presentation.Wpf.ViewModels;

namespace AssessMeister.Presentation.Wpf.Views;

public partial class MainWindow
{
    public MainWindow(ShellViewModel shellViewModel)
    {
        DataContext = shellViewModel;
    }
}
