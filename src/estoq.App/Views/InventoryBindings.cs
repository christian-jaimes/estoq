using Microsoft.UI.Xaml;

namespace Estoq.App.Views;

/// <summary>Function bindings for the Inventory page (x:Bind functions must be static).</summary>
public static class InventoryBindings
{
    public static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility Hidden(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
}
