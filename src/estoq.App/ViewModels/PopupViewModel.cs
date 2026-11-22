using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;

namespace Estoq.App.ViewModels;

/// <summary>
/// Drives the shared pop-up shown by F1 to F4. The text for each flow lives here until the flow is built;
/// each roadmap step replaces the placeholder message with the real flow content.
/// </summary>
public sealed partial class PopupViewModel : ObservableObject
{
    private readonly IPopupService _popups;

    public PopupViewModel(IPopupService popups)
    {
        _popups = popups;
        _popups.Changed += (_, _) => Refresh();
    }

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Subtitle { get; set; } = string.Empty;

    /// <summary>Material glyph shown in the header badge.</summary>
    [ObservableProperty]
    public partial string Glyph { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PrimaryText { get; set; } = string.Empty;

    /// <summary>The primary action stays disabled until the flow behind it is built.</summary>
    [ObservableProperty]
    public partial bool IsPrimaryEnabled { get; set; }

    [RelayCommand]
    private void Close() => _popups.Close();

    private void Refresh()
    {
        if (_popups.Current is not { } kind)
        {
            // Keep the last text so the pop-up does not change while it hides.
            IsOpen = false;
            return;
        }

        (Title, Subtitle, Glyph, Message, PrimaryText) = kind switch
        {
            PopupKind.PointOfSale => (
                "Point of sale",
                "New sale (F1)",
                "",
                "The sales workflow (scan, cart, payment and receipt) is built next (see roadmap).",
                "Complete sale"),
            PopupKind.AddItems => (
                "Add or receive items",
                "Create a product or add stock (F2)",
                "",
                "Product creation and stock receiving is built next (see roadmap).",
                "Save product"),
            PopupKind.StockAdjustments => (
                "Stock adjustments",
                "Count an item and record a reason (F3)",
                "",
                "Counting items and recording a reason for each change is built next (see roadmap).",
                "Save adjustment"),
            PopupKind.Returns => (
                "Returns",
                "Return or exchange an item (F4)",
                "",
                "Customer returns is built next (see roadmap).",
                "Save return"),
            PopupKind.Settings => (
                "Settings",
                "Account, store, staff and exchange rates",
                "",
                "Settings are built per the roadmap.",
                "Save changes"),
            _ => (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
        };

        IsPrimaryEnabled = false;
        IsOpen = true;
    }
}
