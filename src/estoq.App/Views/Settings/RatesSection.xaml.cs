using Estoq.App.ViewModels.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Views.Settings;

/// <summary>The Exchange rates section of the Settings pop-up. All behaviour lives in <see cref="ExchangeRatesViewModel"/>.</summary>
public sealed partial class RatesSection : UserControl
{
    public RatesSection()
    {
        ViewModel = App.Current.Services.GetRequiredService<ExchangeRatesViewModel>();
        InitializeComponent();
    }

    public ExchangeRatesViewModel ViewModel { get; }

    private void OnUpdateClick(object sender, RoutedEventArgs e) => ViewModel.StartUpdateCommand.Execute(RowOf(sender));

    private void OnHistoryClick(object sender, RoutedEventArgs e) => ViewModel.ShowHistoryCommand.Execute(RowOf(sender));

    private static RateRowViewModel? RowOf(object sender) => (sender as FrameworkElement)?.DataContext as RateRowViewModel;
}
