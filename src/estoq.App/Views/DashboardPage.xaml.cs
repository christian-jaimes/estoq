using System.ComponentModel;
using Estoq.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
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
