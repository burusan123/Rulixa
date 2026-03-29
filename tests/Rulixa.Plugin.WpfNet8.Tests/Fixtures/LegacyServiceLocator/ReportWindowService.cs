namespace LegacyServiceLocator;

public sealed class ReportWindowService
{
    public void Show()
    {
        new ReportWindow().ShowDialog();
    }
}
