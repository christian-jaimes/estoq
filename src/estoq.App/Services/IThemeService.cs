using Microsoft.UI.Xaml;

namespace Estoq.App.Services;

public interface IThemeService
{
    ThemePreference Preference { get; }

    /// <summary>True when the dark theme is what the user currently sees.</summary>
    bool IsDark { get; }

    /// <summary>Raised after the displayed theme changes (including a Windows theme change).</summary>
    /// <summary>Where the next theme change grows from (window coordinates), set just before toggling. Cleared once used.</summary>
    Windows.Foundation.Point? RevealOrigin { get; set; }

    event EventHandler? Changed;

    /// <summary>Binds the service to the root element whose RequestedTheme it controls.</summary>
    void Attach(FrameworkElement root);

    void Apply(ThemePreference preference);

    /// <summary>Switches between light and dark based on what the user currently sees.</summary>
    void Toggle();
}
