using System.Collections.ObjectModel;
using System.Linq;
using ReferenceWorkspace.Presentation.Wpf.Common;
using ReferenceWorkspace.Presentation.Wpf.Models;
using ReferenceWorkspace.Presentation.Wpf.Services;

namespace ReferenceWorkspace.Presentation.Wpf.ViewModels;

public sealed partial class ShellViewModel
{
    private readonly IProjectWorkspaceService projectWorkspaceService;
    private readonly IProjectWorkspaceFlowService projectWorkspaceFlowService;
    private readonly ISettingWindowService settingWindowService;

    public DelegateCommand OpenSettingsCommand { get; }

    public ObservableCollection<NavItemViewModel> Items { get; } = [];

    public NavItemViewModel? SelectedItem { get; set; }

    public object? CurrentPage { get; private set; }

    public ShellViewModel(
        IProjectWorkspaceService projectWorkspaceService,
        IProjectWorkspaceFlowService projectWorkspaceFlowService,
        ISettingWindowService settingWindowService)
    {
        this.projectWorkspaceService = projectWorkspaceService;
        this.projectWorkspaceFlowService = projectWorkspaceFlowService;
        this.settingWindowService = settingWindowService;
        OpenSettingsCommand = new DelegateCommand(OpenSettings);

        var dashboardPage = new DashboardPageViewModel();
        var item = new NavItemViewModel("Dashboard", dashboardPage);
        Items.Add(item);

        var projectDocument = projectWorkspaceFlowService.OpenMostRecent();
        LoadPagesFromProjectDocument(projectDocument);
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
        _ = projectWorkspaceService;
        OpenSettingsCore();
    }
}

