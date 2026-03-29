namespace ReferenceWorkspace.Presentation.Wpf.Services;

public interface ISettingWindowService
{
    void Show();
}

public sealed class SettingWindowService : ISettingWindowService
{
    public void Show()
    {
        var window = new SettingWindow();
        window.ShowDialog();
    }
}

