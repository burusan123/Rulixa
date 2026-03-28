using Rulixa.Domain.Entries;
using Rulixa.Domain.Packs;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal static class ViewBindingPackSectionBuilder
{
    internal static void AddBindingContracts(
        WorkspaceScanResult scanResult,
        IReadOnlyList<ViewModelBinding> bindings,
        bool required,
        ICollection<Contract> contracts,
        ICollection<FileSelectionCandidate> fileCandidates)
    {
        foreach (var binding in bindings)
        {
            var priority = binding.BindingKind == ViewModelBindingKind.DataTemplate ? 40 : 5;
            var title = binding.BindingKind switch
            {
                ViewModelBindingKind.RootDataContext => "ルート DataContext",
                ViewModelBindingKind.ViewDataContext => "View DataContext",
                ViewModelBindingKind.DataTemplate => "DataTemplate",
                _ => binding.BindingKind.ToString()
            };

            var summary = binding.BindingKind switch
            {
                ViewModelBindingKind.RootDataContext =>
                    $"{Path.GetFileName(binding.SourcePath)} が {binding.ViewPath} の DataContext に {binding.ViewModelSymbol} を設定します。",
                ViewModelBindingKind.ViewDataContext =>
                    $"{Path.GetFileName(binding.SourcePath)} が {binding.ViewPath} の DataContext に {binding.ViewModelSymbol} を設定します。",
                ViewModelBindingKind.DataTemplate =>
                    $"{binding.ViewPath} は {binding.ViewModelSymbol} 向けの DataTemplate を定義します。",
                _ =>
                    $"{binding.ViewPath} は {binding.ViewModelSymbol} に対応します。"
            };

            contracts.Add(new Contract(
                ContractKind.ViewModelBinding,
                title,
                summary,
                [binding.ViewPath, binding.SourcePath],
                [binding.ViewSymbol, binding.ViewModelSymbol]));

            var reason = binding.BindingKind switch
            {
                ViewModelBindingKind.RootDataContext => "root-binding",
                ViewModelBindingKind.ViewDataContext => "view-binding",
                ViewModelBindingKind.DataTemplate => "data-template",
                _ => "view-binding"
            };
            var sourceReason = binding.BindingKind switch
            {
                ViewModelBindingKind.RootDataContext => "root-binding-source",
                ViewModelBindingKind.ViewDataContext => "view-binding-source",
                ViewModelBindingKind.DataTemplate => "data-template-source",
                _ => "view-binding-source"
            };

            fileCandidates.Add(new FileSelectionCandidate(binding.ViewPath, reason, priority, required));
            fileCandidates.Add(new FileSelectionCandidate(
                binding.SourcePath,
                sourceReason,
                priority + 5,
                required && binding.BindingKind != ViewModelBindingKind.RootDataContext));

            var viewModelFile = scanResult.Symbols.FirstOrDefault(symbol =>
                string.Equals(symbol.QualifiedName, binding.ViewModelSymbol, StringComparison.OrdinalIgnoreCase))?.FilePath;
            if (!string.IsNullOrWhiteSpace(viewModelFile))
            {
                fileCandidates.Add(new FileSelectionCandidate(viewModelFile, reason, priority, required));
            }
        }
    }

    internal static void AddDataTemplateSummaryContract(
        IReadOnlyList<ViewModelBinding> dataTemplateBindings,
        ICollection<Contract> contracts)
    {
        if (dataTemplateBindings.Count == 0)
        {
            return;
        }

        var firstBinding = dataTemplateBindings[0];
        var sampleNames = dataTemplateBindings
            .Select(static binding => PackExtractionConventions.GetSimpleTypeName(binding.ViewModelSymbol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();
        var summary = $"{firstBinding.ViewPath} には {dataTemplateBindings.Count} 件の DataTemplate 二次文脈があります。例: {string.Join(", ", sampleNames)}。";

        contracts.Add(new Contract(
            ContractKind.ViewModelBinding,
            "DataTemplate 二次文脈",
            summary,
            [firstBinding.ViewPath],
            dataTemplateBindings
                .Select(static binding => binding.ViewModelSymbol)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()));
    }

    internal static void AddConventionalViewFiles(
        WorkspaceScanResult scanResult,
        ResolvedEntry resolvedEntry,
        ICollection<FileSelectionCandidate> fileCandidates)
    {
        if (resolvedEntry.ResolvedKind != ResolvedEntryKind.Symbol || string.IsNullOrWhiteSpace(resolvedEntry.Symbol))
        {
            if (!string.IsNullOrWhiteSpace(resolvedEntry.ResolvedPath)
                && resolvedEntry.ResolvedPath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                PackExtractionConventions.AddCodeBehindIfPresent(scanResult, resolvedEntry.ResolvedPath, fileCandidates, 1, true);
            }

            return;
        }

        var viewName = PackExtractionConventions.BuildConventionalViewName(resolvedEntry.Symbol);
        if (string.IsNullOrWhiteSpace(viewName))
        {
            return;
        }

        var viewPath = scanResult.Files
            .Where(static file => file.Kind == ScanFileKind.Xaml)
            .Select(static file => file.Path)
            .FirstOrDefault(path => Path.GetFileNameWithoutExtension(path).Equals(viewName, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(viewPath))
        {
            return;
        }

        fileCandidates.Add(new FileSelectionCandidate(viewPath, "conventional-view", 2, true));
        PackExtractionConventions.AddCodeBehindIfPresent(scanResult, viewPath, fileCandidates, 3, true);
    }

    internal static IndexSection BuildViewModelIndex(
        IReadOnlyList<ViewModelBinding> primaryBindings,
        IReadOnlyList<ViewModelBinding> secondaryBindings)
    {
        var lines = primaryBindings
            .Select(binding => $"{binding.ViewPath} <-> {binding.ViewModelSymbol} ({DescribeBindingKind(binding.BindingKind)}: {binding.SourcePath})")
            .ToList();

        if (secondaryBindings.Count > 0)
        {
            var firstBinding = secondaryBindings[0];
            var sampleNames = secondaryBindings
                .Select(static binding => PackExtractionConventions.GetSimpleTypeName(binding.ViewModelSymbol))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToArray();
            lines.Add($"{firstBinding.ViewPath} <-> DataTemplate 二次文脈 {secondaryBindings.Count}件 (例: {string.Join(", ", sampleNames)})");
        }

        return new IndexSection("View-ViewModel", lines);
    }

    private static string DescribeBindingKind(ViewModelBindingKind bindingKind) => bindingKind switch
    {
        ViewModelBindingKind.RootDataContext => "ルート DataContext",
        ViewModelBindingKind.ViewDataContext => "View DataContext",
        ViewModelBindingKind.DataTemplate => "DataTemplate",
        _ => bindingKind.ToString()
    };
}
