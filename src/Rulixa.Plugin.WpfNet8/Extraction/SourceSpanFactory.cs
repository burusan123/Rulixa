using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class SourceSpanFactory
{
    internal static SourceSpan FromLineNumber(int lineNumber) => new(lineNumber, lineNumber);

    internal static SourceSpan FromMatch(string source, int startIndex, int length)
    {
        var startLine = GetLineNumberAt(source, startIndex);
        var endIndex = Math.Max(startIndex, startIndex + Math.Max(0, length) - 1);
        var endLine = GetLineNumberAt(source, endIndex);
        return new SourceSpan(startLine, endLine);
    }

    private static int GetLineNumberAt(string source, int index)
    {
        var boundedIndex = Math.Clamp(index, 0, Math.Max(0, source.Length - 1));
        var lineNumber = 1;
        for (var currentIndex = 0; currentIndex < boundedIndex; currentIndex++)
        {
            if (source[currentIndex] == '\n')
            {
                lineNumber++;
            }
        }

        return lineNumber;
    }
}
