namespace Rulixa.Infrastructure.Quality;

internal sealed class QualityHandoffWarningEvaluator
{
    private static readonly string[] DraftingFamilies =
    [
        "Drafting",
        "Algorithm",
        "Analyzer",
        "Persistence"
    ];

    private static readonly string[] UiNoiseTokens =
    [
        "Overlay",
        "Prompt",
        "Renderer"
    ];

    public IReadOnlyList<HandoffWarningArtifact> Evaluate(IReadOnlyList<QualityCaseArtifact> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);

        var warnings = new List<HandoffWarningArtifact>();
        foreach (var qualityCase in cases)
        {
            AppendDraftingGuidanceWarning(qualityCase, warnings);
            AppendSettingsAndReportNoiseWarning(qualityCase, warnings);
            AppendRawFailureWarning(qualityCase, warnings);
        }

        return warnings
            .OrderBy(static item => item.CaseId, StringComparer.Ordinal)
            .ThenBy(static item => item.Category, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AppendDraftingGuidanceWarning(
        QualityCaseArtifact qualityCase,
        ICollection<HandoffWarningArtifact> warnings)
    {
        if (!qualityCase.Goal.Contains("drafting", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (qualityCase.UnknownGuidance.Any(item => DraftingFamilies.Contains(item.Family, StringComparer.Ordinal)))
        {
            return;
        }

        warnings.Add(new HandoffWarningArtifact(
            CaseId: qualityCase.CaseId,
            Category: "drafting-guidance-family-missing",
            Message: "Drafting 系の handoff 候補に Drafting / Algorithm / Analyzer / Persistence family が含まれていません。"));
    }

    private static void AppendSettingsAndReportNoiseWarning(
        QualityCaseArtifact qualityCase,
        ICollection<HandoffWarningArtifact> warnings)
    {
        var noisyGuidance = qualityCase.UnknownGuidance.FirstOrDefault(static item =>
            item.Family is "Settings" or "Report/Export"
            && item.FirstCandidate is not null
            && UiNoiseTokens.Any(token => item.FirstCandidate.Contains(token, StringComparison.OrdinalIgnoreCase)));

        if (noisyGuidance is null)
        {
            return;
        }

        warnings.Add(new HandoffWarningArtifact(
            CaseId: qualityCase.CaseId,
            Category: "settings-report-ui-noise",
            Message: $"Settings / Report 系の first candidate が UI ノイズで終わっています: {noisyGuidance.FirstCandidate}."));
    }

    private static void AppendRawFailureWarning(
        QualityCaseArtifact qualityCase,
        ICollection<HandoffWarningArtifact> warnings)
    {
        if (string.Equals(qualityCase.Status, "failed", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(qualityCase.FailureReason)
            && qualityCase.UnknownGuidance.Count == 0)
        {
            warnings.Add(new HandoffWarningArtifact(
                CaseId: qualityCase.CaseId,
                Category: "raw-failure-without-guidance",
                Message: "failure reason はありますが、handoff 用の unknown guidance が残っていません。"));
        }
    }
}
