namespace Rulixa.Domain.Diagnostics;

public sealed record Diagnostic(
    string Code,
    string Message,
    string? FilePath,
    DiagnosticSeverity Severity,
    IReadOnlyList<string> Candidates);

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}
