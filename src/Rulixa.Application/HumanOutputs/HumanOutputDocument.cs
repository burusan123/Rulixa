namespace Rulixa.Application.HumanOutputs;

public sealed record HumanOutputDocument(
    string Title,
    HumanOutputMode Mode,
    IReadOnlyList<HumanOutputSection> Sections);

public sealed record HumanOutputSection(
    string Title,
    IReadOnlyList<string> Paragraphs,
    IReadOnlyList<string> BulletPoints);
