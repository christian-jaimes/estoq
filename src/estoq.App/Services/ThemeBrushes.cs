using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.Services;

/// <summary>
/// Looks up a theme brush for the theme an element is actually showing. Code-behind needs this because
/// <c>Application.Current.Resources[key]</c> follows the Windows theme, not the app's light/dark choice.
/// </summary>
internal static class ThemeBrushes
{
    public static Brush Get(FrameworkElement owner, string key)
    {
        var themeKey = owner.ActualTheme == ElementTheme.Light ? "Light" : "Dark";
        var resources = Application.Current.Resources;
        return TryFind(resources, themeKey, key) ?? (Brush)resources[key];
    }

    private static Brush? TryFind(ResourceDictionary dictionary, string themeKey, string key)
    {
        if (dictionary.ThemeDictionaries.TryGetValue(themeKey, out var theme)
            && theme is ResourceDictionary themed
            && themed.TryGetValue(key, out var value)
            && value is Brush brush)
        {
            return brush;
        }

        foreach (var merged in dictionary.MergedDictionaries)
        {
            if (TryFind(merged, themeKey, key) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
