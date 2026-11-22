using CommunityToolkit.Mvvm.ComponentModel;

namespace Estoq.App.ViewModels;

/// <summary>Drives the temporary page shown for areas delivered in later phases.</summary>
public sealed partial class PlaceholderViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;
}
