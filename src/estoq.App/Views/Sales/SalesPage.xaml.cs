using Estoq.App.Controls;
using Estoq.App.Services;
using Estoq.App.ViewModels.Sales;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.Views.Sales;

public sealed partial class SalesPage : Page
{
    public SalesPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<SalesViewModel>();
        InitializeComponent();
        _panel = new DetailPanelController(DetailPanel, DetailTransform);

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        _panel.Set(ViewModel.HasSelection, animate: false);

        // The view model listens for language changes; release it when the page goes away.
        Unloaded += (_, _) =>
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            ViewModel.Dispose();
        };
    }

    private readonly DetailPanelController _panel;

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SalesViewModel.HasSelection))
        {
            _panel.Set(ViewModel.HasSelection, animate: true);
        }
    }

    private void OnRowTapped(object sender, TappedRoutedEventArgs e)
    {
        // A tap on the row's own menu button is not a row selection.
        if (e.OriginalSource is DependencyObject source && DetailPanelController.IsInsideButton(source, (DependencyObject)sender))
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: SaleRowViewModel row })
        {
            ViewModel.ToggleRow(row);
            e.Handled = true;
        }
    }

    /// <summary>A click anywhere that is not a row, a button or the detail closes it.</summary>
    private void OnPageTapped(object sender, TappedRoutedEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || !_panel.IsClosingTap(source, this))
        {
            return;
        }

        ViewModel.CloseDetailCommand.Execute(null);
    }

    public SalesViewModel ViewModel { get; }

    private void OnRowMenuClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SaleRowViewModel row } anchor)
        {
            ViewModel.View(row);
            ShowMenu(anchor, row);
        }
    }

    private void OnDetailMenuClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement anchor && ViewModel.Selected is { } row)
        {
            ShowMenu(anchor, row);
        }
    }

    private void OnRowPointerEntered(object sender, PointerRoutedEventArgs e) => TableRowHover.Set(sender, 1);

    private void OnRowPointerExited(object sender, PointerRoutedEventArgs e) => TableRowHover.Set(sender, 0);

    private void OnTableViewChanged(object? sender, ScrollViewerViewChangedEventArgs e) => LoadMoreIfNeeded();

    private void OnTableSizeChanged(object sender, SizeChangedEventArgs e) => LoadMoreIfNeeded();

    /// <summary>Load on scroll: adds the next batch of sales while the end of the table is near, or the rows do not fill the view yet.</summary>
    private void LoadMoreIfNeeded() => TableLazyLoad.LoadMoreIfNeeded(TableScroll, () => ViewModel.HasMore, ViewModel.LoadMore);

    /// <summary>The row menu: view, print receipt, return (docs/features/sales.md). Built here because menu items cannot bind to a row template.</summary>
    private void ShowMenu(FrameworkElement anchor, SaleRowViewModel row)
    {
        var menu = new MenuFlyout();
        menu.Items.Add(MenuItem("Sales_MenuView", "", () => ViewModel.View(row), isEnabled: true));
        menu.Items.Add(MenuItem("Sales_MenuPrint", "", () => _ = ViewModel.PrintReceiptOfAsync(row), isEnabled: true));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(MenuItem("Sales_MenuReturn", "", () => ViewModel.StartReturn(row), SalesViewModel.CanReturn(row)));
        menu.ShowAt(anchor);
    }

    private static MenuFlyoutItem MenuItem(string textKey, string glyph, Action invoke, bool isEnabled)
    {
        var item = new MenuFlyoutItem
        {
            Text = Loc.Get(textKey),
            IsEnabled = isEnabled,
            Icon = new FontIcon { Glyph = glyph, FontFamily = (FontFamily)Application.Current.Resources["EqIconFont"] },
        };
        item.Click += (_, _) => invoke();
        return item;
    }
}
