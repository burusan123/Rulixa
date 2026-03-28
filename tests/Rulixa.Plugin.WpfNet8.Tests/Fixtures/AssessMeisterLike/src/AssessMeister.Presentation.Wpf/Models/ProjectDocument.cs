namespace AssessMeister.Presentation.Wpf.Models;

public sealed class ProjectDocument
{
    private string snapshot = "{}";

    public ProjectDocument(string identity)
    {
        Identity = identity;
    }

    public string Identity { get; }

    public bool IsDirty { get; private set; }

    public string CreateSnapshot() => snapshot;

    public void RestoreFromSnapshot(string persistedSnapshot)
    {
        snapshot = persistedSnapshot;
        IsDirty = false;
    }

    public void MarkDirty()
    {
        IsDirty = true;
    }

    public void MarkSaved()
    {
        IsDirty = false;
    }
}
