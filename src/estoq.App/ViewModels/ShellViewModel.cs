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

    public ShellViewModel(INavigationService navigation, IThemeService theme, IPopupService popups)
    {
        _navigation = navigation;
        _theme = theme;
        _popups = popups;

        NavItems =
        [
            new NavItemViewModel(AppRoute.Dashboard, "Dashboard", "", OnNavSelected),
            new NavItemViewModel(AppRoute.Inventory, "Inventory", "", OnNavSelected),
            new NavItemViewModel(AppRoute.Sales, "Sales", "", OnNavSelected),
        ];

        _navigation.Navigated += (_, route) => SyncSelection(route);
        _theme.Changed += (_, _) => RefreshThemeState();
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

    [ObservableProperty]
    public partial string SidebarToggleToolTip { get; set; } = "Collapse sidebar";

    [ObservableProperty]
    public partial string ThemeGlyph { get; set; } = "";

    [ObservableProperty]
    public partial string ThemeToolTip { get; set; } = "Switch to light theme";

    /// <summary>Sample value until sign-in provides the real role.</summary>
    public string UserRole => "Owner";

    public Microsoft.UI.Xaml.HorizontalAlignment AccountAlignment =>
        IsSidebarExpanded ? Microsoft.UI.Xaml.HorizontalAlignment.Stretch : Microsoft.UI.Xaml.HorizontalAlignment.Center;

    public string UserDisplayName => "Local user";

    public string UserInitials => "LU";

    partial void OnIsSidebarExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSidebarCollapsed));
        SidebarWidth = value ? ExpandedWidth : CollapsedWidth;
        OnPropertyChanged(nameof(SidebarColumnWidth));
        OnPropertyChanged(nameof(SidebarPadding));
        OnPropertyChanged(nameof(AccountAlignment));
        SidebarToggleGlyph = value ? "" : "";
        SidebarToggleToolTip = value ? "Collapse sidebar" : "Expand sidebar";
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

    private void RefreshThemeState()
    {
        // Dark theme shows the sun (switches to light); light theme shows the moon (design system, top bar).
        ThemeGlyph = _theme.IsDark ? "" : "";
        ThemeToolTip = _theme.IsDark ? "Switch to light theme" : "Switch to dark theme";
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

    [RelayCommand]
    private void ToggleTheme() => _theme.Toggle();
}
