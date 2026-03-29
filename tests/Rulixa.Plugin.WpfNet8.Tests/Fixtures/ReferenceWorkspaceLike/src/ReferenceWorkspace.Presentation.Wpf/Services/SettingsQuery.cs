namespace ReferenceWorkspace.Presentation.Wpf.Services;

public interface ISettingsQuery
{
    WorkspaceSettings Load();
}

public sealed class SettingsQuery : ISettingsQuery
{
    public WorkspaceSettings Load()
    {
        var settingsPath = Path.Combine("Config", "WorkspaceSettings.xlsx");
        if (!File.Exists(settingsPath))
        {
            return new WorkspaceSettings("project.asm");
        }

        return new WorkspaceSettings("project.asm");
    }
}

public sealed record WorkspaceSettings(string DefaultProjectPath);

