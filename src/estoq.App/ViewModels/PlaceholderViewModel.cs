using CommunityToolkit.Mvvm.ComponentModel;
using Estoq.App.Services;

namespace Estoq.App.ViewModels;

/// <summary>Drives the temporary page shown for areas delivered in later phases.</summary>
public sealed partial class PlaceholderViewModel : ObservableObject
{
    private readonly ILocalizer _localizer;

    public PlaceholderViewModel(ILocalizer localizer) => _localizer = localizer;

    /// <summary>Sets the page text for the area's route.</summary>
    public void Show(AppRoute route)
    {
        (Title, Message) = route switch
        {
            AppRoute.Inventory => (_localizer["Nav_Inventory"], _localizer["Page_Inventory_Message"]),
            AppRoute.Sales => (_localizer["Nav_Sales"], _localizer["Page_Sales_Message"]),
            _ => (route.ToString(), string.Empty),
        };
    }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;
}
