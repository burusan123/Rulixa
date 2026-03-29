using ReferenceWorkspace.Presentation.Wpf.ViewModels;

namespace ReferenceWorkspace.Presentation.Wpf.Views;

public partial class ShellView
{
    public ShellView(ShellViewModel shellViewModel)
    {
        DataContext = shellViewModel;
    }
}

