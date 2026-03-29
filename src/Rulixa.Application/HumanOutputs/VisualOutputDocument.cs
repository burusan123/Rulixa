namespace Rulixa.Application.HumanOutputs;

public sealed record VisualOutputDocument(
    string Title,
    VisualHeaderSummary Header,
    IReadOnlyList<VisualView> Views,
    IReadOnlyDictionary<string, VisualInspectorItem> InspectorItems,
    string? InitialInspectorId);

public sealed record VisualHeaderSummary(
    string RootEntry,
    string Goal,
    string ResolvedKind,
    string? ResolvedPath,
    string? ResolvedSymbol,
    string? SystemSummary,
    IReadOnlyList<string> CenterStates,
    IReadOnlyList<string> NextCandidates,
    string PrimaryUnknown);

public sealed record VisualView(
    string Id,
    string Label,
    string Summary,
    IReadOnlyList<VisualSection> Sections);

public sealed record VisualSection(
    string Id,
    string Title,
    string Kind,
    bool Collapsible,
    IReadOnlyList<VisualItem> Items,
    VisualGraph? Graph);

public sealed record VisualItem(
    string Id,
    string Title,
    string Summary,
    string SearchText,
    string? InspectorId,
    IReadOnlyList<string> Meta);

public sealed record VisualGraph(
    string Title,
    IReadOnlyList<VisualGraphNode> Nodes,
    IReadOnlyList<VisualGraphEdge> Edges);

public sealed record VisualGraphNode(
    string Id,
    string Label,
    bool Emphasized);

public sealed record VisualGraphEdge(
    string From,
    string To,
    string Label);

public sealed record VisualInspectorItem(
    string Id,
    string Title,
    string Category,
    IReadOnlyList<string> Facts,
    string? FilePath,
    string? Symbol,
    string? Snippet,
    string? Reason,
    IReadOnlyList<string> Candidates);
