using System.Text.RegularExpressions;
using Rulixa.Domain.Scanning;

namespace Rulixa.Plugin.WpfNet8.Extraction;

internal sealed class NavigationContractExtractor
{
    private static readonly Regex ItemsSourceBindingRegex = new(@"ItemsSource\s*=\s*""\{Binding\s+(?<property>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex SelectedItemBindingRegex = new(@"SelectedItem\s*=\s*""\{Binding\s+(?<property>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex ContentBindingRegex = new(@"Content\s*=\s*""\{Binding\s+(?<property>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex CommandBindingRegex = new(@"Command\s*=\s*""\{Binding\s+(?<property>[A-Za-z_]\w*)", RegexOptions.Compiled);

    public NavigationBinding? Extract(string viewPath, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewPath);
        ArgumentNullException.ThrowIfNull(content);

        var itemsSource = MatchBinding(ItemsSourceBindingRegex, content);
        var selectedItem = MatchBinding(SelectedItemBindingRegex, content);
        var contentTarget = MatchBinding(ContentBindingRegex, content);
        var command = MatchBinding(CommandBindingRegex, content);

        if (selectedItem is null && contentTarget is null && itemsSource is null && command is null)
        {
            return null;
        }

        return new NavigationBinding(
            viewPath,
            itemsSource,
            selectedItem,
            contentTarget,
            command);
    }

    private static BoundBinding? MatchBinding(Regex regex, string content)
    {
        var match = regex.Match(content);
        return match.Success
            ? new BoundBinding(
                match.Groups["property"].Value,
                SourceSpanFactory.FromMatch(content, match.Index, match.Length))
            : null;
    }
}

internal sealed record NavigationBinding(
    string ViewPath,
    BoundBinding? ItemsSourceBinding,
    BoundBinding? SelectedItemBinding,
    BoundBinding? ContentBinding,
    BoundBinding? CommandBinding)
{
    public string? ItemsSourceProperty => ItemsSourceBinding?.PropertyName;

    public string? SelectedItemProperty => SelectedItemBinding?.PropertyName;

    public string? ContentProperty => ContentBinding?.PropertyName;

    public string? CommandProperty => CommandBinding?.PropertyName;
}

internal sealed record BoundBinding(string PropertyName, SourceSpan SourceSpan);
