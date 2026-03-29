namespace LegacyServiceLocator;

public sealed class ProjectDocument
{
    public bool IsDirty { get; private set; }

    public void MarkDirty()
    {
        IsDirty = true;
    }

    public ProjectDocument Snapshot()
    {
        return new ProjectDocument { IsDirty = IsDirty };
    }

    public void Restore()
    {
        IsDirty = false;
    }
}
