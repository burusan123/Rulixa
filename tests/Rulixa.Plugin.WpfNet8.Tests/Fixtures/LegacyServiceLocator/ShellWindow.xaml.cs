namespace LegacyServiceLocator;

public partial class ShellWindow
{
    public ProjectDocument ProjectDocument { get; } = new();

    public ShellWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public void OpenReport()
    {
        var service = AppServices.Current.Get<ReportWindowService>();
        service.Show();
    }

    public void OpenDrafting()
    {
        var service = AppServices.Current.Get<DraftingDialogService>();
        service.Show();
    }
}
