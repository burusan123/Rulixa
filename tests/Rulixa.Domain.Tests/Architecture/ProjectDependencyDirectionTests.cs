using System.Xml.Linq;

namespace Rulixa.Domain.Tests.Architecture;

public sealed class ProjectDependencyDirectionTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    public static IEnumerable<object[]> ProjectReferenceExpectations()
    {
        yield return ["src/Rulixa.Domain/Rulixa.Domain.csproj", Array.Empty<string>()];
        yield return ["src/Rulixa.Application/Rulixa.Application.csproj", new[] { "Rulixa.Domain" }];
        yield return ["src/Rulixa.Infrastructure/Rulixa.Infrastructure.csproj", new[] { "Rulixa.Application", "Rulixa.Domain" }];
        yield return ["src/Rulixa.Plugin.WpfNet8/Rulixa.Plugin.WpfNet8.csproj", new[] { "Rulixa.Application", "Rulixa.Domain" }];
        yield return ["src/Rulixa.Cli/Rulixa.Cli.csproj", new[] { "Rulixa.Application", "Rulixa.Domain", "Rulixa.Infrastructure", "Rulixa.Plugin.WpfNet8" }];
    }

    [Theory]
    [MemberData(nameof(ProjectReferenceExpectations))]
    public void Projects_OnlyReferenceAllowedInnerLayers(string relativeProjectPath, string[] expectedReferences)
    {
        var document = XDocument.Load(Path.Combine(RepoRoot, relativeProjectPath));

        var actualReferences = document
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(reference.Attribute("Include")?.Value))
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedReferences.OrderBy(static name => name, StringComparer.Ordinal), actualReferences);
    }
}
