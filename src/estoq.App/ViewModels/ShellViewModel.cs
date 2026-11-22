using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;

namespace Estoq.App.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    private const double ExpandedWidth = 240;
    private const double CollapsedWidth = 64;

    private readonly INavigationService _navigation;
    private readonly IThemeService _theme;
    private readonly IPopupService _popups;
    private readonly ILocalizer _localizer;
    private readonly ISessionService _session;

    public ShellViewModel(INavigationService navigation, IThemeService theme, IPopupService popups, ILocalizer localizer, ISessionService session)
    {
        _navigation = navigation;
        _theme = theme;
        _popups = popups;
        _localizer = localizer;
        _session = session;

        NavItems =
        [
            new NavItemViewModel(AppRoute.Dashboard, "Nav_Dashboard", "", OnNavSelected, localizer),
            new NavItemViewModel(AppRoute.Inventory, "Nav_Inventory", "", OnNavSelected, localizer),
            new NavItemViewModel(AppRoute.Sales, "Nav_Sales", "", OnNavSelected, localizer),
        ];

        _navigation.Navigated += (_, route) => SyncSelection(route);
        _theme.Changed += (_, _) => RefreshThemeState();
        _localizer.LanguageChanged += (_, _) => RefreshStrings();
        _session.SignedOut += (_, _) => SearchText = string.Empty;
        RefreshThemeState();
    }

    public ObservableCollection<NavItemViewModel> NavItems { get; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSidebarExpanded { get; set; } = true;

    public bool IsSidebarCollapsed => !IsSidebarExpanded;

    /// <summary>The sidebar plus the 12 px the floating collapse button overhangs the content column.</summary>
    public double SidebarColumnWidth => SidebarWidth + 12;

    /// <summary>8 px side padding in the 64 px rail so the 24 px icons fit; 12 px when expanded.</summary>
    public Microsoft.UI.Xaml.Thickness SidebarPadding => IsSidebarExpanded ? new Microsoft.UI.Xaml.Thickness(12, 16, 12, 16) : new Microsoft.UI.Xaml.Thickness(8, 16, 8, 16);

    [ObservableProperty]
    public partial double SidebarWidth { get; set; } = ExpandedWidth;

    /// <summary>Material glyph for the sidebar toggle.</summary>
    [ObservableProperty]
    public partial string SidebarToggleGlyph { get; set; } = "";

    public string SidebarToggleToolTip => _localizer[IsSidebarExpanded ? "Shell_CollapseSidebar" : "Shell_ExpandSidebar"];

    [ObservableProperty]
    public partial string ThemeGlyph { get; set; } = "";

    public string ThemeToolTip => _localizer[_theme.IsDark ? "Shell_ToLightTheme" : "Shell_ToDarkTheme"];

    /// <summary>Role of the signed-in user, in the app language.</summary>
    public string UserRole => _localizer[$"Role_{_session.Role}"];

    public Microsoft.UI.Xaml.HorizontalAlignment AccountAlignment =>
        IsSidebarExpanded ? Microsoft.UI.Xaml.HorizontalAlignment.Stretch : Microsoft.UI.Xaml.HorizontalAlignment.Center;

    public string UserDisplayName => _session.UserName ?? _localizer["Shell_UserName"];

    public string UserInitials => _session.UserName is { Length: > 0 } name ? InitialsOf(name) : _localizer["Shell_UserInitials"];

    /// <summary>First letters of the first two words, upper case: "Ana Pérez Ruiz" gives "AP".</summary>
    private static string InitialsOf(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(word => char.ToUpperInvariant(word[0])));

    partial void OnIsSidebarExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSidebarCollapsed));
        SidebarWidth = value ? ExpandedWidth : CollapsedWidth;
        OnPropertyChanged(nameof(SidebarColumnWidth));
        OnPropertyChanged(nameof(SidebarPadding));
        OnPropertyChanged(nameof(AccountAlignment));
        SidebarToggleGlyph = value ? "" : "";
        OnPropertyChanged(nameof(SidebarToggleToolTip));
        foreach (var item in NavItems)
        {
            item.IsExpanded = value;
        }
    }

    private void OnNavSelected(AppRoute route)
    {
        if (route != _navigation.Current)
        {
            _navigation.NavigateTo(route);
        }
    }

    private void SyncSelection(AppRoute route)
    {
        foreach (var item in NavItems)
        {
            item.IsSelected = item.Route == route;
        }
    }

    /// <summary>Re-reads every localized string after a language change.</summary>
    private void RefreshStrings()
    {
        foreach (var item in NavItems)
        {
            item.RefreshStrings();
        }

        OnPropertyChanged(string.Empty);
    }

    private void RefreshThemeState()
    {
        // Dark theme shows the sun (switches to light); light theme shows the moon (design system, top bar).
        ThemeGlyph = _theme.IsDark ? "" : "";
        OnPropertyChanged(nameof(ThemeToolTip));
    }

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

    [RelayCommand]
    private void OpenSettings() => _popups.Show(PopupKind.Settings);

    [RelayCommand]
    private void OpenPointOfSale() => _popups.Show(PopupKind.PointOfSale);

    [RelayCommand]
    private void OpenAddItems() => _popups.Show(PopupKind.AddItems);

    [RelayCommand]
    private void OpenStockAdjustments() => _popups.Show(PopupKind.StockAdjustments);

    [RelayCommand]
    private void OpenReturns() => _popups.Show(PopupKind.Returns);

    /// <summary>Ends the session (F12 and the Account settings button). The window then shows sign-in.</summary>
    [RelayCommand]
    private void LogOut() => _session.SignOut(SignOutReason.UserRequested);

    [RelayCommand]
    private void ToggleTheme() => _theme.Toggle();
}
