namespace Rulixa.Infrastructure.Rendering;

internal static class VisualOutputAssets
{
    public static string BuildCss() => VisualOutputCssAsset.Content;

    public static string BuildJavaScript() => VisualOutputJavaScriptAsset.Content;
}
