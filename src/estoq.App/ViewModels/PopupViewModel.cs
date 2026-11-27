using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.App.ViewModels.AddProduct;

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
    private readonly AddProductViewModel _addProduct;
    private PopupKind? _shown;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _hintTimer;

    public PopupViewModel(IPopupService popups, ILocalizer localizer, ISessionService session, AddProductViewModel addProduct)
    {
        _popups = popups;
        _localizer = localizer;
        _session = session;
        _addProduct = addProduct;
        _popups.Changed += (_, _) => Refresh();
        _localizer.LanguageChanged += (_, _) => Refresh();

        // The Add product form decides when its primary action is allowed; saving closes the pop-up.
        _addProduct.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AddProductViewModel.Name) && _popups.Current == PopupKind.AddItems)
            {
                Title = _addProduct.Name.Length == 0 ? Text("Items", "Title") : $"{Text("Items", "Title")} – {_addProduct.Name}";
            }
            if (e.PropertyName is nameof(AddProductViewModel.CanSave) or nameof(AddProductViewModel.IsCompleted) && _popups.Current == PopupKind.AddItems)
            {
                // Save stays off while the "Product added" confirmation is showing.
                IsPrimaryEnabled = _addProduct.CanSave && !_addProduct.IsCompleted;
            }
        };
        _addProduct.CloseRequested += (_, _) => _popups.Close();
    }

    /// <summary>The Add product form; the shell binds its "Product added" confirmation over the whole pop-up.</summary>
    public AddProductViewModel AddProduct => _addProduct;

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Subtitle { get; set; } = string.Empty;

    /// <summary>The key that opens this pop-up (F2 to F4), shown next to the title for four seconds after it opens.</summary>
    [ObservableProperty]
    public partial string ShortcutHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsShortcutHintVisible { get; set; }

    /// <summary>Material glyph shown in the header badge.</summary>
    [ObservableProperty]
    public partial string Glyph { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    /// <summary>True while the point of sale is open. It has its own layout and does not use the shared frame.</summary>
    [ObservableProperty]
    public partial bool IsPointOfSale { get; set; }

    /// <summary>True while a pop-up that still uses the shared frame is open.</summary>
    [ObservableProperty]
    public partial bool IsFramed { get; set; }

    [ObservableProperty]
    public partial string PrimaryText { get; set; } = string.Empty;

    /// <summary>The primary action stays disabled until the flow behind it is built.</summary>
    [ObservableProperty]
    public partial bool IsPrimaryEnabled { get; set; }

    /// <summary>True for Settings: its body offers Log out.</summary>
    [ObservableProperty]
    public partial bool IsSettings { get; set; }

    /// <summary>True for F2: the body shows the Add product form.</summary>
    [ObservableProperty]
    public partial bool IsAddProduct { get; set; }

    /// <summary>True while a placeholder message (a flow that is not built yet) is shown.</summary>
    [ObservableProperty]
    public partial bool ShowMessage { get; set; } = true;

    [RelayCommand]
    private void Close() => _popups.Close();

    [RelayCommand]
    private async Task PrimaryAsync()
    {
        if (_popups.Current == PopupKind.AddItems)
        {
            await _addProduct.SaveCommand.ExecuteAsync(null);
        }
    }

    [RelayCommand]
    private void LogOut() => _session.SignOut(SignOutReason.UserRequested);

    private void ShowShortcutHintForFourSeconds()
    {
        _hintTimer?.Stop();
        _hintTimer = null;
        IsShortcutHintVisible = ShortcutHint.Length > 0;
        if (!IsShortcutHintVisible)
        {
            return;
        }

        _hintTimer = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().CreateTimer();
        _hintTimer.Interval = TimeSpan.FromSeconds(4);
        _hintTimer.IsRepeating = false;
        _hintTimer.Tick += (_, _) =>
        {
            IsShortcutHintVisible = false;
            _hintTimer?.Stop();
            _hintTimer = null;
        };
        _hintTimer.Start();
    }

    private string Text(string popup, string part) => _localizer[$"Popup_{popup}_{part}"];

    private void Refresh()
    {
        if (_popups.Current is not { } kind)
        {
            _hintTimer?.Stop();
            _hintTimer = null;
            IsShortcutHintVisible = false;

            // Closing the pop-up also dismisses a "Product added" confirmation so it never shows over another pop-up.
            if (_addProduct.IsCompleted)
            {
                _addProduct.Reset();
            }

            if (!IsOpen)
            {
                return;
            }

            // Keep the last text so the pop-up does not change while it hides.
            IsOpen = false;
            _shown = null;
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
        IsAddProduct = kind == PopupKind.AddItems;
        ShowMessage = !IsAddProduct;

        // A language change refreshes the text only; a fresh open starts a fresh form.
        if (kind == PopupKind.AddItems && _shown != kind)
        {
            _addProduct.Reset();
        }

        if (kind == PopupKind.AddItems)
        {
            Title = _addProduct.Name.Length == 0 ? Text("Items", "Title") : $"{Text("Items", "Title")} – {_addProduct.Name}";
            Subtitle = Text("Items", "Subtitle");
        }

        // Only the shortcut key is temporary: it shows for four seconds on a fresh open, the subtitle always stays.
        ShortcutHint = kind switch
        {
            PopupKind.AddItems => "F2",
            PopupKind.StockAdjustments => "F3",
            PopupKind.Returns => "F4",
            _ => string.Empty,
        };
        if (_shown != kind)
        {
            ShowShortcutHintForFourSeconds();
        }

        _shown = kind;
        IsPrimaryEnabled = kind == PopupKind.AddItems && _addProduct.CanSave;
        IsPointOfSale = kind == PopupKind.PointOfSale;
        IsFramed = !IsPointOfSale;
        IsOpen = true;
    }
}
