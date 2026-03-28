namespace Rulixa.Cli;

internal static class CliMessages
{
    public static string UsageHeader => "使用方法:";

    public static IReadOnlyList<string> UsageLines { get; } =
    [
        "  rulixa scan [--workspace <path>] [--out <path>]",
        "  rulixa resolve-entry --entry <entry> [--workspace <path>]",
        "  rulixa pack --entry <entry> --goal <goal> [--workspace <path>] [--out <path>] [--max-files <n>] [--max-total-lines <n>] [--max-snippets-per-file <n>]"
    ];

    public static string UnknownCommand(string command) => $"未対応のコマンドです: {command}";

    public static string RequiredOption(string optionName) => $"{optionName} は必須です。";

    public static string OutputWritten(string path) => $"出力先: {path}";

    public static string UnexpectedError(string message) => $"エラー: {message}";
}
