using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.Views;

/// <summary>Function bindings for the dashboard page (x:Bind functions must be static).</summary>
public static class DashboardBindings
{
    public static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility Hidden(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>The tone of a change is shown by arrow and words as well as colour: 1 good, -1 bad, 0 neutral.</summary>
    public static Visibility WhenTone(int tone, int wanted) => tone == wanted ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Success or danger colour for a KPI change. The two tokens are the same in light and dark, so the application-level lookup is safe.</summary>
    public static Brush ToneBrush(int tone) => (Brush)Application.Current.Resources[tone < 0 ? "EqDangerBrush" : "EqSuccessBrush"];

    /// <summary>Danger fill for the scarcest stock, warning fill for the rest (same in both themes).</summary>
    public static Brush PillFill(bool critical) => (Brush)Application.Current.Resources[critical ? "EqDangerBrush" : "EqWarningBrush"];

    public static Visibility PillTextWhite(bool critical) => critical ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility PillTextDark(bool critical) => critical ? Visibility.Collapsed : Visibility.Visible;
}
