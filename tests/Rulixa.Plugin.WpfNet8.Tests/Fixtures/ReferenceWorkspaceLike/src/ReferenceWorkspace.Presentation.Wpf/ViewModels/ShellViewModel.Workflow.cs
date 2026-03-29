using ReferenceWorkspace.Presentation.Wpf.Models;

namespace ReferenceWorkspace.Presentation.Wpf.ViewModels;

public sealed partial class ShellViewModel
{
    private void LoadPagesFromProjectDocument(ProjectDocument projectDocument)
    {
        projectWorkspaceService.Remember(projectDocument);
        projectDocument.MarkSaved();
    }

    private void OpenSettingsCore()
    {
        settingWindowService.Show();
    }
}

