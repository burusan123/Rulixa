using Rulixa.Domain.Entries;

namespace Rulixa.Domain.Tests.Entries;

public sealed class EntryTests
{
    [Theory]
    [InlineData("file:src/App.xaml.cs", EntryKind.File, "src/App.xaml.cs")]
    [InlineData("symbol:MyApp.ShellViewModel", EntryKind.Symbol, "MyApp.ShellViewModel")]
    [InlineData("auto:ShellView", EntryKind.Auto, "ShellView")]
    public void Parse_ValidInput_ReturnsExpectedEntry(string input, EntryKind expectedKind, string expectedValue)
    {
        var entry = Entry.Parse(input);

        Assert.Equal(expectedKind, entry.Kind);
        Assert.Equal(expectedValue, entry.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("file")]
    [InlineData("unknown:value")]
    public void Parse_InvalidInput_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => Entry.Parse(input));
    }
}
