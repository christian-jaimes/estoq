using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Path = Microsoft.UI.Xaml.Shapes.Path;
using Windows.Foundation;

namespace Estoq.App.Controls;

/// <summary>One slice of a <see cref="DonutChart"/>: its share of the ring (0 to 1) and its chart colour index.</summary>
public sealed record DonutSlice(double Fraction, int ColorIndex);

/// <summary>A ring chart. Put the total in a child on top of it; the ring fills the smaller side of the control.</summary>
public sealed partial class DonutChart : Grid
{
    public static readonly DependencyProperty SlicesProperty =
        DependencyProperty.Register(nameof(Slices), typeof(IReadOnlyList<DonutSlice>), typeof(DonutChart), new PropertyMetadata(null, (d, _) => ((DonutChart)d).Draw()));

    private const double Thickness = 22;
    private const double GapDegrees = 2;

    private readonly Canvas _canvas = new();

    public DonutChart()
    {
        Children.Add(_canvas);
        SizeChanged += (_, _) => Draw();
        Loaded += (_, _) => Draw();
        ActualThemeChanged += (_, _) => Draw();
    }

    public IReadOnlyList<DonutSlice>? Slices
    {
        get => (IReadOnlyList<DonutSlice>?)GetValue(SlicesProperty);
        set => SetValue(SlicesProperty, value);
    }

    private void Draw()
    {
        _canvas.Children.Clear();
        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= Thickness * 2)
        {
            return;
        }

        var radius = (size - Thickness) / 2;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var slices = Slices?.Where(slice => slice.Fraction > 0).ToList() ?? [];
        if (slices.Count == 0)
        {
            // Nothing to show: a muted empty ring keeps the card's shape.
            AddRing(center, radius, Services.ThemeBrushes.Get(this, "EqSurfaceElevatedBrush"));
            return;
        }

        if (slices.Count == 1)
        {
            AddRing(center, radius, ChartPalette.Brush(this, slices[0].ColorIndex));
            return;
        }

        var start = -90d;
        foreach (var slice in slices)
        {
            var sweep = slice.Fraction * 360d;
            var from = start + GapDegrees / 2;
            var to = start + sweep - GapDegrees / 2;
            start += sweep;
            if (to <= from)
            {
                continue;
            }

            var figure = new PathFigure { StartPoint = OnRing(center, radius, from), IsClosed = false };
            figure.Segments.Add(new ArcSegment
            {
                Point = OnRing(center, radius, to),
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = to - from > 180,
            });
            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            _canvas.Children.Add(new Path
            {
                Data = geometry,
                Stroke = ChartPalette.Brush(this, slice.ColorIndex),
                StrokeThickness = Thickness,
            });
        }
    }

    private void AddRing(Point center, double radius, Brush stroke)
    {
        var ring = new Ellipse { Width = radius * 2, Height = radius * 2, StrokeThickness = Thickness, Stroke = stroke };
        Canvas.SetLeft(ring, center.X - radius);
        Canvas.SetTop(ring, center.Y - radius);
        _canvas.Children.Add(ring);
    }

    private static Point OnRing(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180d;
        return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }
}
