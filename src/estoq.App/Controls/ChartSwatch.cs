using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Controls;

/// <summary>A small round colour key for a chart legend, in the chart colour with the given index. Follows the theme.</summary>
public sealed partial class ChartSwatch : Border
{
    public static readonly DependencyProperty ColorIndexProperty =
        DependencyProperty.Register(nameof(ColorIndex), typeof(int), typeof(ChartSwatch), new PropertyMetadata(0, (d, _) => ((ChartSwatch)d).Apply()));

    public ChartSwatch()
    {
        Width = 10;
        Height = 10;
        CornerRadius = new CornerRadius(5);
        Loaded += (_, _) => Apply();
        ActualThemeChanged += (_, _) => Apply();
    }

    public int ColorIndex
    {
        get => (int)GetValue(ColorIndexProperty);
        set => SetValue(ColorIndexProperty, value);
    }

    private void Apply() => Background = ChartPalette.Brush(this, ColorIndex);
}
