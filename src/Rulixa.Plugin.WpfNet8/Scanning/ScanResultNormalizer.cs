using Rulixa.Domain.Diagnostics;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Scanning;

internal static class ScanResultNormalizer
{
    internal static IReadOnlyList<ViewModelBinding> NormalizeBindings(
        IReadOnlyList<ViewModelBinding> bindings,
        ICollection<Diagnostic> diagnostics)
    {
        foreach (var binding in bindings.Where(static binding => binding.Candidates.Count > 1))
        {
            diagnostics.Add(new Diagnostic(
                "binding.viewmodel.ambiguous",
                $"ViewModel 候補が複数あります: {binding.ViewPath}",
                binding.ViewPath,
                DiagnosticSeverity.Warning,
                binding.Candidates));
        }

        return bindings;
    }

    internal static IReadOnlyList<ServiceRegistration> NormalizeServiceRegistrations(
        IReadOnlyList<ServiceRegistration> registrations,
        IReadOnlyDictionary<string, string> fileContents,
        IReadOnlyList<ScanSymbol> symbols,
        ICollection<Diagnostic> diagnostics)
    {
        var normalized = new List<ServiceRegistration>();

        foreach (var registration in registrations)
        {
            var serviceType = SymbolResolution.ResolveTypeSymbol(
                registration.ServiceType,
                registration.RegistrationFile,
                fileContents,
                symbols,
                diagnostics,
                "service-registration.service-type.unresolved",
                SymbolKind.Interface,
                SymbolKind.Class);
            var implementationType = SymbolResolution.ResolveTypeSymbol(
                registration.ImplementationType,
                registration.RegistrationFile,
                fileContents,
                symbols,
                diagnostics,
                "service-registration.implementation-type.unresolved",
                SymbolKind.Class);

            if (string.IsNullOrWhiteSpace(serviceType) || string.IsNullOrWhiteSpace(implementationType))
            {
                continue;
            }

            normalized.Add(registration with
            {
                ServiceType = serviceType,
                ImplementationType = implementationType
            });
        }

        return normalized;
    }

    internal static IReadOnlyList<WindowActivation> NormalizeWindowActivations(
        IReadOnlyList<WindowActivation> activations,
        IReadOnlyDictionary<string, string> fileContents,
        IReadOnlyList<ScanSymbol> symbols,
        ICollection<Diagnostic> diagnostics)
    {
        var normalized = new List<WindowActivation>();

        foreach (var activation in activations)
        {
            var sourcePath = symbols.FirstOrDefault(symbol =>
                string.Equals(symbol.QualifiedName, activation.ServiceSymbol, StringComparison.Ordinal))?.FilePath;
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                sourcePath = activation.ServiceSymbol.Replace('.', '/') + ".cs";
            }

            var serviceSymbol = SymbolResolution.ResolveTypeSymbol(
                activation.ServiceSymbol,
                sourcePath,
                fileContents,
                symbols,
                diagnostics,
                "window-activation.service-symbol.unresolved",
                SymbolKind.Class);
            var windowSymbol = SymbolResolution.ResolveTypeSymbol(
                activation.WindowSymbol,
                sourcePath,
                fileContents,
                symbols,
                diagnostics,
                "window-activation.window-symbol.unresolved",
                SymbolKind.Window,
                SymbolKind.Class);
            var windowViewModelSymbol = string.IsNullOrWhiteSpace(activation.WindowViewModelSymbol)
                ? null
                : SymbolResolution.ResolveTypeSymbol(
                    activation.WindowViewModelSymbol,
                    sourcePath,
                    fileContents,
                    symbols,
                    diagnostics,
                    "window-activation.window-viewmodel-symbol.unresolved",
                    SymbolKind.Class);

            if (string.IsNullOrWhiteSpace(serviceSymbol) || string.IsNullOrWhiteSpace(windowSymbol))
            {
                continue;
            }

            normalized.Add(activation with
            {
                ServiceSymbol = serviceSymbol,
                WindowSymbol = windowSymbol,
                WindowViewModelSymbol = windowViewModelSymbol
            });
        }

        return normalized;
    }
}
