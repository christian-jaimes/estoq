using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Controls;

/// <summary>A status pill with a dot and words. <see cref="Kind"/> is "success", "warning", "danger" or "neutral".</summary>
public sealed partial class StatusBadge : UserControl
{
    public static readonly DependencyProperty KindProperty =
        DependencyProperty.Register(nameof(Kind), typeof(string), typeof(StatusBadge), new PropertyMetadata("neutral", OnKindChanged));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(StatusBadge), new PropertyMetadata(string.Empty, OnTextChanged));

    public StatusBadge()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateState();
    }

    public string Kind
    {
        get => (string)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((StatusBadge)d).UpdateState();

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((StatusBadge)d).Label.Text = (string?)e.NewValue ?? string.Empty;

    private void UpdateState()
    {
        var state = Kind switch
        {
            "success" => "Success",
            "warning" => "Warning",
            "danger" => "Danger",
            _ => "Neutral",
        };
        VisualStateManager.GoToState(this, state, false);
    }
}
