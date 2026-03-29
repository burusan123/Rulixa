namespace LegacyDialogHeavy;

public partial class ShellWindow
{
    public ProjectDocument ProjectDocument { get; } = new();

    public ShellWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public void OpenSettings()
    {
        ForwardToSettings();
    }

    public void OpenReport()
    {
        ForwardToReport();
    }

    private void ForwardToSettings()
    {
        var adapter = new SettingsWindowAdapter();
        adapter.Show();
    }

    private void ForwardToReport()
    {
        var adapter = new ReportWindowAdapter();
        adapter.Show();
    }
}
