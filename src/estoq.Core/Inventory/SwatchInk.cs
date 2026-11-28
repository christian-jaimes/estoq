using System.Globalization;

namespace Estoq.Core.Inventory;

/// <summary>Picks readable text for a colour swatch, such as the size written inside a colour circle.</summary>
public static class SwatchInk
{
    /// <summary>
    /// True when dark text reads better on <paramref name="hex"/> ("#RRGGBB"), false when light text does.
    /// An unreadable colour counts as light-on-neutral, so dark text is used.
    /// </summary>
    public static bool UseDarkText(string? hex)
    {
        if (hex is not { Length: 7 } || hex[0] != '#'
            || !int.TryParse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            return true;
        }

        var red = (rgb >> 16) & 0xFF;
        var green = (rgb >> 8) & 0xFF;
        var blue = rgb & 0xFF;
        return ((0.299 * red) + (0.587 * green) + (0.114 * blue)) / 255d > 0.55;
    }
}
