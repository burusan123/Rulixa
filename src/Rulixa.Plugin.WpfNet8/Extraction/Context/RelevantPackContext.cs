using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed record RelevantPackContext(
    GoalExpansionProfile GoalProfile,
    IReadOnlySet<string> ViewModelSymbols,
    IReadOnlySet<string> ExplorationRootSymbols,
    IReadOnlySet<string> RelatedSymbols,
    IReadOnlyDictionary<string, PartialSymbolAggregate> SymbolAggregates,
    IReadOnlyList<ViewModelBinding> RelevantBindings,
    IReadOnlyList<ViewModelBinding> PrimaryBindings,
    IReadOnlyList<ViewModelBinding> SecondaryBindings,
    IReadOnlyList<NavigationTransition> RelevantTransitions,
    SystemPackContext? SystemPack);
