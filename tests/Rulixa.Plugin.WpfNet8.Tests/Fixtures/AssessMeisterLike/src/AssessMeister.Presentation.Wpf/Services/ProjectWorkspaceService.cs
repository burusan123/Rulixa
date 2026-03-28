using AssessMeister.Presentation.Wpf.Models;

namespace AssessMeister.Presentation.Wpf.Services;

public interface IProjectWorkspaceService
{
    void Remember(ProjectDocument projectDocument);
}

public sealed class ProjectWorkspaceService : IProjectWorkspaceService
{
    public ProjectDocument? CurrentDocument { get; private set; }

    public void Remember(ProjectDocument projectDocument)
    {
        CurrentDocument = projectDocument;
    }
}
