namespace LegacyDialogHeavy;

public sealed class ReportWindowAdapter
{
    public void Show()
    {
        var window = new ReportWindow();
        window.ShowDialog();
    }
}
