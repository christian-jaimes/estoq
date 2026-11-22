using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Estoq.App.Services;

namespace Estoq.App.ViewModels;

/// <summary>One entry in the sidebar. Selection is two-way bound to a RadioButton.</summary>
public sealed partial class NavItemViewModel : ObservableObject
{
    private readonly Action<AppRoute> _onSelected;
    private readonly ILocalizer _localizer;
    private readonly string _labelKey;

    public NavItemViewModel(AppRoute route, string labelKey, string glyph, Action<AppRoute> onSelected, ILocalizer localizer)
    {
        Route = route;
        _labelKey = labelKey;
        _localizer = localizer;
        Glyph = glyph;
        _onSelected = onSelected;
    }

    public AppRoute Route { get; }

    public string Label => _localizer[_labelKey];

    /// <summary>Re-reads the label after a language change.</summary>
    public void RefreshStrings() => OnPropertyChanged(nameof(Label));

    /// <summary>Material Symbols Rounded code point (see Assets/Fonts/codepoints.txt).</summary>
    public string Glyph { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = true;

    public bool IsNotSelected => !IsSelected;

    /// <summary>Left-aligned when expanded; centred in the 64 px rail.</summary>
    public HorizontalAlignment ItemAlignment => IsExpanded ? HorizontalAlignment.Left : HorizontalAlignment.Center;

    /// <summary>No side padding in the rail so the 24 px icon fits the 36 px item width.</summary>
    public Thickness ItemPadding => IsExpanded ? new Thickness(12, 0, 12, 0) : new Thickness(0);

    partial void OnIsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(ItemAlignment)); OnPropertyChanged(nameof(ItemPadding));
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotSelected));
        if (value)
        {
            _onSelected(Route);
        }
    }
}
