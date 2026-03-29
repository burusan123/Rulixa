using Rulixa.Application.HumanOutputs;
using Rulixa.Application.Ports;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.UseCases;

public sealed class RenderVisualOutputUseCase
{
    private readonly IVisualOutputRenderer renderer;
    private readonly HumanOutputFactAnalyzer factAnalyzer = new();

    public RenderVisualOutputUseCase(IVisualOutputRenderer renderer)
    {
        this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public Task<VisualOutputRenderResult> ExecuteAsync(
        ContextPack contextPack,
        WorkspaceScanResult scanResult,
        string outputDirectory,
        VisualOutputRenderOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextPack);
        ArgumentNullException.ThrowIfNull(scanResult);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(options);

        var facts = factAnalyzer.Analyze(
            contextPack,
            scanResult,
            new HumanOutputRenderOptions(options.EvidenceDirectory));
        var document = VisualOutputDocumentFactory.Create(contextPack, scanResult, facts);
        return renderer.RenderAsync(document, outputDirectory, cancellationToken);
    }
}
