using ModernSiblingRoot.Models;
using ModernSiblingRoot.ViewModels;

namespace ModernSiblingRoot;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ShellViewModel(
            new ShellThreeDViewModel(),
            new ReportCenterViewModel(),
            new SettingsViewModel(),
            new ProjectDocument());
    }
}
