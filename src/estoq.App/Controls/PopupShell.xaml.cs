using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Estoq.App.Controls;

/// <summary>
/// Reusable pop-up frame: header (icon, title, subtitle, close), content area and footer actions.
/// Escape and the close button raise <see cref="CloseRequested"/>; the gold button raises <see cref="PrimaryInvoked"/>.
/// The host decides what to do with both, so the shell holds no flow logic.
/// </summary>
[ContentProperty(Name = nameof(Body))]
public sealed partial class PopupShell : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(PopupShell), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(PopupShell), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(PopupShell), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PrimaryTextProperty =
        DependencyProperty.Register(nameof(PrimaryText), typeof(string), typeof(PopupShell), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsPrimaryEnabledProperty =
        DependencyProperty.Register(nameof(IsPrimaryEnabled), typeof(bool), typeof(PopupShell), new PropertyMetadata(false));

    public static readonly DependencyProperty BodyProperty =
        DependencyProperty.Register(nameof(Body), typeof(object), typeof(PopupShell), new PropertyMetadata(null));

    public PopupShell()
    {
        InitializeComponent();
    }

    public event EventHandler? CloseRequested;

    public event EventHandler? PrimaryInvoked;

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>Material glyph for the header badge.</summary>
    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string PrimaryText
    {
        get => (string)GetValue(PrimaryTextProperty);
        set => SetValue(PrimaryTextProperty, value);
    }

    public bool IsPrimaryEnabled
    {
        get => (bool)GetValue(IsPrimaryEnabledProperty);
        set => SetValue(IsPrimaryEnabledProperty, value);
    }

    /// <summary>The flow content shown in the scrolling area.</summary>
    public object? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    /// <summary>Moves keyboard focus into the pop-up so Esc and Tab act on it.</summary>
    public bool FocusFirst() => CloseButton.Focus(FocusState.Programmatic);

    private void OnCloseClick(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void OnPrimaryClick(object sender, RoutedEventArgs e) => PrimaryInvoked?.Invoke(this, EventArgs.Empty);
}
