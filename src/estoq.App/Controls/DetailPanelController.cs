using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Estoq.App.Controls;

/// <summary>
/// The standard row detail pop-up (docs/technical/ui-implementation.md, "Detail pop-up pattern"): slides a card in from
/// the right and out to the right in 100 ms, and decides which taps close it. Used by Inventory and Sales.
/// The card is a Border with <c>EqDetailPanelStyle</c> and a <see cref="CompositeTransform"/> as its RenderTransform.
/// </summary>
public sealed class DetailPanelController
{
    private static readonly Duration SlideDuration = new(TimeSpan.FromMilliseconds(100));

    private readonly FrameworkElement _panel;
    private readonly CompositeTransform _transform;
    private Storyboard? _storyboard;
    private bool _shown;

    public DetailPanelController(FrameworkElement panel, CompositeTransform transform)
    {
        _panel = panel;
        _transform = transform;
    }

    /// <summary>Slides the card in or out. Changing rows while it is open does not move it.</summary>
    public void Set(bool show, bool animate)
    {
        if (show == _shown)
        {
            return;
        }

        _shown = show;
        _storyboard?.Stop();
        var distance = _panel.Width + 40;

        if (!animate)
        {
            _panel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            _transform.TranslateX = 0;
            _panel.Opacity = 1;
            return;
        }

        // Stop() hands control back to the plain property values, which still hold the last end state. Set the start
        // state explicitly, otherwise closing starts from "hidden" and the card vanishes with no animation.
        _panel.Visibility = Visibility.Visible;
        _transform.TranslateX = show ? distance : 0;
        _panel.Opacity = show ? 0 : 1;

        var move = new DoubleAnimation { To = show ? 0 : distance, Duration = SlideDuration, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } };
        Storyboard.SetTarget(move, _transform);
        Storyboard.SetTargetProperty(move, "TranslateX");
        var fade = new DoubleAnimation { To = show ? 1 : 0, Duration = SlideDuration, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } };
        Storyboard.SetTarget(fade, _panel);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(move);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (_storyboard != storyboard)
            {
                return;
            }

            // Keep the end state as plain values, and collapse only once it has left the view so the exit is seen.
            storyboard.Stop();
            _transform.TranslateX = _shown ? 0 : distance;
            _panel.Opacity = _shown ? 1 : 0;
            if (!_shown)
            {
                _panel.Visibility = Visibility.Collapsed;
            }
        };
        _storyboard = storyboard;
        storyboard.Begin();
    }

    /// <summary>A tap outside the card closes it, unless it landed on a button (buttons do their own thing).</summary>
    public bool IsClosingTap(DependencyObject source, DependencyObject page) =>
        _shown && !IsInside(source, _panel) && !IsInsideButton(source, page);

    /// <summary>True when the tap landed on a button inside <paramref name="row"/>, so it is not a row click.</summary>
    public static bool IsInsideButton(DependencyObject source, DependencyObject row)
    {
        for (var current = source; current is not null && current != row; current = VisualTreeHelper.GetParent(current))
        {
            if (current is ButtonBase)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsInside(DependencyObject source, DependencyObject container)
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current == container)
            {
                return true;
            }
        }

        return false;
    }
}
