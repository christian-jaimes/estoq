using System.ComponentModel;
using Estoq.App.Controls;
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
        _panel = new DetailPanelController(DetailPanel, DetailTransform);
    }

    public InventoryViewModel ViewModel { get; }

    private readonly DetailPanelController _panel;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.Activate();
        _panel.Set(ViewModel.HasDetail, animate: false);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Deactivate();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InventoryViewModel.HasDetail))
        {
            _panel.Set(ViewModel.HasDetail, animate: true);
        }
    }

    private void OnRowTapped(object sender, TappedRoutedEventArgs e)
    {
        // A tap on the row's own menu button is not a row selection.
        if (e.OriginalSource is DependencyObject source && DetailPanelController.IsInsideButton(source, (DependencyObject)sender))
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: InventoryRowViewModel row })
        {
            ViewModel.ToggleRow(row);
            e.Handled = true;
        }
    }

    /// <summary>A click anywhere that is not a row, a button or the detail panel closes the panel.</summary>
    private void OnPageTapped(object sender, TappedRoutedEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || !_panel.IsClosingTap(source, this))
        {
            return;
        }

        ViewModel.CloseDetailCommand.Execute(null);
    }

    private void OnTypeClick(object sender, RoutedEventArgs e) =>
        ShowMultiSelect((FrameworkElement)sender, ViewModel.TypeValues, ViewModel.SelectedTypes);

    private void OnCategoryClick(object sender, RoutedEventArgs e) =>
        ShowMultiSelect((FrameworkElement)sender, ViewModel.CategoryValues, ViewModel.SelectedCategories);

    private void OnBrandClick(object sender, RoutedEventArgs e) =>
        ShowMultiSelect((FrameworkElement)sender, ViewModel.BrandValues, ViewModel.SelectedBrands);

    private void OnSortClick(object sender, RoutedEventArgs e) =>
        ShowOptions((FrameworkElement)sender, ViewModel.SortOptions, ViewModel.SelectedSort, ViewModel.SelectSort);

    private void OnRowDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && DetailPanelController.IsInsideButton(source, (DependencyObject)sender))
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: InventoryRowViewModel row })
        {
            ViewModel.EditRow(row);
            e.Handled = true;
        }
    }

    private void OnRowPointerEntered(object sender, PointerRoutedEventArgs e) => SetHover(sender, 1);

    private void OnRowPointerExited(object sender, PointerRoutedEventArgs e) => SetHover(sender, 0);

    private static void SetHover(object row, double opacity)
    {
        if (row is Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is Border { Name: "HoverFill" } fill)
                {
                    fill.Opacity = opacity;
                    return;
                }
            }
        }
    }

    private void OnTableViewChanged(object? sender, ScrollViewerViewChangedEventArgs e) => LoadMoreIfNeeded();

    private void OnTableSizeChanged(object sender, SizeChangedEventArgs e) => LoadMoreIfNeeded();

    /// <summary>Lazy load: adds the next batch of rows while the end of the table is near, or the rows do not fill the view yet.</summary>
    private void LoadMoreIfNeeded()
    {
        if (!ViewModel.HasMore)
        {
            return;
        }

        if (TableScroll.ScrollableHeight - TableScroll.VerticalOffset > 240)
        {
            return;
        }

        ViewModel.LoadMore();

        // Once the new rows are laid out, check again: a tall window may need more than one batch.
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, LoadMoreIfNeeded);
    }

    /// <summary>Shows a menu of choices under <paramref name="anchor"/>, with the current one ticked.</summary>
    /// <summary>Opens a multi-select dropdown right below <paramref name="anchor"/>, as wide as the anchor, with a search field and a scrolling checkbox list.</summary>
    private void ShowMultiSelect(FrameworkElement anchor, IReadOnlyList<string> values, ISet<string> selected)
    {
        var flyout = new Flyout
        {
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedLeft,
            FlyoutPresenterStyle = (Style)Application.Current.Resources["EqDropdownFlyoutPresenterStyle"],
            Content = new Controls.MultiSelectList(values, selected, ViewModel.FiltersChanged) { Width = Math.Max(anchor.ActualWidth, 220) - 16 },
        };
        flyout.ShowAt(anchor);
    }

    private static void ShowOptions(FrameworkElement anchor, IReadOnlyList<FilterOption> options, string? selected, Action<string?> choose)
    {
        var menu = new MenuFlyout
        {
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedLeft,
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
