namespace LegacyDialogHeavy;

public sealed class SettingsWindowAdapter
{
    public void Show()
    {
        var window = new SettingsWindow();
        window.ShowDialog();
    }
}
