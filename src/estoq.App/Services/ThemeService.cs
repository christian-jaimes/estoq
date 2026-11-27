using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Estoq.App.Services;

/// <summary>
/// Applies the theme preference. Persistence to BusinessSettings arrives with the Settings
/// phase; until then the choice lasts for the session.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private FrameworkElement? _root;
    private bool _isFading;

    public ThemePreference Preference { get; private set; } = ThemePreference.System;

    public bool IsDark => (_root?.ActualTheme ?? ElementTheme.Dark) == ElementTheme.Dark;

    public Point? RevealOrigin { get; set; }

    public event EventHandler? Changed;

    public void Attach(FrameworkElement root)
    {
        ArgumentNullException.ThrowIfNull(root);

        _root = root;
        root.ActualThemeChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        SetRequestedTheme(Preference);
    }

    public void Apply(ThemePreference preference)
    {
        Preference = preference;
        if (_root is null)
        {
            return;
        }

        var requested = ToElementTheme(preference);
        if (_root.RequestedTheme == requested)
        {
            return;
        }

        // A soft cross-fade needs a panel to hold the snapshot; otherwise (or with animations off) switch straight away.
        if (_root is Grid host && !_isFading && new UISettings().AnimationsEnabled && _root.ActualWidth > 0 && _root.ActualHeight > 0)
        {
            var origin = RevealOrigin ?? new Point(host.ActualWidth / 2, host.ActualHeight / 2);
            RevealOrigin = null;
            _ = RevealAsync(host, requested, origin);
            return;
        }

        _root.RequestedTheme = requested;
    }

    private void SetRequestedTheme(ThemePreference preference)
    {
        if (_root is not null)
        {
            _root.RequestedTheme = ToElementTheme(preference);
        }
    }

    private static ElementTheme ToElementTheme(ThemePreference preference) => preference switch
    {
        ThemePreference.Light => ElementTheme.Light,
        ThemePreference.Dark => ElementTheme.Dark,
        _ => ElementTheme.Default,
    };

    /// <summary>
    /// The new theme grows out of <paramref name="origin"/> (the toggle button) as an expanding circle. A picture of the old
    /// screen covers the window while the theme changes underneath; a picture of the new screen is then revealed through a
    /// circular clip that grows until it covers everything. Any failure falls back to the plain switch.
    /// </summary>
    private async Task RevealAsync(Grid host, ElementTheme requested, Point origin)
    {
        _isFading = true;
        Image? before = null;
        Image? after = null;
        try
        {
            before = await ScreenSnapshot.CaptureAsync(host);
            host.Children.Add(before);
            host.RequestedTheme = requested;
            await ScreenSnapshot.NextFrameAsync();

            // Photograph the new look from the content only, so the old picture on top is not included.
            var content = host.Children[0] as FrameworkElement ?? host;
            after = await ScreenSnapshot.CaptureAsync(content);
            host.Children.Add(after);

            var visual = ElementCompositionPreview.GetElementVisual(after);
            var compositor = visual.Compositor;
            var circle = compositor.CreateEllipseGeometry();
            circle.Center = new Vector2((float)origin.X, (float)origin.Y);
            circle.Radius = Vector2.Zero;
            visual.Clip = compositor.CreateGeometricClip(circle);

            var far = Math.Sqrt(Math.Pow(Math.Max(origin.X, host.ActualWidth - origin.X), 2) + Math.Pow(Math.Max(origin.Y, host.ActualHeight - origin.Y), 2));
            var animation = compositor.CreateVector2KeyFrameAnimation();
            animation.Duration = TimeSpan.FromMilliseconds(450);
            animation.InsertKeyFrame(1f, new Vector2((float)far, (float)far),
                compositor.CreateCubicBezierEasingFunction(new Vector2(0.65f, 0f), new Vector2(0.35f, 1f)));

            var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            var oldPicture = before;
            var newPicture = after;
            batch.Completed += (_, _) =>
            {
                host.Children.Remove(oldPicture);
                host.Children.Remove(newPicture);
                _isFading = false;
            };
            circle.StartAnimation("Radius", animation);
            batch.End();
        }
        catch
        {
            if (before is not null)
            {
                host.Children.Remove(before);
            }

            if (after is not null)
            {
                host.Children.Remove(after);
            }

            host.RequestedTheme = requested;
            _isFading = false;
        }
    }

    public void Toggle()
    {
        Apply(IsDark ? ThemePreference.Light : ThemePreference.Dark);
    }
}
