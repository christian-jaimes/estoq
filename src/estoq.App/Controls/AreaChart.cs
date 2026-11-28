using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Path = Microsoft.UI.Xaml.Shapes.Path;
using Windows.Foundation;

namespace Estoq.App.Controls;

/// <summary>One point of an <see cref="AreaChart"/>: the text under the axis, the value, and the text of its tooltip.</summary>
public sealed record ChartPoint(string Label, decimal Value, string Tooltip);

/// <summary>
/// A gold area chart with value gridlines, labels under the axis and a hover marker with the exact value.
/// <see cref="AxisFormat"/> writes the value-axis labels (compact amounts).
/// </summary>
public sealed partial class AreaChart : Grid
{
    public static readonly DependencyProperty PointsProperty =
        DependencyProperty.Register(nameof(Points), typeof(IReadOnlyList<ChartPoint>), typeof(AreaChart), new PropertyMetadata(null, (d, _) => ((AreaChart)d).Draw()));

    private const double LeftGutter = 56;
    private const double BottomGutter = 28;
    private const double TopGutter = 12;
    private const double RightGutter = 12;
    private const int GridLines = 4;

    private readonly Canvas _canvas = new();
    private readonly Canvas _hover = new() { IsHitTestVisible = false };
    private readonly Border _tip = new() { Padding = new Thickness(10, 4, 10, 4), CornerRadius = new CornerRadius(999), Visibility = Visibility.Collapsed };
    private readonly TextBlock _tipText = new() { FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
    private readonly Ellipse _dot = new() { Width = 10, Height = 10, Visibility = Visibility.Collapsed };
    private readonly Line _guide = new() { StrokeThickness = 1, Visibility = Visibility.Collapsed };
    private List<Point> _positions = [];

    public AreaChart()
    {
        Background = new SolidColorBrush(Colors.Transparent);
        Children.Add(_canvas);
        Children.Add(_hover);
        _tip.Child = _tipText;
        _hover.Children.Add(_guide);
        _hover.Children.Add(_dot);
        _hover.Children.Add(_tip);
        SizeChanged += (_, _) => Draw();
        Loaded += (_, _) => Draw();
        ActualThemeChanged += (_, _) => Draw();
        PointerMoved += OnPointerMoved;
        PointerExited += (_, _) => HideHover();
    }

    public IReadOnlyList<ChartPoint>? Points
    {
        get => (IReadOnlyList<ChartPoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    /// <summary>Writes a value-axis label, for example "$1.2K". Without it the plain number is shown.</summary>
    public Func<decimal, string>? AxisFormat { get; set; }

    private void Draw()
    {
        _canvas.Children.Clear();
        _positions = [];
        HideHover();
        var points = Points;
        var plotWidth = ActualWidth - LeftGutter - RightGutter;
        var plotHeight = ActualHeight - TopGutter - BottomGutter;
        if (points is null || points.Count < 2 || plotWidth <= 0 || plotHeight <= 0)
        {
            return;
        }

        var secondary = Services.ThemeBrushes.Get(this, "EqTextSecondaryBrush");
        var border = Services.ThemeBrushes.Get(this, "EqBorderBrush");
        var gold = Services.ThemeBrushes.Get(this, "EqGoldBrush");

        // A round top value so the gridlines land on tidy numbers.
        var max = NiceCeiling(points.Max(point => point.Value));
        for (var i = 0; i <= GridLines; i++)
        {
            var y = TopGutter + plotHeight - plotHeight * i / GridLines;
            _canvas.Children.Add(new Line { X1 = LeftGutter, X2 = LeftGutter + plotWidth, Y1 = y, Y2 = y, Stroke = border, StrokeThickness = 1, Opacity = i == 0 ? 1 : 0.6 });
            var value = max * i / GridLines;
            var label = new TextBlock { Text = AxisFormat?.Invoke(value) ?? value.ToString("0"), FontSize = 12, Foreground = secondary, TextAlignment = Microsoft.UI.Xaml.TextAlignment.Right, Width = LeftGutter - 10 };
            Canvas.SetLeft(label, 0);
            Canvas.SetTop(label, y - 9);
            _canvas.Children.Add(label);
        }

        for (var i = 0; i < points.Count; i++)
        {
            var x = LeftGutter + plotWidth * i / (points.Count - 1);
            var y = TopGutter + plotHeight - (max == 0 ? 0 : (double)(points[i].Value / max)) * plotHeight;
            _positions.Add(new Point(x, y));
        }

        var linePoints = new PointCollection();
        var area = new PathFigure { StartPoint = new Point(_positions[0].X, TopGutter + plotHeight), IsClosed = true };
        foreach (var position in _positions)
        {
            area.Segments.Add(new LineSegment { Point = position });
            linePoints.Add(position);
        }

        area.Segments.Add(new LineSegment { Point = new Point(_positions[^1].X, TopGutter + plotHeight) });
        var areaGeometry = new PathGeometry();
        areaGeometry.Figures.Add(area);
        var fill = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        var goldColor = ((SolidColorBrush)gold).Color;
        fill.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(110, goldColor.R, goldColor.G, goldColor.B), Offset = 0 });
        fill.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0, goldColor.R, goldColor.G, goldColor.B), Offset = 1 });
        _canvas.Children.Add(new Path { Data = areaGeometry, Fill = fill });
        _canvas.Children.Add(new Polyline { Points = linePoints, Stroke = gold, StrokeThickness = 2.5, StrokeLineJoin = PenLineJoin.Round });

        // About seven labels under the axis, whatever the number of points.
        var step = Math.Max(1, (int)Math.Ceiling(points.Count / 7d));
        for (var i = 0; i < points.Count; i += step)
        {
            var label = new TextBlock { Text = points[i].Label, FontSize = 12, Foreground = secondary, TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center, Width = 64 };
            Canvas.SetLeft(label, _positions[i].X - 32);
            Canvas.SetTop(label, TopGutter + plotHeight + 8);
            _canvas.Children.Add(label);
        }

        _dot.Fill = gold;
        _dot.Stroke = Services.ThemeBrushes.Get(this, "EqSurfaceBrush");
        _dot.StrokeThickness = 2;
        _guide.Stroke = border;
        _guide.Y1 = TopGutter;
        _guide.Y2 = TopGutter + plotHeight;
        _tip.Background = gold;
        _tipText.Foreground = Services.ThemeBrushes.Get(this, "EqOnGoldBrush");
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_positions.Count < 2 || Points is not { } points)
        {
            return;
        }

        var x = e.GetCurrentPoint(this).Position.X;
        var nearest = 0;
        for (var i = 1; i < _positions.Count; i++)
        {
            if (Math.Abs(_positions[i].X - x) < Math.Abs(_positions[nearest].X - x))
            {
                nearest = i;
            }
        }

        var position = _positions[nearest];
        _guide.X1 = _guide.X2 = position.X;
        Canvas.SetLeft(_dot, position.X - 5);
        Canvas.SetTop(_dot, position.Y - 5);
        _tipText.Text = points[nearest].Tooltip;
        _tip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var left = Math.Clamp(position.X - _tip.DesiredSize.Width / 2, 0, Math.Max(0, ActualWidth - _tip.DesiredSize.Width));
        Canvas.SetLeft(_tip, left);
        Canvas.SetTop(_tip, Math.Max(0, position.Y - _tip.DesiredSize.Height - 12));
        _guide.Visibility = _dot.Visibility = _tip.Visibility = Visibility.Visible;
    }

    private void HideHover() => _guide.Visibility = _dot.Visibility = _tip.Visibility = Visibility.Collapsed;

    /// <summary>The next "round" number at or above the value: 1, 2, 2.5 or 5 times a power of ten. At least 4, so an empty chart still has an axis.</summary>
    internal static decimal NiceCeiling(decimal value)
    {
        if (value <= 4m)
        {
            return 4m;
        }

        var magnitude = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)value)));
        foreach (var factor in new[] { 1m, 2m, 2.5m, 5m, 10m })
        {
            if (value <= factor * magnitude)
            {
                return factor * magnitude;
            }
        }

        return 10m * magnitude;
    }
}
