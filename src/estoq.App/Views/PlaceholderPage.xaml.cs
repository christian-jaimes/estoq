using Estoq.App.Services;
using Estoq.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Estoq.App.Views;

/// <summary>Temporary page for areas delivered in later roadmap steps (see docs/roadmap.md).</summary>
public sealed partial class PlaceholderPage : Page
{
    public PlaceholderPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<PlaceholderViewModel>();
        InitializeComponent();
    }

    public PlaceholderViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is AppRoute route)
        {
            ViewModel.Show(route);
        }
    }
}
