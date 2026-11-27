using Estoq.App.Views;
using Estoq.App.Views.Sales;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Estoq.App.Services;

public sealed class NavigationService : INavigationService
{
    private Frame? _frame;

    public AppRoute Current { get; private set; } = AppRoute.Dashboard;

    public event EventHandler<AppRoute>? Navigated;

    public void Initialize(Frame frame) => _frame = frame;

    public void NavigateTo(AppRoute route)
    {
        if (_frame is null)
        {
            throw new InvalidOperationException("Navigation frame has not been initialised.");
        }

        if (route == Current && _frame.Content is not null)
        {
            return;
        }

        var page = route switch
        {
            AppRoute.Dashboard => typeof(DashboardPage),
            AppRoute.Sales => typeof(SalesPage),
            _ => typeof(PlaceholderPage),
        };

        // Subtle, short transition.
        _frame.Navigate(page, route, new EntranceNavigationTransitionInfo());
        Current = route;
        Navigated?.Invoke(this, route);
    }
}
