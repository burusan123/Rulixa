using System.Collections.ObjectModel;
using System.Linq;
using AssessMeister.Presentation.Wpf.Common;

namespace AssessMeister.Presentation.Wpf.ViewModels;

public sealed class ShellViewModel
{
    public DelegateCommand OpenSettingsCommand { get; }

    public ObservableCollection<NavItemViewModel> Items { get; } = [];

    public NavItemViewModel? SelectedItem { get; set; }

    public object? CurrentPage { get; private set; }

    public ShellViewModel()
    {
        OpenSettingsCommand = new DelegateCommand(OpenSettings);
        var dashboardPage = new DashboardPageViewModel();
        var item = new NavItemViewModel("Dashboard", dashboardPage);
        Items.Add(item);
        RestoreSelection();
    }

    private void RestoreSelection()
    {
        var match = Items.FirstOrDefault();
        if (match is null)
        {
            return;
        }

        SelectedItem = match;
        Select(match);
    }

    private void Select(NavItemViewModel item)
    {
        CurrentPage = item.PageViewModel;
    }

    private void OpenSettings()
    {
    }
}
