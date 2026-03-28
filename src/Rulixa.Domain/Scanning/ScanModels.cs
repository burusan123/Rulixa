using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Entries;

namespace Rulixa.Domain.Scanning;

public sealed record WorkspaceScanResult(
    string SchemaVersion,
    string WorkspaceRoot,
    DateTimeOffset GeneratedAtUtc,
    ProjectSummary ProjectSummary,
    IReadOnlyList<ScanFile> Files,
    IReadOnlyList<ScanSymbol> Symbols,
    IReadOnlyList<ViewModelBinding> ViewModelBindings,
    IReadOnlyList<NavigationTransition> NavigationTransitions,
    IReadOnlyList<CommandBinding> Commands,
    IReadOnlyList<WindowActivation> WindowActivations,
    IReadOnlyList<ServiceRegistration> ServiceRegistrations,
    IReadOnlyList<Diagnostic> Diagnostics);

public sealed record ProjectSummary(
    IReadOnlyList<string> SolutionFiles,
    IReadOnlyList<string> ProjectFiles,
    IReadOnlyList<string> TargetFrameworks,
    bool UsesWpf,
    IReadOnlyList<string> EntryPoints,
    IReadOnlyList<string> RootViewModels);

public sealed record ScanFile(
    string Path,
    ScanFileKind Kind,
    string Project,
    string Hash,
    int LineCount,
    IReadOnlyList<string> Tags);

public sealed record ScanSymbol(
    string Id,
    SymbolKind Kind,
    string QualifiedName,
    string DisplayName,
    string FilePath,
    int StartLine,
    int EndLine,
    IReadOnlyList<string> Tags);

public sealed record ViewModelBinding(
    string ViewPath,
    string ViewSymbol,
    string ViewModelSymbol,
    ViewModelBindingKind BindingKind,
    string SourcePath,
    ConfidenceLevel Confidence,
    IReadOnlyList<string> Candidates);

public sealed record CommandBinding(
    string ViewModelSymbol,
    string PropertyName,
    string CommandType,
    string ExecuteSymbol,
    string? CanExecuteSymbol,
    IReadOnlyList<string> BoundViews);

public sealed record NavigationTransition(
    string ViewModelSymbol,
    string SourceFilePath,
    string UpdateMethodName,
    string? SelectedItemPropertyName,
    string? CurrentPagePropertyName,
    string UpdateExpressionSummary,
    int StartLine);

public sealed record WindowActivation(
    string CallerSymbol,
    string ServiceSymbol,
    string WindowSymbol,
    string? WindowViewModelSymbol,
    string ActivationKind,
    string OwnerKind);

public sealed record ServiceRegistration(
    string RegistrationFile,
    string ServiceType,
    string ImplementationType,
    ServiceRegistrationLifetime Lifetime);

public enum ScanFileKind
{
    Unknown,
    Solution,
    Project,
    Startup,
    Xaml,
    CodeBehind,
    ViewModel,
    Service,
    Config,
    CommandSupport,
    CSharp
}

public enum SymbolKind
{
    Class,
    Method,
    Property,
    Command,
    Interface,
    Window
}

public enum ServiceRegistrationLifetime
{
    Singleton,
    Scoped,
    Transient,
    Factory
}

public enum ViewModelBindingKind
{
    RootDataContext,
    ViewDataContext,
    DataTemplate
}
