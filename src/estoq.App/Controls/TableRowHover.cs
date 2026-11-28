using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Controls;

/// <summary>
/// Row hover of the standard list table (docs/technical/ui-implementation.md, "Full-bleed table in a card"): shows or hides the
/// row's <c>HoverFill</c> border. Wire a row's PointerEntered to <c>Set(sender, 1)</c> and PointerExited to <c>Set(sender, 0)</c>.
/// </summary>
public static class TableRowHover
{
    public static void Set(object row, double opacity)
    {
        if (row is Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is Border { Name: "HoverFill" } fill)
                {
                    fill.Opacity = opacity;
                    return;
                }
            }
        }
    }
}
