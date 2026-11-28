using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.Views;

/// <summary>Function bindings for the Inventory page (x:Bind functions must be static).</summary>
public static class InventoryBindings
{
    public static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility Hidden(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    private static readonly SolidColorBrush DarkInk = new(Colors.Black);
    private static readonly SolidColorBrush LightInk = new(Colors.White);

    /// <summary>The fill of a colour circle; null (no fill) when the colour is unknown.</summary>
    public static Brush? SwatchFill(string hex) => Estoq.App.ViewModels.AddProduct.Swatch.FromHex(hex);

    /// <summary>The text colour for a size written in a colour circle.</summary>
    public static Brush SwatchInk(bool useDarkText) => useDarkText ? DarkInk : LightInk;
}
