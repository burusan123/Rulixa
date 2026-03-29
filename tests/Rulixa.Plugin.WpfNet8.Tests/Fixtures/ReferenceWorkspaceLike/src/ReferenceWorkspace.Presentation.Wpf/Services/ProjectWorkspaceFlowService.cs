using ReferenceWorkspace.Presentation.Wpf.Models;

namespace ReferenceWorkspace.Presentation.Wpf.Services;

public interface IProjectWorkspaceFlowService
{
    ProjectDocument OpenMostRecent();
}

public sealed class ProjectWorkspaceFlowService : IProjectWorkspaceFlowService
{
    private readonly IProjectRepository projectRepository;
    private readonly ISettingsQuery settingsQuery;

    public ProjectWorkspaceFlowService(
        IProjectRepository projectRepository,
        ISettingsQuery settingsQuery)
    {
        this.projectRepository = projectRepository;
        this.settingsQuery = settingsQuery;
    }

    public ProjectDocument OpenMostRecent()
    {
        var settings = settingsQuery.Load();
        var document = projectRepository.Load(settings.DefaultProjectPath);
        document.MarkDirty();
        return document;
    }
}

