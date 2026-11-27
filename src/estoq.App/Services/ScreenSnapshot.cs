using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Estoq.App.Services;

/// <summary>Pictures of the screen used to cross-fade between states (theme change, page change) without a visible jump.</summary>
internal static class ScreenSnapshot
{
    /// <summary>A picture of <paramref name="element"/> as it looks now, sized to match it, ready to lay over it.</summary>
    public static async Task<Image> CaptureAsync(FrameworkElement element)
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(element);
        var image = new Image
        {
            Source = bitmap,
            Stretch = Stretch.Fill,
            Width = element.ActualWidth,
            Height = element.ActualHeight,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            IsHitTestVisible = false,
        };
        Canvas.SetZIndex(image, 1000);
        return image;
    }

    /// <summary>Lets a change reach the screen before it is photographed.</summary>
    public static Task NextFrameAsync()
    {
        var done = new TaskCompletionSource();
        void OnRendering(object? sender, object e)
        {
            Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnRendering;
            done.TrySetResult();
        }

        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnRendering;
        return done.Task;
    }
}
