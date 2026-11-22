namespace Estoq.App.Services;

public enum AppRoute
{
    Dashboard,
    Inventory,
    Sales,
}

public interface INavigationService
{
    AppRoute Current { get; }

    event EventHandler<AppRoute>? Navigated;

    /// <summary>Supplies the frame that hosts pages. Called once by the shell.</summary>
    void Initialize(Microsoft.UI.Xaml.Controls.Frame frame);

    void NavigateTo(AppRoute route);
}
