using Rulixa.Domain.Scanning;
using Rulixa.Plugin.WpfNet8.Discovery;

namespace Rulixa.Plugin.WpfNet8.Scanning;

internal sealed class ProjectSummaryBuilder
{
    internal ProjectSummary Build(
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
}
