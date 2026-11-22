using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Estoq.App.Controls;

/// <summary>An image that swaps its source with the theme (logos are single-ink PNGs; see design system, Logo).</summary>
public sealed class ThemedImage : UserControl
{
    public static readonly DependencyProperty LightSourceProperty =
        DependencyProperty.Register(nameof(LightSource), typeof(string), typeof(ThemedImage), new PropertyMetadata(string.Empty, OnSourceChanged));

    public static readonly DependencyProperty DarkSourceProperty =
        DependencyProperty.Register(nameof(DarkSource), typeof(string), typeof(ThemedImage), new PropertyMetadata(string.Empty, OnSourceChanged));

    private readonly Image _image = new() { Stretch = Stretch.Uniform };

    public ThemedImage()
    {
        Content = _image;
        Loaded += (_, _) => Update();
        ActualThemeChanged += (_, _) => Update();
    }

    public string LightSource
    {
        get => (string)GetValue(LightSourceProperty);
        set => SetValue(LightSourceProperty, value);
    }

    public string DarkSource
    {
        get => (string)GetValue(DarkSourceProperty);
        set => SetValue(DarkSourceProperty, value);
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ThemedImage)d).Update();

    private void Update()
    {
        var path = ActualTheme == ElementTheme.Light ? LightSource : DarkSource;
        if (!string.IsNullOrEmpty(path))
        {
            _image.Source = new BitmapImage(new Uri(path));
        }
    }
}
