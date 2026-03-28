using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed record RelevantPackContext(
    IReadOnlySet<string> ViewModelSymbols,
    IReadOnlyList<ViewModelBinding> RelevantBindings,
    IReadOnlyList<ViewModelBinding> PrimaryBindings,
    IReadOnlyList<ViewModelBinding> SecondaryBindings,
    IReadOnlyList<NavigationTransition> RelevantTransitions);
