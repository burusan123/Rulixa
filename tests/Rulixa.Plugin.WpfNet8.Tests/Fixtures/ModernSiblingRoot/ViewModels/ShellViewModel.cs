using ModernSiblingRoot.Models;

namespace ModernSiblingRoot.ViewModels;

public sealed class ShellViewModel
{
    public ShellViewModel(
        ShellThreeDViewModel shellThreeDViewModel,
        ReportCenterViewModel reportCenterViewModel,
        SettingsViewModel settingsViewModel,
        ProjectDocument projectDocument)
    {
        ShellThreeD = shellThreeDViewModel;
        ReportCenter = reportCenterViewModel;
        Settings = settingsViewModel;
        ProjectDocument = projectDocument;
    }

    public ShellThreeDViewModel ShellThreeD { get; }

    public ReportCenterViewModel ReportCenter { get; }

    public SettingsViewModel Settings { get; }

    public ProjectDocument ProjectDocument { get; }
}
