using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Estoq.App.Controls;

/// <summary>A labelled text or password input with required or optional mark, suffix and error text.</summary>
public sealed partial class EqField : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(EqField), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(EqField), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(EqField), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ErrorTextProperty =
        DependencyProperty.Register(nameof(ErrorText), typeof(string), typeof(EqField), new PropertyMetadata(string.Empty, OnErrorChanged));

    public static readonly DependencyProperty SuffixProperty =
        DependencyProperty.Register(nameof(Suffix), typeof(string), typeof(EqField), new PropertyMetadata(string.Empty, OnDecorationChanged));

    public static readonly DependencyProperty IsRequiredProperty =
        DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(EqField), new PropertyMetadata(false, OnDecorationChanged));

    public static readonly DependencyProperty IsOptionalProperty =
        DependencyProperty.Register(nameof(IsOptional), typeof(bool), typeof(EqField), new PropertyMetadata(false, OnDecorationChanged));

    public static readonly DependencyProperty IsPasswordProperty =
        DependencyProperty.Register(nameof(IsPassword), typeof(bool), typeof(EqField), new PropertyMetadata(false, OnDecorationChanged));

    public EqField()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyDecoration();
        VisualStateManager.GoToState(this, "Unfocused", false);
        VisualStateManager.GoToState(this, "Valid", false);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public string ErrorText
    {
        get => (string)GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public string Suffix
    {
        get => (string)GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }

    public bool IsRequired
    {
        get => (bool)GetValue(IsRequiredProperty);
        set => SetValue(IsRequiredProperty, value);
    }

    public bool IsOptional
    {
        get => (bool)GetValue(IsOptionalProperty);
        set => SetValue(IsOptionalProperty, value);
    }

    public bool IsPassword
    {
        get => (bool)GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var field = (EqField)d;
        var hasError = !string.IsNullOrEmpty((string)e.NewValue);
        field.ErrorBlock.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;
        VisualStateManager.GoToState(field, hasError ? "Invalid" : "Valid", true);
    }

    private static void OnDecorationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((EqField)d).ApplyDecoration();

    private void ApplyDecoration()
    {
        RequiredMark.Visibility = IsRequired ? Visibility.Visible : Visibility.Collapsed;
        OptionalMark.Visibility = IsOptional && !IsRequired ? Visibility.Visible : Visibility.Collapsed;
        Input.Visibility = IsPassword ? Visibility.Collapsed : Visibility.Visible;
        SecretInput.Visibility = IsPassword ? Visibility.Visible : Visibility.Collapsed;
        SuffixText.Text = Suffix;
        SuffixText.Visibility = string.IsNullOrEmpty(Suffix) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnInputGotFocus(object sender, RoutedEventArgs e) => VisualStateManager.GoToState(this, "Focused", true);

    private void OnInputLostFocus(object sender, RoutedEventArgs e) => VisualStateManager.GoToState(this, "Unfocused", true);

    private void OnInputPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (Label != "Cost Price" && Label != "Selling Price") return;
        var box = (TextBox)sender;
        var key = e.Key.ToString();
        if (e.Key is Windows.System.VirtualKey.Back or Windows.System.VirtualKey.Delete or Windows.System.VirtualKey.Left or
            Windows.System.VirtualKey.Right or Windows.System.VirtualKey.Tab or Windows.System.VirtualKey.Home or Windows.System.VirtualKey.End) return;
        if (key.Length == 1 && char.IsDigit(key[0])) return;
        var separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        if (e.Key == Windows.System.VirtualKey.Decimal &&
            !box.Text.Contains(separator, StringComparison.Ordinal)) return;
        e.Handled = true;
        ErrorText = "Numbers only";
        if (Parent is FrameworkElement parent)
        {
            var transform = parent.RenderTransform as Microsoft.UI.Xaml.Media.CompositeTransform ?? new Microsoft.UI.Xaml.Media.CompositeTransform();
            parent.RenderTransform = transform;
            var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
            var animation = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimationUsingKeyFrames();
            animation.KeyFrames.Add(new Microsoft.UI.Xaml.Media.Animation.EasingDoubleKeyFrame { KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0 });
            animation.KeyFrames.Add(new Microsoft.UI.Xaml.Media.Animation.EasingDoubleKeyFrame { KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(45)), Value = -4 });
            animation.KeyFrames.Add(new Microsoft.UI.Xaml.Media.Animation.EasingDoubleKeyFrame { KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90)), Value = 4 });
            animation.KeyFrames.Add(new Microsoft.UI.Xaml.Media.Animation.EasingDoubleKeyFrame { KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(135)), Value = -2 });
            animation.KeyFrames.Add(new Microsoft.UI.Xaml.Media.Animation.EasingDoubleKeyFrame { KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180)), Value = 0 });
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(animation, transform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animation, "TranslateX");
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }
    }

    private void OnInputTextChanging(TextBox sender, TextBoxTextChangingEventArgs e)
    {
        if (Label != "Cost Price" && Label != "Selling Price") return;
        var separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        if (sender.Text.Length == 0 || (sender.Text.All(c => char.IsDigit(c) || c == separator[0]) && sender.Text.Count(c => c == separator[0]) <= 1))
        {
            ErrorText = string.Empty;
            return;
        }
        var filtered = new string(sender.Text.Where(c => char.IsDigit(c) || c == separator[0]).ToArray());
        if (filtered.Count(c => c == separator[0]) > 1) filtered = filtered[..filtered.LastIndexOf(separator[0])];
        sender.Text = filtered;
        sender.SelectionStart = filtered.Length;
    }
}
