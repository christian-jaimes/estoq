using Estoq.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Estoq.App.Controls;

/// <summary>
/// The standard filter-bar dropdowns (docs/technical/ui-implementation.md, "Menus and dropdown lists"), shared by Inventory and Sales:
/// a multi-select checkbox list with a search field, and a single-choice menu used for sorting.
/// </summary>
public static class FilterFlyouts
{
    /// <summary>Opens a multi-select dropdown right below <paramref name="anchor"/>, as wide as the anchor, with a search field and a scrolling checkbox list.</summary>
    public static void ShowMultiSelect(FrameworkElement anchor, IReadOnlyList<string> values, ISet<string> selected, Action changed)
    {
        var flyout = new Flyout
        {
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
            FlyoutPresenterStyle = (Style)Application.Current.Resources["EqDropdownFlyoutPresenterStyle"],
            Content = new MultiSelectList(values, selected, changed) { Width = Math.Max(anchor.ActualWidth, 220) - 16 },
        };
        flyout.ShowAt(anchor);
    }

    /// <summary>Shows a menu of choices under <paramref name="anchor"/>, with the current one ticked.</summary>
    public static void ShowOptions(FrameworkElement anchor, IReadOnlyList<FilterOption> options, string? selected, Action<string?> choose)
    {
        var menu = new MenuFlyout
        {
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
            MenuFlyoutPresenterStyle = (Style)Application.Current.Resources["EqMenuFlyoutPresenterStyle"],
        };
        foreach (var option in options)
        {
            var item = new ToggleMenuFlyoutItem { Text = option.Label, IsChecked = option.Value == selected };
            var value = option.Value;
            item.Click += (_, _) => choose(value);
            menu.Items.Add(item);
        }

        menu.ShowAt(anchor);
    }
}
