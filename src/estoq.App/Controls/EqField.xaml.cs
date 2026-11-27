using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

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

    public static readonly DependencyProperty IsNumericProperty =
        DependencyProperty.Register(nameof(IsNumeric), typeof(bool), typeof(EqField), new PropertyMetadata(false));

    public static readonly DependencyProperty NumericErrorTextProperty =
        DependencyProperty.Register(nameof(NumericErrorText), typeof(string), typeof(EqField), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ShowErrorTextProperty =
        DependencyProperty.Register(nameof(ShowErrorText), typeof(bool), typeof(EqField), new PropertyMetadata(true, OnErrorTextVisibilityChanged));

    public static readonly DependencyProperty ShowLabelProperty =
        DependencyProperty.Register(nameof(ShowLabel), typeof(bool), typeof(EqField), new PropertyMetadata(true, OnDecorationChanged));

    private bool _updatingNumericText;
    private Storyboard? _invalidInputStoryboard;

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

    public bool IsNumeric
    {
        get => (bool)GetValue(IsNumericProperty);
        set => SetValue(IsNumericProperty, value);
    }

    public string NumericErrorText
    {
        get => (string)GetValue(NumericErrorTextProperty);
        set => SetValue(NumericErrorTextProperty, value);
    }

    public bool ShowErrorText
    {
        get => (bool)GetValue(ShowErrorTextProperty);
        set => SetValue(ShowErrorTextProperty, value);
    }

    public bool ShowLabel
    {
        get => (bool)GetValue(ShowLabelProperty);
        set => SetValue(ShowLabelProperty, value);
    }

    public event EventHandler? ErrorTextChanged;

    private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var field = (EqField)d;
        var hasError = !string.IsNullOrEmpty((string)e.NewValue);
        field.ErrorBlock.Visibility = hasError && field.ShowErrorText ? Visibility.Visible : Visibility.Collapsed;
        VisualStateManager.GoToState(field, hasError ? "Invalid" : "Valid", true);
        field.Box.BorderBrush = hasError
            ? (Brush)Application.Current.Resources["EqDangerBrush"]
            : field.Input.FocusState == FocusState.Unfocused
                ? (Brush)Application.Current.Resources["EqBorderBrush"]
                : (Brush)Application.Current.Resources["EqGoldBrush"];
        field.ErrorTextChanged?.Invoke(field, EventArgs.Empty);
    }

    private static void OnErrorTextVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var field = (EqField)d;
        field.ErrorBlock.Visibility = field.ShowErrorText && !string.IsNullOrEmpty(field.ErrorText)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static void OnDecorationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((EqField)d).ApplyDecoration();

    private void ApplyDecoration()
    {
        LabelContainer.Visibility = ShowLabel ? Visibility.Visible : Visibility.Collapsed;
        RequiredMark.Visibility = IsRequired ? Visibility.Visible : Visibility.Collapsed;
        OptionalMark.Visibility = IsOptional && !IsRequired ? Visibility.Visible : Visibility.Collapsed;
        Input.Visibility = IsPassword ? Visibility.Collapsed : Visibility.Visible;
        SecretInput.Visibility = IsPassword ? Visibility.Visible : Visibility.Collapsed;
        SuffixText.Text = Suffix;
        SuffixText.Visibility = string.IsNullOrEmpty(Suffix) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnInputGotFocus(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "Focused", true);
        if (string.IsNullOrEmpty(ErrorText)) Box.BorderBrush = (Brush)Application.Current.Resources["EqGoldBrush"];
    }

    private void OnInputLostFocus(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "Unfocused", true);
        if (string.IsNullOrEmpty(ErrorText)) Box.BorderBrush = (Brush)Application.Current.Resources["EqBorderBrush"];
    }

    private void OnInputTextChanging(TextBox sender, TextBoxTextChangingEventArgs e)
    {
        if (!IsNumeric || _updatingNumericText) return;
        var separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var decimalSeparator = separator.Length == 0 ? '.' : separator[0];
        var raw = sender.Text;
        var caret = Math.Clamp(sender.SelectionStart, 0, raw.Length);
        var filtered = FilterNumericText(raw, decimalSeparator);
        if (raw == filtered)
        {
            if (ErrorText == NumericErrorText) ErrorText = string.Empty;
            return;
        }

        var caretText = FilterNumericText(raw[..caret], decimalSeparator);
        _updatingNumericText = true;
        try
        {
            sender.Text = filtered;
            sender.SelectionStart = Math.Min(caretText.Length, filtered.Length);
        }
        finally
        {
            _updatingNumericText = false;
        }

        ErrorText = NumericErrorText;
        ShakeFieldContainer();
    }

    private static string FilterNumericText(string value, char decimalSeparator)
    {
        var result = new System.Text.StringBuilder(value.Length);
        var hasSeparator = false;
        foreach (var character in value)
        {
            if (char.IsDigit(character)) result.Append(character);
            else if (character == decimalSeparator && !hasSeparator)
            {
                result.Append(character);
                hasSeparator = true;
            }
        }
        return result.ToString();
    }

    private void ShakeFieldContainer()
    {
        _invalidInputStoryboard?.Stop();
        var transform = Box.RenderTransform as CompositeTransform ?? new CompositeTransform();
        Box.RenderTransform = transform;
        transform.TranslateX = 0;
        var storyboard = new Storyboard();
        var shake = new DoubleAnimationUsingKeyFrames();
        shake.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0 });
        shake.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(35)), Value = -4 });
        shake.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(70)), Value = 4 });
        shake.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(105)), Value = -2 });
        shake.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140)), Value = 0 });
        Storyboard.SetTarget(shake, transform);
        Storyboard.SetTargetProperty(shake, "TranslateX");
        storyboard.Children.Add(shake);
        storyboard.Completed += (_, _) => transform.TranslateX = 0;
        _invalidInputStoryboard = storyboard;
        storyboard.Begin();
    }
}
