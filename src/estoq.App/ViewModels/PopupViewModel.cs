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
    private readonly ILocalizer _localizer;
    private readonly ISessionService _session;

    public PopupViewModel(IPopupService popups, ILocalizer localizer, ISessionService session)
    {
        _popups = popups;
        _localizer = localizer;
        _session = session;
        _popups.Changed += (_, _) => Refresh();
        _localizer.LanguageChanged += (_, _) => Refresh();
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

    /// <summary>True for Settings: its body offers Log out.</summary>
    [ObservableProperty]
    public partial bool IsSettings { get; set; }

    [RelayCommand]
    private void Close() => _popups.Close();

    [RelayCommand]
    private void LogOut() => _session.SignOut(SignOutReason.UserRequested);

    private string Text(string popup, string part) => _localizer[$"Popup_{popup}_{part}"];

    private void Refresh()
    {
        if (_popups.Current is not { } kind)
        {
            if (!IsOpen)
            {
                return;
            }

            // Keep the last text so the pop-up does not change while it hides.
            IsOpen = false;
            return;
        }

        (Title, Subtitle, Glyph, Message, PrimaryText) = kind switch
        {
            PopupKind.PointOfSale => (
                Text("Sale", "Title"),
                Text("Sale", "Subtitle"),
                "",
                Text("Sale", "Message"),
                Text("Sale", "Primary")),
            PopupKind.AddItems => (
                Text("Items", "Title"),
                Text("Items", "Subtitle"),
                "",
                Text("Items", "Message"),
                Text("Items", "Primary")),
            PopupKind.StockAdjustments => (
                Text("Adjust", "Title"),
                Text("Adjust", "Subtitle"),
                "",
                Text("Adjust", "Message"),
                Text("Adjust", "Primary")),
            PopupKind.Returns => (
                Text("Returns", "Title"),
                Text("Returns", "Subtitle"),
                "",
                Text("Returns", "Message"),
                Text("Returns", "Primary")),
            PopupKind.Settings => (
                Text("Settings", "Title"),
                Text("Settings", "Subtitle"),
                "",
                Text("Settings", "Message"),
                Text("Settings", "Primary")),
            _ => (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
        };

        IsSettings = kind == PopupKind.Settings;
        IsPrimaryEnabled = false;
        IsOpen = true;
    }
}
