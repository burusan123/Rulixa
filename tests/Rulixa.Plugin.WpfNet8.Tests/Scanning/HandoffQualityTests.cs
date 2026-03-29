using Rulixa.Infrastructure.Quality;

namespace Rulixa.Plugin.WpfNet8.Tests.Scanning;

public sealed class HandoffQualityTests
{
    [Fact]
    public async Task ExecuteCaseAsync_ForSyntheticLegacyRoot_ProducesRepresentativeChainsAndGuidedUnknown()
    {
        var definition = Assert.Single(
            QualityArtifactSupport.CreateSyntheticCaseDefinitions(),
            static item => item.CaseId == "service-locator-root");

        var result = await QualityArtifactSupport.ExecuteCaseAsync(definition);

        Assert.Equal("passed", result.Status);
        Assert.True(result.PackSuccess);
        Assert.True(result.RepresentativeChainCount > 0);
        Assert.True(result.HasUnknownGuidance);
        Assert.Equal("hit", result.HandoffOutcome);
        Assert.Contains("Drafting", result.HandoffExpectedFamilies ?? []);
        Assert.Contains("Report/Export", result.HandoffObservedFamilies ?? []);
        Assert.Contains(result.UnknownGuidance, static item =>
            item.CandidateCount > 0
            && item.Family is "Drafting" or "Algorithm" or "Analyzer" or "Persistence");
    }

    [Fact]
    public async Task ExecuteCaseAsync_ForSyntheticModernRoot_ProducesRepresentativeChainsWithoutFalseConfidence()
    {
        var definition = Assert.Single(
            QualityArtifactSupport.CreateSyntheticCaseDefinitions(),
            static item => item.CaseId == "modern-sibling-root");

        var result = await QualityArtifactSupport.ExecuteCaseAsync(definition);

        Assert.Equal("passed", result.Status);
        Assert.True(result.PackSuccess);
        Assert.True(result.RepresentativeChainCount > 0);
        Assert.False(result.FalseConfidenceDetected);
        Assert.Equal("hit", result.HandoffOutcome);
        Assert.Contains("3D", result.HandoffObservedFamilies ?? []);
        Assert.Contains("Settings", result.HandoffObservedFamilies ?? []);
    }

    [Fact]
    public async Task ExecuteCaseAsync_ForWeakSignalCorpus_PrefersUnknownGuidanceOverRepresentativeGuess()
    {
        var definition = Assert.Single(
            QualityArtifactSupport.CreateSyntheticCaseDefinitions(),
            static item => item.CaseId == "weak-signal-root");

        var result = await QualityArtifactSupport.ExecuteCaseAsync(definition);

        Assert.Equal("passed", result.Status);
        Assert.True(result.HasUnknownGuidance);
        Assert.False(result.FalseConfidenceDetected);
        Assert.Equal("unknown", result.HandoffOutcome);
        Assert.Contains(result.UnknownGuidance, static item =>
            item.CandidateCount > 0
            && item.FirstCandidate is not null);
    }
}
