using AssessMeister.Presentation.Wpf.Common;

namespace AssessMeister.Presentation.Wpf.ViewModels;

public sealed class ShellViewModel
{
    public DelegateCommand OpenSettingsCommand { get; }

    public ShellViewModel()
    {
        OpenSettingsCommand = new DelegateCommand(OpenSettings);
    }

    private void OpenSettings()
    {
    }
}
