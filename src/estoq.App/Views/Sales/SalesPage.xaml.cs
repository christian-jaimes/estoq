using Estoq.App.Services;
using Estoq.App.ViewModels.Sales;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.Views.Sales;

public sealed partial class SalesPage : Page
{
    public SalesPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<SalesViewModel>();
        InitializeComponent();

        // The view model listens for language changes; release it when the page goes away.
        Unloaded += (_, _) => ViewModel.Dispose();
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

    private void OnPageButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PageButtonViewModel page })
        {
            page.Go();
        }
    }

    /// <summary>The row menu: view, print receipt, return, void (docs/features/sales.md). Built here because menu items cannot bind to a row template.</summary>
    private void ShowMenu(FrameworkElement anchor, SaleRowViewModel row)
    {
        var menu = new MenuFlyout();
        menu.Items.Add(MenuItem("Sales_MenuView", "", () => ViewModel.View(row), isEnabled: true));
        menu.Items.Add(MenuItem("Sales_MenuPrint", "", () => _ = ViewModel.PrintReceiptOfAsync(row), isEnabled: true));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(MenuItem("Sales_MenuReturn", "", () => ViewModel.StartReturn(row), SalesViewModel.CanReturn(row)));

        // Voids are specified in docs/features/returns-and-voids.md and built with the returns work.
        menu.Items.Add(MenuItem("Sales_MenuVoid", "", () => ViewModel.StartVoid(row), isEnabled: true));
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
