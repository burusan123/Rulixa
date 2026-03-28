using Rulixa.Application.Ports;
using Rulixa.Domain.Scanning;
using Rulixa.Plugin.WpfNet8.Extraction;

namespace Rulixa.Plugin.WpfNet8.Scanning;

public sealed class WpfNet8WorkspaceScanner : IWorkspaceScanner
{
    private readonly WorkspaceScanInventoryBuilder inventoryBuilder;
    private readonly ScanSymbolCatalogBuilder symbolCatalogBuilder = new();
    private readonly ProjectSummaryBuilder projectSummaryBuilder = new();
    private readonly XamlViewModelBindingExtractor bindingExtractor = new();
    private readonly ViewModelNavigationTransitionExtractor navigationTransitionExtractor = new();
    private readonly CommandContractExtractor commandExtractor = new();
    private readonly DialogActivationExtractor dialogExtractor = new();
    private readonly ServiceRegistrationExtractor registrationExtractor = new();

    public WpfNet8WorkspaceScanner(IWorkspaceFileSystem fileSystem)
    {
        inventoryBuilder = new WorkspaceScanInventoryBuilder(fileSystem ?? throw new ArgumentNullException(nameof(fileSystem)));
    }

    public async Task<WorkspaceScanResult> ScanAsync(string workspaceRoot, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);

        var inventory = await inventoryBuilder.BuildAsync(workspaceRoot, cancellationToken).ConfigureAwait(false);
        var symbols = symbolCatalogBuilder.Build(inventory.FileContents);
        var bindings = bindingExtractor.Extract(inventory.FileContents, symbols);
        var navigationTransitions = navigationTransitionExtractor.Extract(inventory.FileContents);
        var commands = commandExtractor.Extract(inventory.FileContents, inventory.ScanFiles);
        var windowActivations = dialogExtractor.Extract(inventory.FileContents);
        var serviceRegistrations = registrationExtractor.Extract(inventory.FileContents);

        return new WorkspaceScanResult(
            "phase1.v1",
            Path.GetFullPath(workspaceRoot),
            DateTimeOffset.UtcNow,
            projectSummaryBuilder.Build(inventory.ScanFiles, inventory.FileContents, bindings),
            inventory.ScanFiles,
            symbols,
            bindings,
            navigationTransitions,
            commands,
            windowActivations,
            serviceRegistrations,
            []);
    }
}
