using System.Text.RegularExpressions;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class NavigationContractExtractor
{
    private static readonly Regex ItemsSourceBindingRegex = new(@"ItemsSource\s*=\s*""\{Binding\s+(?<property>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex SelectedItemBindingRegex = new(@"SelectedItem\s*=\s*""\{Binding\s+(?<property>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex ContentBindingRegex = new(@"Content\s*=\s*""\{Binding\s+(?<property>[A-Za-z_]\w*)", RegexOptions.Compiled);

    public NavigationBinding? Extract(string viewPath, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewPath);
        ArgumentNullException.ThrowIfNull(content);

        var itemsSource = MatchProperty(ItemsSourceBindingRegex, content);
        var selectedItem = MatchProperty(SelectedItemBindingRegex, content);
        var contentTarget = MatchProperty(ContentBindingRegex, content);

        if (string.IsNullOrWhiteSpace(selectedItem) && string.IsNullOrWhiteSpace(contentTarget))
        {
            return null;
        }

        return new NavigationBinding(
            viewPath,
            itemsSource,
            selectedItem,
            contentTarget);
    }

    private static string? MatchProperty(Regex regex, string content)
    {
        var match = regex.Match(content);
        return match.Success ? match.Groups["property"].Value : null;
    }
}

internal sealed record NavigationBinding(
    string ViewPath,
    string? ItemsSourceProperty,
    string? SelectedItemProperty,
    string? ContentProperty);
