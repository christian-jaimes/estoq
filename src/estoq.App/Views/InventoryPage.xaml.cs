using Estoq.App.ViewModels.Inventory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Estoq.App.Views;

/// <summary>The Inventory page (docs/features/inventory.md). All behaviour lives in <see cref="InventoryViewModel"/>.</summary>
public sealed partial class InventoryPage : Page
{
    public InventoryPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<InventoryViewModel>();
        InitializeComponent();
    }

    public InventoryViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Activate();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.Deactivate();
    }

    private void OnRowTapped(object sender, TappedRoutedEventArgs e)
    {
        // A tap on the row's own menu button is not a row selection.
        if (e.OriginalSource is DependencyObject source && IsInsideButton(source, (DependencyObject)sender))
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: InventoryRowViewModel row })
        {
            ViewModel.Select(row);
        }
    }

    private void OnRowMenuClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: InventoryRowViewModel row } button)
        {
            return;
        }

        var menu = new MenuFlyout { Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedRight };
        var view = new MenuFlyoutItem { Text = ViewModel.MenuViewText };
        view.Click += (_, _) => ViewModel.Select(row);
        menu.Items.Add(view);

        if (ViewModel.CanArchive)
        {
            var archive = new MenuFlyoutItem { Text = row.IsArchived ? ViewModel.MenuRestoreText : ViewModel.MenuArchiveText };
            archive.Click += (_, _) =>
            {
                ViewModel.Select(row);
                ViewModel.ToggleArchiveSelectedCommand.Execute(null);
            };
            menu.Items.Add(archive);
        }

        menu.ShowAt(button);
    }

    private void OnCategoryClick(object sender, RoutedEventArgs e) =>
        ShowOptions((FrameworkElement)sender, ViewModel.CategoryOptions, ViewModel.SelectedCategory, ViewModel.SelectCategory);

    private void OnBrandClick(object sender, RoutedEventArgs e) =>
        ShowOptions((FrameworkElement)sender, ViewModel.BrandOptions, ViewModel.SelectedBrand, ViewModel.SelectBrand);

    private void OnStatusClick(object sender, RoutedEventArgs e) =>
        ShowOptions((FrameworkElement)sender, ViewModel.StatusOptions, ViewModel.SelectedStatus, ViewModel.SelectStatus);

    private void OnSortClick(object sender, RoutedEventArgs e) =>
        ShowOptions((FrameworkElement)sender, ViewModel.SortOptions, ViewModel.SelectedSort, ViewModel.SelectSort);

    private void OnPageClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: int page })
        {
            ViewModel.GoToPage(page);
        }
    }

    /// <summary>Shows a menu of choices under <paramref name="anchor"/>, with the current one ticked.</summary>
    private static void ShowOptions(FrameworkElement anchor, IReadOnlyList<FilterOption> options, string? selected, Action<string?> choose)
    {
        var menu = new MenuFlyout { Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedLeft };
        foreach (var option in options)
        {
            var item = new ToggleMenuFlyoutItem { Text = option.Label, IsChecked = option.Value == selected };
            var value = option.Value;
            item.Click += (_, _) => choose(value);
            menu.Items.Add(item);
        }

        menu.ShowAt(anchor);
    }

    private static bool IsInsideButton(DependencyObject source, DependencyObject row)
    {
        for (var current = source; current is not null && current != row; current = VisualTreeHelper.GetParent(current))
        {
            if (current is Microsoft.UI.Xaml.Controls.Primitives.ButtonBase)
            {
                return true;
            }
        }

        return false;
    }
}
