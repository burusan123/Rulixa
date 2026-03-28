using System.Text.RegularExpressions;
using Rulixa.Application.Ports;
using Rulixa.Domain.Scanning;
using Rulixa.Plugin.WpfNet8.Discovery;
using Rulixa.Plugin.WpfNet8.Extraction;

namespace Rulixa.Plugin.WpfNet8.Scanning;

public sealed class WpfNet8WorkspaceScanner : IWorkspaceScanner
{
    private static readonly Regex NamespaceRegex = new(@"namespace\s+(?<namespace>[A-Za-z0-9_\.]+)\s*[;{]", RegexOptions.Compiled);
    private static readonly Regex ClassRegex = new(@"(?:(?:public|internal)\s+)?(?:sealed\s+|partial\s+|abstract\s+)?class\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex InterfaceRegex = new(@"(?:(?:public|internal)\s+)?interface\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex CommandPropertyRegex = new(@"public\s+(?<type>[A-Za-z0-9_<>\.\?,]+)\s+(?<name>[A-Za-z_]\w*Command)\s*\{\s*get;", RegexOptions.Compiled);
    private static readonly Regex XamlClassRegex = new(@"x:Class=""(?<class>[^""]+)""", RegexOptions.Compiled);

    private readonly IWorkspaceFileSystem fileSystem;
    private readonly XamlViewModelBindingExtractor bindingExtractor = new();
    private readonly CommandContractExtractor commandExtractor = new();
    private readonly DialogActivationExtractor dialogExtractor = new();
    private readonly ServiceRegistrationExtractor registrationExtractor = new();

    public WpfNet8WorkspaceScanner(IWorkspaceFileSystem fileSystem)
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public async Task<WorkspaceScanResult> ScanAsync(string workspaceRoot, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);

        var allFiles = await fileSystem.EnumerateFilesAsync(workspaceRoot, cancellationToken).ConfigureAwait(false);
        var relevantFiles = allFiles
            .Select(path => Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/'))
            .Where(static path => IsRelevant(path))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var fileContents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var scanFiles = new List<ScanFile>();

        foreach (var relativePath in relevantFiles)
        {
            var absolutePath = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var content = await fileSystem.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
            fileContents[relativePath] = content;

            var kind = ProjectFileDiscovery.DetectKind(relativePath);
            var hash = await fileSystem.ComputeSha256Async(absolutePath, cancellationToken).ConfigureAwait(false);
            var lineCount = CountLines(content);
            scanFiles.Add(new ScanFile(relativePath, kind, ResolveProjectName(relativePath), hash, lineCount, ProjectFileDiscovery.DetectTags(relativePath, kind)));
        }

        var symbols = ExtractSymbols(fileContents);
        var bindings = bindingExtractor.Extract(fileContents, symbols);
        var commands = commandExtractor.Extract(fileContents, scanFiles);
        var windowActivations = dialogExtractor.Extract(fileContents);
        var serviceRegistrations = registrationExtractor.Extract(fileContents);

        return new WorkspaceScanResult(
            "phase1.v1",
            Path.GetFullPath(workspaceRoot),
            DateTimeOffset.UtcNow,
            BuildProjectSummary(scanFiles, fileContents, bindings),
            scanFiles,
            symbols,
            bindings,
            commands,
            windowActivations,
            serviceRegistrations,
            []);
    }

    private static ProjectSummary BuildProjectSummary(
        IReadOnlyList<ScanFile> scanFiles,
        IReadOnlyDictionary<string, string> fileContents,
        IReadOnlyList<ViewModelBinding> bindings)
    {
        var solutionFiles = ProjectFileDiscovery.FindSolutionFiles(scanFiles.Select(static file => file.Path));
        var projectFiles = ProjectFileDiscovery.FindProjectFiles(scanFiles.Select(static file => file.Path));
        var targetFrameworks = projectFiles
            .Select(project => ExtractTargetFramework(fileContents.TryGetValue(project, out var content) ? content : string.Empty))
            .Where(static framework => !string.IsNullOrWhiteSpace(framework))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var usesWpf = projectFiles.Any(project =>
            fileContents.TryGetValue(project, out var content)
            && content.Contains("<UseWPF>true</UseWPF>", StringComparison.OrdinalIgnoreCase));

        var entryPoints = scanFiles
            .Where(static file => file.Kind == ScanFileKind.Startup)
            .Select(static file => file.Path)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var rootViewModels = bindings
            .Where(static binding => binding.BindingKind == ViewModelBindingKind.RootDataContext)
            .Select(static binding => binding.ViewModelSymbol)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static symbol => symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ProjectSummary(solutionFiles, projectFiles, targetFrameworks, usesWpf, entryPoints, rootViewModels);
    }

    private static IReadOnlyList<ScanSymbol> ExtractSymbols(IReadOnlyDictionary<string, string> fileContents)
    {
        var symbols = new List<ScanSymbol>();
        var nextId = 1;

        foreach (var (path, content) in fileContents)
        {
            if (path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                var xamlClass = XamlClassRegex.Match(content).Groups["class"].Value;
                if (!string.IsNullOrWhiteSpace(xamlClass))
                {
                    symbols.Add(new ScanSymbol(
                        $"sym-{nextId++:0000}",
                        xamlClass.EndsWith("Window", StringComparison.Ordinal) ? SymbolKind.Window : SymbolKind.Class,
                        xamlClass,
                        xamlClass.Split('.').Last(),
                        path,
                        1,
                        CountLines(content),
                        ["xaml"]));
                }

                continue;
            }

            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var namespaceName = NamespaceRegex.Match(content).Groups["namespace"].Value;
            foreach (Match match in InterfaceRegex.Matches(content))
            {
                var name = match.Groups["name"].Value;
                symbols.Add(new ScanSymbol($"sym-{nextId++:0000}", SymbolKind.Interface, Qualify(namespaceName, name), name, path, 1, CountLines(content), []));
            }

            foreach (Match match in ClassRegex.Matches(content))
            {
                var name = match.Groups["name"].Value;
                symbols.Add(new ScanSymbol(
                    $"sym-{nextId++:0000}",
                    name.EndsWith("Window", StringComparison.Ordinal) ? SymbolKind.Window : SymbolKind.Class,
                    Qualify(namespaceName, name),
                    name,
                    path,
                    1,
                    CountLines(content),
                    []));
            }

            foreach (Match match in CommandPropertyRegex.Matches(content))
            {
                var name = match.Groups["name"].Value;
                symbols.Add(new ScanSymbol($"sym-{nextId++:0000}", SymbolKind.Command, Qualify(namespaceName, name), name, path, 1, CountLines(content), ["command"]));
            }
        }

        return symbols
            .OrderBy(static symbol => symbol.QualifiedName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static symbol => symbol.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ExtractTargetFramework(string content)
    {
        const string startToken = "<TargetFramework>";
        const string endToken = "</TargetFramework>";
        var startIndex = content.IndexOf(startToken, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
        {
            return string.Empty;
        }

        startIndex += startToken.Length;
        var endIndex = content.IndexOf(endToken, startIndex, StringComparison.OrdinalIgnoreCase);
        if (endIndex < 0)
        {
            return string.Empty;
        }

        return content[startIndex..endIndex].Trim();
    }

    private static bool IsRelevant(string relativePath) =>
        relativePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
        || relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
        || relativePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
        || relativePath.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)
        || relativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
        || relativePath.EndsWith(".props", StringComparison.OrdinalIgnoreCase);

    private static int CountLines(string content) => content.Split('\n').Length;

    private static string Qualify(string namespaceName, string name) =>
        string.IsNullOrWhiteSpace(namespaceName) ? name : $"{namespaceName}.{name}";

    private static string ResolveProjectName(string relativePath)
    {
        var parts = relativePath.Split('/');
        return parts.Length >= 2 ? parts[1] : parts[0];
    }
}
