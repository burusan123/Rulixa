using Rulixa.Domain.Scanning;

namespace Rulixa.Domain.Tests.Scanning;

public sealed class SourceSpanTests
{
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(5, 8, 4)]
    public void Create_ComputesLengthInLines(int startLine, int endLine, int expectedLength)
    {
        var span = new SourceSpan(startLine, endLine);

        Assert.Equal(startLine, span.StartLine);
        Assert.Equal(endLine, span.EndLine);
        Assert.Equal(expectedLength, span.LengthInLines);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(3, 2)]
    public void Create_RejectsInvalidRange(int startLine, int endLine)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceSpan(startLine, endLine));
    }
}
