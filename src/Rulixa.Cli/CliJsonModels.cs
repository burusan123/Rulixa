using System.Text.Json.Serialization;
using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;
using Rulixa.Domain.Scanning;

namespace Rulixa.Cli;

internal static class CliJsonModels
{
    internal static ScanResultDto FromScanResult(WorkspaceScanResult scanResult) =>
        new(
            scanResult.SchemaVersion,
            scanResult.WorkspaceRoot.Replace('\\', '/'),
            scanResult.GeneratedAtUtc.UtcDateTime.ToString("O"),
            ProjectSummaryDto.FromDomain(scanResult.ProjectSummary),
            scanResult.Files.Select(ScanFileDto.FromDomain).ToArray(),
            scanResult.Symbols.Select(ScanSymbolDto.FromDomain).ToArray(),
            scanResult.ViewModelBindings.Select(ViewModelBindingDto.FromDomain).ToArray(),
            scanResult.NavigationTransitions.Select(NavigationTransitionDto.FromDomain).ToArray(),
            scanResult.Commands.Select(CommandBindingDto.FromDomain).ToArray(),
            scanResult.WindowActivations.Select(WindowActivationDto.FromDomain).ToArray(),
            scanResult.ServiceRegistrations.Select(ServiceRegistrationDto.FromDomain).ToArray(),
            scanResult.Diagnostics.Select(DiagnosticDto.FromDomain).ToArray());

    internal static ResolvedEntryDto FromResolvedEntry(ResolvedEntry resolvedEntry) =>
        new(
            resolvedEntry.Input,
            ToToken(resolvedEntry.ResolvedKind),
            NormalizePath(resolvedEntry.ResolvedPath),
            resolvedEntry.Symbol,
            ToToken(resolvedEntry.Confidence),
            resolvedEntry.Candidates.Select(ResolvedCandidateDto.FromDomain).ToArray());

    private static string? NormalizePath(string? path) => path?.Replace('\\', '/');

