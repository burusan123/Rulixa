namespace LegacyServiceLocator;

public sealed class DraftingDialogService
{
    public void Show()
    {
        new DraftingWindow().ShowDialog();
    }
}
