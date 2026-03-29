using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Rulixa.Application.HumanOutputs;

namespace Rulixa.Infrastructure.Rendering;

internal static class VisualOutputHtmlBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Default,
        WriteIndented = false
    };

    public static string Build(VisualOutputDocument document)
    {
        var dataJson = JsonSerializer.Serialize(document, JsonOptions);
        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"ja\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine($"  <title>{document.Title}</title>");
        builder.AppendLine("  <link rel=\"stylesheet\" href=\"app.css\">");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <div class=\"app-shell\">");
        builder.AppendLine("    <aside class=\"nav-pane\">");
        builder.AppendLine("      <div class=\"brand-block\">");
        builder.AppendLine("        <p class=\"eyebrow\">Rulixa Visual Output</p>");
        builder.AppendLine("        <h1 class=\"brand-title\">探索型レビュー</h1>");
        builder.AppendLine("        <p class=\"brand-summary\">Overview から局所 workflow と evidence へ段階的に降ります。</p>");
        builder.AppendLine("      </div>");
        builder.AppendLine("      <label class=\"search-block\">");
        builder.AppendLine("        <span>Search</span>");
        builder.AppendLine("        <input type=\"search\" placeholder=\"view / file / candidate を検索\" data-role=\"search-input\">");
        builder.AppendLine("      </label>");
        builder.AppendLine("      <nav class=\"view-nav\" data-role=\"view-nav\"></nav>");
        builder.AppendLine("      <div class=\"jump-links\">");
        builder.AppendLine("        <button type=\"button\" data-role=\"jump-view\" data-target-view=\"evidence\">Evidence へ移動</button>");
        builder.AppendLine("        <button type=\"button\" data-role=\"jump-view\" data-target-view=\"unknowns\">Unknowns へ移動</button>");
        builder.AppendLine("      </div>");
        builder.AppendLine("    </aside>");
        builder.AppendLine("    <main class=\"main-pane\">");
        builder.AppendLine("      <header class=\"header-summary\" data-role=\"header-summary\"></header>");
        builder.AppendLine("      <section class=\"view-host\" data-role=\"view-host\"></section>");
        builder.AppendLine("    </main>");
        builder.AppendLine("    <aside class=\"inspector-pane\">");
        builder.AppendLine("      <div class=\"inspector-header\">");
        builder.AppendLine("        <p class=\"eyebrow\">Inspector</p>");
        builder.AppendLine("        <h2>選択中の詳細</h2>");
        builder.AppendLine("      </div>");
        builder.AppendLine("      <div class=\"inspector-body\" data-role=\"inspector\"></div>");
        builder.AppendLine("    </aside>");
        builder.AppendLine("  </div>");
        builder.AppendLine($"  <script id=\"rulixa-visual-data\" type=\"application/json\">{dataJson}</script>");
        builder.AppendLine("  <script src=\"app.js\"></script>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }
}
