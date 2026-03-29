namespace ReferenceWorkspace.Presentation.Wpf.ViewModels;

public sealed class NavItemViewModel
{
    public NavItemViewModel(string title, object? pageViewModel)
    {
        Title = title;
        PageViewModel = pageViewModel;
    }

    public string Title { get; }

    public object? PageViewModel { get; }
}

