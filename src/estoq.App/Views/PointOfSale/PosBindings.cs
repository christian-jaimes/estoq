using Microsoft.UI.Xaml;

namespace Estoq.App.Views.PointOfSale;

/// <summary>Function bindings for the point of sale view (x:Bind functions must be static).</summary>
public static class PosBindings
{
    public static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static bool Not(bool value) => !value;

    public static Visibility Hidden(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
}
