using System.Text;
using Rulixa.Application.HumanOutputs;
using Rulixa.Application.Ports;

namespace Rulixa.Infrastructure.Rendering;

public sealed class HumanOutputMarkdownRenderer : IHumanOutputRenderer
{
    public string Render(HumanOutputDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var builder = new StringBuilder();
        builder.AppendLine($"# {document.Title}");
        builder.AppendLine();
        builder.AppendLine($"- mode: `{document.Mode.ToString().ToLowerInvariant()}`");

        foreach (var section in document.Sections)
        {
            builder.AppendLine();
            builder.AppendLine($"## {section.Title}");

            foreach (var paragraph in section.Paragraphs)
            {
                builder.AppendLine();
                builder.AppendLine(paragraph);
            }

            if (section.BulletPoints.Count == 0)
            {
                builder.AppendLine();
                builder.AppendLine("- なし");
                continue;
            }

            builder.AppendLine();
            foreach (var bulletPoint in section.BulletPoints)
            {
                builder.AppendLine($"- {bulletPoint}");
            }
        }

        return builder.ToString();
    }
}
