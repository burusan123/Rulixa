namespace LegacyServiceLocator;

public sealed class AppServices
{
    public static AppServices Current { get; } = new();

    public T Get<T>() where T : new()
    {
        return new T();
    }
}
