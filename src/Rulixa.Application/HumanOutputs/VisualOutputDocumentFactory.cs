using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Application.HumanOutputs;

internal static class VisualOutputDocumentFactory
{
    public static VisualOutputDocument Create(
        ContextPack contextPack,
        WorkspaceScanResult scanResult,
        HumanOutputFactSet facts)
    {
        var inspectorItems = VisualOutputInspectorFactory.Create(contextPack, scanResult, facts);
        return new VisualOutputDocument(
            Title: "Rulixa Visual Output",
            Header: VisualOutputFormatting.BuildHeader(facts),
            Views: VisualOutputViewFactory.Create(contextPack, facts),
            InspectorItems: inspectorItems,
            InitialInspectorId: inspectorItems.Keys.FirstOrDefault());
    }
}
