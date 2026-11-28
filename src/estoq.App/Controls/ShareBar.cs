using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Controls;

/// <summary>A horizontal bar filled to <see cref="Fraction"/> (0 to 1) of its width, on a track, in a chart colour. Used for sales by category.</summary>
public sealed partial class ShareBar : Grid
{
    public static readonly DependencyProperty FractionProperty =
        DependencyProperty.Register(nameof(Fraction), typeof(double), typeof(ShareBar), new PropertyMetadata(0d, (d, _) => ((ShareBar)d).UpdateFill()));

    public static readonly DependencyProperty ColorIndexProperty =
        DependencyProperty.Register(nameof(ColorIndex), typeof(int), typeof(ShareBar), new PropertyMetadata(0, (d, _) => ((ShareBar)d).Apply()));

    private readonly Border _track = new() { CornerRadius = new CornerRadius(4) };
    private readonly Border _fill = new() { CornerRadius = new CornerRadius(4), HorizontalAlignment = HorizontalAlignment.Left };

    public ShareBar()
    {
        Height = 8;
        Children.Add(_track);
        Children.Add(_fill);
        SizeChanged += (_, _) => UpdateFill();
        Loaded += (_, _) => Apply();
        ActualThemeChanged += (_, _) => Apply();
    }

    public double Fraction
    {
        get => (double)GetValue(FractionProperty);
        set => SetValue(FractionProperty, value);
    }

    public int ColorIndex
    {
        get => (int)GetValue(ColorIndexProperty);
        set => SetValue(ColorIndexProperty, value);
    }

    private void UpdateFill() => _fill.Width = Math.Max(0, Math.Min(1, Fraction)) * ActualWidth;

    private void Apply()
    {
        _track.Background = Services.ThemeBrushes.Get(this, "EqSurfaceElevatedBrush");
        _fill.Background = ChartPalette.Brush(this, ColorIndex);
    }
}
