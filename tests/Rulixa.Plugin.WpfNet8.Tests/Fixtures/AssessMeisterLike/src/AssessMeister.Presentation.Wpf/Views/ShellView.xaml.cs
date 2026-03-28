using AssessMeister.Presentation.Wpf.ViewModels;

namespace AssessMeister.Presentation.Wpf.Views;

public partial class ShellView
{
    public ShellView(ShellViewModel shellViewModel)
    {
        DataContext = shellViewModel;
    }
}
