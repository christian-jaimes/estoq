using Microsoft.UI.Xaml;

namespace Estoq.App.Services;

/// <summary>
/// Applies the theme preference. Persistence to BusinessSettings arrives with the Settings
/// phase; until then the choice lasts for the session.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private FrameworkElement? _root;

    public ThemePreference Preference { get; private set; } = ThemePreference.System;

    public bool IsDark => (_root?.ActualTheme ?? ElementTheme.Dark) == ElementTheme.Dark;

    public event EventHandler? Changed;

    public void Attach(FrameworkElement root)
    {
        _root = root;
        _root.ActualThemeChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        Apply(Preference);
    }

    public void Apply(ThemePreference preference)
    {
        Preference = preference;
        if (_root is null)
        {
            return;
        }

        _root.RequestedTheme = preference switch
        {
            ThemePreference.Light => ElementTheme.Light,
            ThemePreference.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
    }

    public void Toggle()
    {
        Apply(IsDark ? ThemePreference.Light : ThemePreference.Dark);
    }
}
