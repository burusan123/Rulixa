using Rulixa.Application.HumanOutputs;

namespace Rulixa.Application.Ports;

public interface IVisualOutputRenderer
{
    Task<VisualOutputRenderResult> RenderAsync(
        VisualOutputDocument document,
        string outputDirectory,
        CancellationToken cancellationToken = default);
}
