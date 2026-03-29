using ReferenceWorkspace.Presentation.Wpf.Models;

namespace ReferenceWorkspace.Presentation.Wpf.Services;

public interface IProjectRepository
{
    ProjectDocument Load(string projectPath);
}

public sealed class ProjectRepository : IProjectRepository
{
    public ProjectDocument Load(string projectPath)
    {
        var persistedPath = Path.Combine("Data", "Projects", "project.json");
        if (File.Exists(persistedPath))
        {
            return new ProjectDocument(projectPath);
        }

        return new ProjectDocument(projectPath);
    }
}