    private static string ToToken(ResolvedEntryKind kind) => kind switch
    {
        ResolvedEntryKind.File => "file",
        ResolvedEntryKind.Symbol => "symbol",
        ResolvedEntryKind.Unresolved => "unresolved",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static string ToToken(ConfidenceLevel confidence) => confidence switch
    {
        ConfidenceLevel.Low => "low",
        ConfidenceLevel.Medium => "medium",
        ConfidenceLevel.High => "high",
        _ => throw new ArgumentOutOfRangeException(nameof(confidence), confidence, null)
    };

    internal sealed record ScanResultDto(
        string SchemaVersion,
        string WorkspaceRoot,
        string GeneratedAtUtc,
        ProjectSummaryDto ProjectSummary,
        IReadOnlyList<ScanFileDto> Files,
        IReadOnlyList<ScanSymbolDto> Symbols,
        IReadOnlyList<ViewModelBindingDto> ViewModelBindings,
        IReadOnlyList<NavigationTransitionDto> NavigationTransitions,
        IReadOnlyList<CommandBindingDto> Commands,
        IReadOnlyList<WindowActivationDto> WindowActivations,
        IReadOnlyList<ServiceRegistrationDto> ServiceRegistrations,
        IReadOnlyList<DiagnosticDto> Diagnostics);

    internal sealed record ProjectSummaryDto(
        IReadOnlyList<string> SolutionFiles,
        IReadOnlyList<string> ProjectFiles,
        IReadOnlyList<string> TargetFrameworks,
        bool UsesWpf,
        IReadOnlyList<string> EntryPoints,
        IReadOnlyList<string> RootViewModels)
    {
        internal static ProjectSummaryDto FromDomain(ProjectSummary summary) =>
            new(
                summary.SolutionFiles.Select(NormalizePath).OfType<string>().ToArray(),
                summary.ProjectFiles.Select(NormalizePath).OfType<string>().ToArray(),
                summary.TargetFrameworks,
                summary.UsesWpf,
                summary.EntryPoints.Select(NormalizePath).OfType<string>().ToArray(),
                summary.RootViewModels);
    }

    internal sealed record ScanFileDto(
        string Path,
        string Kind,
        string Project,
        string Hash,
        int LineCount,
        IReadOnlyList<string> Tags)
    {
        internal static ScanFileDto FromDomain(ScanFile file) =>
            new(
                NormalizePath(file.Path)!,
                file.Kind switch
                {
                    ScanFileKind.Unknown => "unknown",
                    ScanFileKind.Solution => "solution",
                    ScanFileKind.Project => "project",
                    ScanFileKind.Startup => "startup",
                    ScanFileKind.Xaml => "xaml",
                    ScanFileKind.CodeBehind => "codebehind",
                    ScanFileKind.ViewModel => "viewmodel",
                    ScanFileKind.Service => "service",
                    ScanFileKind.Config => "config",
                    ScanFileKind.CommandSupport => "command-support",
                    ScanFileKind.CSharp => "csharp",
                    _ => throw new ArgumentOutOfRangeException(nameof(file.Kind), file.Kind, null)
                },
                file.Project,
                $"sha256:{file.Hash}",
                file.LineCount,
                file.Tags);
    }

    internal sealed record ScanSymbolDto(
        string Id,
        string Kind,
        string QualifiedName,
        string DisplayName,
        string FilePath,
        int StartLine,
        int EndLine,
        IReadOnlyList<string> Tags)
    {
        internal static ScanSymbolDto FromDomain(ScanSymbol symbol) =>
            new(
                symbol.Id,
                symbol.Kind switch
                {
                    SymbolKind.Class => "class",
                    SymbolKind.Method => "method",
                    SymbolKind.Property => "property",
                    SymbolKind.Command => "command",
                    SymbolKind.Interface => "interface",
                    SymbolKind.Window => "window",
                    _ => throw new ArgumentOutOfRangeException(nameof(symbol.Kind), symbol.Kind, null)
                },
                symbol.QualifiedName,
                symbol.DisplayName,
                NormalizePath(symbol.FilePath)!,
                symbol.StartLine,
                symbol.EndLine,
                symbol.Tags);
    }

    internal sealed record SourceSpanDto(int StartLine, int EndLine)
    {
        internal static SourceSpanDto FromDomain(SourceSpan sourceSpan) => new(sourceSpan.StartLine, sourceSpan.EndLine);
    }

    internal sealed record ViewModelBindingDto(
        string ViewPath,
        string ViewSymbol,
        string ViewModelSymbol,
        string BindingKind,
        string SourcePath,
        SourceSpanDto SourceSpan,
        string Confidence,
        IReadOnlyList<string> Candidates)
    {
        internal static ViewModelBindingDto FromDomain(ViewModelBinding binding) =>
            new(
                NormalizePath(binding.ViewPath)!,
                binding.ViewSymbol,
                binding.ViewModelSymbol,
                binding.BindingKind switch
                {
                    ViewModelBindingKind.RootDataContext => "root-data-context",
                    ViewModelBindingKind.ViewDataContext => "view-data-context",
                    ViewModelBindingKind.DataTemplate => "data-template",
                    _ => throw new ArgumentOutOfRangeException(nameof(binding.BindingKind), binding.BindingKind, null)
                },
                NormalizePath(binding.SourcePath)!,
                SourceSpanDto.FromDomain(binding.SourceSpan),
                ToToken(binding.Confidence),
                binding.Candidates);
    }

    internal sealed record NavigationTransitionDto(
        string ViewModelSymbol,
        string SourceFilePath,
        string UpdateMethodName,
        string? SelectedItemPropertyName,
        string? CurrentPagePropertyName,
        string UpdateExpressionSummary,
        SourceSpanDto SourceSpan)
    {
        internal static NavigationTransitionDto FromDomain(NavigationTransition transition) =>
            new(
                transition.ViewModelSymbol,
                NormalizePath(transition.SourceFilePath)!,
                transition.UpdateMethodName,
                transition.SelectedItemPropertyName,
                transition.CurrentPagePropertyName,
                transition.UpdateExpressionSummary,
                SourceSpanDto.FromDomain(transition.SourceSpan));
    }

    internal sealed record CommandBindingDto(
        string ViewModelSymbol,
        string PropertyName,
        string CommandType,
        string ExecuteSymbol,
        string? CanExecuteSymbol,
        IReadOnlyList<string> BoundViews)
    {
        internal static CommandBindingDto FromDomain(CommandBinding command) =>
            new(
                command.ViewModelSymbol,
                command.PropertyName,
                command.CommandType,
                command.ExecuteSymbol,
                command.CanExecuteSymbol,
                command.BoundViews.Select(NormalizePath).OfType<string>().ToArray());
    }

    internal sealed record WindowActivationDto(
        string CallerSymbol,
        string ServiceSymbol,
        string WindowSymbol,
        string? WindowViewModelSymbol,
        string ActivationKind,
        string OwnerKind)
    {
        internal static WindowActivationDto FromDomain(WindowActivation activation) =>
            new(
                activation.CallerSymbol,
                activation.ServiceSymbol,
                activation.WindowSymbol,
                activation.WindowViewModelSymbol,
                activation.ActivationKind,
                activation.OwnerKind);
    }

    internal sealed record ServiceRegistrationDto(
        string RegistrationFile,
        string ServiceType,
        string ImplementationType,
        string Lifetime,
        SourceSpanDto SourceSpan)
    {
        internal static ServiceRegistrationDto FromDomain(ServiceRegistration registration) =>
            new(
                NormalizePath(registration.RegistrationFile)!,
                registration.ServiceType,
                registration.ImplementationType,
                registration.Lifetime switch
                {
                    ServiceRegistrationLifetime.Singleton => "singleton",
                    ServiceRegistrationLifetime.Scoped => "scoped",
                    ServiceRegistrationLifetime.Transient => "transient",
                    ServiceRegistrationLifetime.Factory => "factory",
                    _ => throw new ArgumentOutOfRangeException(nameof(registration.Lifetime), registration.Lifetime, null)
                },
                SourceSpanDto.FromDomain(registration.SourceSpan));
    }

    internal sealed record DiagnosticDto(
        string Code,
        string Message,
        string? FilePath,
        string Severity,
        IReadOnlyList<string> Candidates)
    {
        internal static DiagnosticDto FromDomain(Diagnostic diagnostic) =>
            new(
                diagnostic.Code,
                diagnostic.Message,
                NormalizePath(diagnostic.FilePath),
                diagnostic.Severity switch
                {
                    DiagnosticSeverity.Info => "info",
                    DiagnosticSeverity.Warning => "warning",
                    DiagnosticSeverity.Error => "error",
                    _ => throw new ArgumentOutOfRangeException(nameof(diagnostic.Severity), diagnostic.Severity, null)
                },
                diagnostic.Candidates.Select(NormalizePath).OfType<string>().ToArray());
    }

    internal sealed record ResolvedEntryDto(
        string Input,
        string ResolvedKind,
        string? ResolvedPath,
        string? Symbol,
        string Confidence,
        IReadOnlyList<ResolvedCandidateDto> Candidates);

    internal sealed record ResolvedCandidateDto(
        string Kind,
        string? Path,
        string? Symbol,
        string Reason)
    {
        internal static ResolvedCandidateDto FromDomain(ResolvedCandidate candidate) =>
            new(
                candidate.Kind switch
                {
                    CandidateKind.File => "file",
                    CandidateKind.Symbol => "symbol",
                    CandidateKind.View => "view",
                    CandidateKind.ViewModel => "view-model",
                    CandidateKind.Window => "window",
                    CandidateKind.Service => "service",
                    _ => throw new ArgumentOutOfRangeException(nameof(candidate.Kind), candidate.Kind, null)
                },
                NormalizePath(candidate.Path),
                candidate.Symbol,
                candidate.Reason);
    }
}
