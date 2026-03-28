namespace Rulixa.Domain.Packs;

public sealed record Budget(
    int MaxFiles = 8,
    int MaxTotalLines = 1600,
    int MaxSnippetsPerFile = 3)
{
    public static Budget Default { get; } = new();
}
