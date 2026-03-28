namespace Rulixa.Domain.Entries;

public sealed record Entry(EntryKind Kind, string Value)
{
    public static Entry Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("entry は必須です。", nameof(input));
        }

        var separatorIndex = input.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == input.Length - 1)
        {
            throw new ArgumentException("entry は 'file:<path>' 形式で指定してください。", nameof(input));
        }

        var kindToken = input[..separatorIndex].Trim();
        var value = input[(separatorIndex + 1)..].Trim();

        return kindToken.ToLowerInvariant() switch
        {
            "file" => new Entry(EntryKind.File, value),
            "symbol" => new Entry(EntryKind.Symbol, value),
            "auto" => new Entry(EntryKind.Auto, value),
            _ => throw new ArgumentException($"未対応の entry 種別です: {kindToken}", nameof(input))
        };
    }

    public override string ToString() => $"{Kind.ToString().ToLowerInvariant()}:{Value}";
}

public enum EntryKind
{
    File,
    Symbol,
    Auto
}
