using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Discovery;

internal static class ProjectFileDiscovery
{
    public static IReadOnlyList<string> FindSolutionFiles(IEnumerable<string> files) =>
        files.Where(static path => path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static IReadOnlyList<string> FindProjectFiles(IEnumerable<string> files) =>
        files.Where(static path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static ScanFileKind DetectKind(string relativePath)
    {
        var fileName = Path.GetFileName(relativePath);
        if (relativePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            return ScanFileKind.Solution;
        }

        if (relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return ScanFileKind.Project;
        }

        if (string.Equals(fileName, "App.xaml", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "App.xaml.cs", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "MainWindow.xaml.cs", StringComparison.OrdinalIgnoreCase))
        {
            return ScanFileKind.Startup;
        }

        if (relativePath.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase))
        {
            return ScanFileKind.CodeBehind;
        }

        if (relativePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
        {
            return ScanFileKind.Xaml;
        }

        if (relativePath.Contains("/ViewModels/", StringComparison.OrdinalIgnoreCase))
        {
            return ScanFileKind.ViewModel;
        }

        if (relativePath.Contains("/Services/", StringComparison.OrdinalIgnoreCase))
        {
            return ScanFileKind.Service;
        }

        if (string.Equals(fileName, "DelegateCommand.cs", StringComparison.OrdinalIgnoreCase))
        {
            return ScanFileKind.CommandSupport;
        }

        if (relativePath.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
            || relativePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return ScanFileKind.Config;
        }

        return relativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            ? ScanFileKind.CSharp
            : ScanFileKind.Unknown;
    }

    public static IReadOnlyList<string> DetectTags(string relativePath, ScanFileKind kind)
    {
        var tags = new List<string>();

        if (kind is ScanFileKind.Xaml or ScanFileKind.CodeBehind)
        {
            tags.Add("view");
        }

        if (kind == ScanFileKind.ViewModel)
        {
            tags.Add("viewmodel");
        }

        if (kind == ScanFileKind.Service)
        {
            tags.Add("service");
        }

        if (relativePath.Contains("Shell", StringComparison.OrdinalIgnoreCase))
        {
            tags.Add("shell");
        }

        return tags;
    }
}
