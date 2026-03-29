namespace LegacyDialogHeavy;

public sealed class ProjectDocument
{
    public bool IsDirty { get; private set; }

    public string SavedPath { get; private set; } = string.Empty;

    public void MarkDirty()
    {
        IsDirty = true;
    }

    public ProjectDocument Snapshot()
    {
        return new ProjectDocument { SavedPath = SavedPath, IsDirty = IsDirty };
    }

    public void Restore(string savedPath)
    {
        SavedPath = savedPath;
        IsDirty = false;
    }
}
