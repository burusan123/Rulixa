using System.Text.Json;
using System.Text.Json.Serialization;
using Rulixa.Application.Ports;
using Rulixa.Application.UseCases;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Infrastructure.FileSystem;
using Rulixa.Infrastructure.Rendering;
using Rulixa.Infrastructure.Resolution;
using Rulixa.Plugin.WpfNet8.Extraction;
using Rulixa.Plugin.WpfNet8.Scanning;

namespace Rulixa.Cli;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                WriteUsage();
                return 1;
            }

            var workspaceFileSystem = new WorkspaceFileSystem();
            var workspaceScanner = new WpfNet8WorkspaceScanner(workspaceFileSystem);
            var entryResolver = new ScanBackedEntryResolver();
            var contractExtractor = new WpfNet8ContractExtractor(workspaceFileSystem);
            var renderer = new MarkdownContextPackRenderer();

            var scanWorkspaceUseCase = new ScanWorkspaceUseCase(workspaceScanner);
            var resolveEntryUseCase = new ResolveEntryUseCase(entryResolver);
            var buildContextPackUseCase = new BuildContextPackUseCase(contractExtractor);

            return args[0].ToLowerInvariant() switch
            {
                "scan" => await RunScanAsync(args[1..], scanWorkspaceUseCase).ConfigureAwait(false),
                "resolve-entry" => await RunResolveEntryAsync(args[1..], scanWorkspaceUseCase, resolveEntryUseCase).ConfigureAwait(false),
                "pack" => await RunPackAsync(args[1..], scanWorkspaceUseCase, resolveEntryUseCase, buildContextPackUseCase, renderer).ConfigureAwait(false),
                _ => Fail(CliMessages.UnknownCommand(args[0]))
            };
        }
        catch (ArgumentException exception)
        {
            return Fail(exception.Message);
        }
        catch (Exception exception)
        {
            return Fail(CliMessages.UnexpectedError(exception.Message), includeUsage: false);
        }
    }

    private static async Task<int> RunScanAsync(string[] args, ScanWorkspaceUseCase useCase)
    {
        var workspace = GetOption(args, "--workspace") ?? Directory.GetCurrentDirectory();
        var outputPath = GetOption(args, "--out");
        var result = await useCase.ExecuteAsync(workspace).ConfigureAwait(false);
        var json = JsonSerializer.Serialize(result, JsonOptions);
        return await WriteOutputAsync(json, outputPath).ConfigureAwait(false);
    }

    private static async Task<int> RunResolveEntryAsync(
        string[] args,
        ScanWorkspaceUseCase scanUseCase,
        ResolveEntryUseCase resolveUseCase)
    {
        var workspace = GetOption(args, "--workspace") ?? Directory.GetCurrentDirectory();
        var entryText = GetRequiredOption(args, "--entry");
        var scanResult = await scanUseCase.ExecuteAsync(workspace).ConfigureAwait(false);
        var resolved = await resolveUseCase.ExecuteAsync(Entry.Parse(entryText), scanResult).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(resolved, JsonOptions));
        return 0;
    }

    private static async Task<int> RunPackAsync(
        string[] args,
        ScanWorkspaceUseCase scanUseCase,
        ResolveEntryUseCase resolveUseCase,
        BuildContextPackUseCase packUseCase,
        IContextPackRenderer renderer)
    {
        var workspace = GetOption(args, "--workspace") ?? Directory.GetCurrentDirectory();
        var entryText = GetRequiredOption(args, "--entry");
        var goal = GetRequiredOption(args, "--goal");
        var outputPath = GetOption(args, "--out");
        var budget = new Budget(
            MaxFiles: GetOptionAsInt(args, "--max-files", 8),
            MaxTotalLines: GetOptionAsInt(args, "--max-total-lines", 1600),
            MaxSnippetsPerFile: GetOptionAsInt(args, "--max-snippets-per-file", 3));

        var scanResult = await scanUseCase.ExecuteAsync(workspace).ConfigureAwait(false);
        var entry = Entry.Parse(entryText);
        var resolvedEntry = await resolveUseCase.ExecuteAsync(entry, scanResult).ConfigureAwait(false);
        var contextPack = await packUseCase.ExecuteAsync(workspace, scanResult, entry, resolvedEntry, goal, budget).ConfigureAwait(false);
        var markdown = renderer.Render(contextPack);
        return await WriteOutputAsync(markdown, outputPath).ConfigureAwait(false);
    }

    private static string GetRequiredOption(IReadOnlyList<string> args, string optionName) =>
        GetOption(args, optionName) ?? throw new ArgumentException(CliMessages.RequiredOption(optionName));

    private static string? GetOption(IReadOnlyList<string> args, string optionName)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static int GetOptionAsInt(IReadOnlyList<string> args, string optionName, int defaultValue)
    {
        var rawValue = GetOption(args, optionName);
        return int.TryParse(rawValue, out var parsed) ? parsed : defaultValue;
    }

    private static async Task<int> WriteOutputAsync(string content, string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            Console.WriteLine(content);
            return 0;
        }

        var fullPath = Path.GetFullPath(outputPath);
        var directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllTextAsync(fullPath, content).ConfigureAwait(false);
        Console.WriteLine(CliMessages.OutputWritten(fullPath));
        return 0;
    }

    private static int Fail(string message, bool includeUsage = true)
    {
        Console.Error.WriteLine(message);
        if (includeUsage)
        {
            WriteUsage();
        }

        return 1;
    }

    private static void WriteUsage()
    {
        Console.Error.WriteLine(CliMessages.UsageHeader);
        foreach (var usageLine in CliMessages.UsageLines)
        {
            Console.Error.WriteLine(usageLine);
        }
    }
}
