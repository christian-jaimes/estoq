using System.ComponentModel;
using Estoq.App.Controls;
using Estoq.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Estoq.App.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<DashboardViewModel>();
        InitializeComponent();
        SalesChart.AxisFormat = ViewModel.AxisFormat;
        Loaded += (_, _) =>
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            PlaceLowStock();
        };
        Unloaded += (_, _) => ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    public DashboardViewModel ViewModel { get; }

    /// <summary>Reads the figures again each time the page is opened, so a sale rung up since shows and a change of signed-in role takes effect.</summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Refresh();
        PlaceLowStock();
    }

    private void OnDateClick(object sender, RoutedEventArgs e)
    {
        var flyout = new Flyout
        {
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedLeft,
            FlyoutPresenterStyle = (Style)Application.Current.Resources["EqDateRangeFlyoutStyle"],
        };

        // Two months need about 840 px from the button's left edge; in a narrower window show one month instead of clipping.
        var anchor = (FrameworkElement)sender;
        var left = anchor.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0)).X;
        var compact = left + 840 + 24 > anchor.XamlRoot.Size.Width;

        flyout.Content = new DateRangePicker(
            ViewModel.Dates,
            DateOnly.FromDateTime(DateTime.Today),
            ViewModel.Culture,
            ViewModel.ApplyDates,
            compact,
            flyout.Hide);
        flyout.ShowAt(anchor);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.ShowCharts))
        {
            PlaceLowStock();
        }
    }

    /// <summary>A cashier sees no charts, so the low-stock card takes the whole row instead of sitting in the third column.</summary>
    private void PlaceLowStock()
    {
        Grid.SetColumn(LowStockCard, ViewModel.ShowCharts ? 2 : 0);
        Grid.SetColumnSpan(LowStockCard, ViewModel.ShowCharts ? 1 : 3);
    }
}
