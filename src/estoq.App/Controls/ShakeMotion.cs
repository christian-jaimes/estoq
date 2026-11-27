using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Estoq.App.Controls;

/// <summary>The horizontal "invalid input" shake shared by every validated field.</summary>
internal static class ShakeMotion
{
    /// <summary>Returns the element's <see cref="CompositeTransform"/> (adding one if needed), reset to rest.</summary>
    public static CompositeTransform Prepare(UIElement element)
    {
        var transform = element.RenderTransform as CompositeTransform ?? new CompositeTransform();
        element.RenderTransform = transform;
        transform.TranslateX = 0;
        return transform;
    }

    /// <summary>Adds a shake that steps through <paramref name="offsets"/> every <paramref name="stepMilliseconds"/> ms, starting from 0.</summary>
    public static void Add(Storyboard storyboard, CompositeTransform transform, int stepMilliseconds, params double[] offsets)
    {
        var shake = new DoubleAnimationUsingKeyFrames();
        shake.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0 });
        for (var i = 0; i < offsets.Length; i++)
        {
            shake.KeyFrames.Add(new EasingDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(stepMilliseconds * (i + 1))),
                Value = offsets[i],
            });
        }

        Storyboard.SetTarget(shake, transform);
        Storyboard.SetTargetProperty(shake, "TranslateX");
        storyboard.Children.Add(shake);
    }
}
