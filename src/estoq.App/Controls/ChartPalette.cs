using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Estoq.App.Services;

namespace Estoq.App.Controls;

/// <summary>The chart colours of the design system (<c>EqChart1Brush</c> to <c>EqChart4Brush</c>, then the muted one), resolved for the theme an element shows.</summary>
internal static class ChartPalette
{
    /// <summary>Slots 0 to 3 are the four chart colours; every higher slot is the muted colour.</summary>
    public const int Colors = 5;

    public static Brush Brush(FrameworkElement owner, int index) =>
        ThemeBrushes.Get(owner, index is >= 0 and <= 3 ? $"EqChart{index + 1}Brush" : "EqChartMutedBrush");
}
