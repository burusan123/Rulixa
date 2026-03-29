using Rulixa.Application.HumanOutputs;
using Rulixa.Application.Ports;

namespace Rulixa.Infrastructure.Rendering;

public sealed class VisualOutputRenderer : IVisualOutputRenderer
{
    public async Task<VisualOutputRenderResult> RenderAsync(
        VisualOutputDocument document,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var indexPath = Path.Combine(fullOutputDirectory, "index.html");
        var cssPath = Path.Combine(fullOutputDirectory, "app.css");
        var javaScriptPath = Path.Combine(fullOutputDirectory, "app.js");

        await File.WriteAllTextAsync(indexPath, VisualOutputHtmlBuilder.Build(document), cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(cssPath, VisualOutputAssets.BuildCss(), cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(javaScriptPath, VisualOutputAssets.BuildJavaScript(), cancellationToken).ConfigureAwait(false);

        return new VisualOutputRenderResult(indexPath, cssPath, javaScriptPath);
    }
}
