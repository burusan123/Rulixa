namespace Rulixa.Infrastructure.Quality;

public sealed class Phase5QualityGateEvaluator
{
    public Phase5QualityGateArtifact Evaluate(IReadOnlyList<Phase5KpiCaseArtifact> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);

        var executed = cases.Where(static item => !string.Equals(item.Status, "skipped", StringComparison.OrdinalIgnoreCase)).ToArray();
        var rootCases = executed.Where(item => item.Tags.Contains("root-case", StringComparer.OrdinalIgnoreCase)).ToArray();
        var deterministicCases = executed.Where(item => item.Tags.Contains("deterministic", StringComparer.OrdinalIgnoreCase)).ToArray();
        var weakSignalCases = executed.Where(item => item.Tags.Contains("weak-signal", StringComparer.OrdinalIgnoreCase)).ToArray();

        var crashFreeRate = CalculateRate(executed, static item => item.CrashFree == true);
        var packSuccessRate = CalculateRate(rootCases, static item => item.PackSuccess == true);
        var partialPackRate = CalculateRate(executed, static item => item.PartialPack == true);
        var deterministicRate = CalculateRate(deterministicCases, static item => item.Deterministic == true);
        var falseConfidenceRate = CalculateRate(weakSignalCases, static item => item.FalseConfidenceDetected == true);

        var failedChecks = new List<string>();
        if (executed.Length == 0)
        {
            failedChecks.Add("executed_cases_missing");
        }

        if (crashFreeRate < 1d)
        {
            failedChecks.Add("crash_free_rate");
        }

        if (rootCases.Length == 0 || packSuccessRate < 1d)
        {
            failedChecks.Add("pack_success_rate");
        }

        if (deterministicCases.Length == 0 || deterministicRate < 1d)
        {
            failedChecks.Add("deterministic_rate");
        }

        if (weakSignalCases.Length == 0 || falseConfidenceRate > 0d)
        {
            failedChecks.Add("false_confidence_rate");
        }

        return new Phase5QualityGateArtifact(
            Passed: failedChecks.Count == 0,
            CrashFreeRate: crashFreeRate,
            PackSuccessRate: packSuccessRate,
            PartialPackRate: partialPackRate,
            DeterministicRate: deterministicRate,
            FalseConfidenceRate: falseConfidenceRate,
            FailedChecks: failedChecks);
    }

    private static double CalculateRate(
        IReadOnlyCollection<Phase5KpiCaseArtifact> cases,
        Func<Phase5KpiCaseArtifact, bool> predicate)
    {
        if (cases.Count == 0)
        {
            return 0d;
        }

        var matched = cases.Count(predicate);
        return matched / (double)cases.Count;
    }
}
