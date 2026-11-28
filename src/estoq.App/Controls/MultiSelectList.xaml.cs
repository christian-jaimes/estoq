using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Controls;

/// <summary>
/// Content of a multi-select dropdown (see "Menus and dropdown lists" in docs/technical/ui-implementation.md):
/// search field, then a scrolling list of checkboxes. It edits <c>selected</c> in place and calls <c>changed</c> after every tick.
/// </summary>
public sealed partial class MultiSelectList : UserControl
{
    private readonly List<CheckBox> _boxes = [];

    public MultiSelectList(IReadOnlyList<string> values, ISet<string> selected, Action changed)
    {
        InitializeComponent();

        foreach (var value in values)
        {
            var box = new CheckBox
            {
                Content = value,
                IsChecked = selected.Contains(value),
                MinHeight = 34,
                MinWidth = 0,
                // The stock checkbox pads its label 5 px from the top, which sits it above the box; centre it instead.
                Padding = new Thickness(10, 0, 8, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            var captured = value;
            box.Checked += (_, _) =>
            {
                selected.Add(captured);
                changed();
            };
            box.Unchecked += (_, _) =>
            {
                selected.Remove(captured);
                changed();
            };
            _boxes.Add(box);
            ItemsHost.Children.Add(box);
        }

        NoMatches.Visibility = values.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text.Trim();
        var shown = 0;
        foreach (var box in _boxes)
        {
            var match = query.Length == 0
                || ((string)box.Content).Contains(query, StringComparison.CurrentCultureIgnoreCase);
            box.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
            if (match)
            {
                shown++;
            }
        }

        NoMatches.Visibility = shown == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
