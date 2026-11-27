using System.Globalization;
using Estoq.App.Controls;
using Estoq.App.Services;
using Estoq.App.ViewModels.PointOfSale;
using Estoq.Core.Sales;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.System;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Estoq.App.Views.PointOfSale;

/// <summary>
/// The F1 pop-up. Behaviour lives in <see cref="PointOfSaleViewModel"/>; this file only moves focus and
/// forwards keys and flyout buttons that XAML cannot bind.
/// </summary>
public sealed partial class PointOfSaleView : UserControl
{
    private readonly IPopupService _popups;
    private readonly IReceiptPrinter _receiptPrinter;
    private Flyout? _activePaymentFlyout;
    private PaymentMethod? _activePaymentMethod;
    private Storyboard? _paymentInputErrorStoryboard;
    private Storyboard? _paymentRowHoverStoryboard;
    private DispatcherQueueTimer? _shortcutHintTimer;
    private string _lastValidPaymentAmount = string.Empty;
    private int _lastValidPaymentCaret;
    private bool _isRestoringPaymentAmount;

    public PointOfSaleView()
    {
        ViewModel = App.Current.Services.GetRequiredService<PointOfSaleViewModel>();
        _popups = App.Current.Services.GetRequiredService<IPopupService>();
        _receiptPrinter = App.Current.Services.GetRequiredService<IReceiptPrinter>();
        InitializeComponent();
        _shortcutHintTimer = DispatcherQueue.CreateTimer();
        _shortcutHintTimer.Interval = TimeSpan.FromSeconds(4);
        _shortcutHintTimer.IsRepeating = false;
        _shortcutHintTimer.Tick += OnShortcutHintTimerTick;
        _popups.Changed += OnPopupChanged;
        Unloaded += OnUnloaded;
        if (_popups.Current == PopupKind.PointOfSale)
        {
            ShowShortcutHintsForFourSeconds();
        }
    }

    public PointOfSaleViewModel ViewModel { get; }

    public void ShowShortcutHintsForFourSeconds()
    {
        if (_shortcutHintTimer is null)
        {
            return;
        }

        _shortcutHintTimer.Stop();
        ViewModel.ShowShortcutHints = true;
        _shortcutHintTimer.Start();
    }

    private void OnShortcutHintTimerTick(DispatcherQueueTimer sender, object args) =>
        ViewModel.ShowShortcutHints = false;

    private void OnPopupChanged(object? sender, EventArgs e)
    {
        if (_popups.Current == PopupKind.PointOfSale)
        {
            ShowShortcutHintsForFourSeconds();
            return;
        }

        _shortcutHintTimer?.Stop();
        ViewModel.ShowShortcutHints = false;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _popups.Changed -= OnPopupChanged;
        _shortcutHintTimer?.Stop();
        if (_shortcutHintTimer is not null)
        {
            _shortcutHintTimer.Tick -= OnShortcutHintTimerTick;
        }
    }

    /// <summary>Puts the cursor in the scan field, or on the next-sale button once a sale is done.</summary>
    public bool FocusStart() =>
        ViewModel.IsCompleted ? NewSaleButton.Focus(FocusState.Programmatic) : ScanInput.Focus(FocusState.Programmatic);

    private void OnCloseClick(object sender, RoutedEventArgs e) => _popups.Close();

    private async void OnPrintReceiptClick(object sender, RoutedEventArgs e)
    {
        await _receiptPrinter.PrintAsync(ViewModel.DoneNumberText, ViewModel.BuildReceiptText());
    }

    private void OnScanKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            ViewModel.ScanCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnSuggestionClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: SaleVariant variant })
        {
            ViewModel.AddVariant(variant);
            ScanInput.Focus(FocusState.Programmatic);
        }
    }

    /// <summary>A typed quantity is applied when the box loses focus; Enter hands focus back to the scan field.</summary>
    private void OnQuantityCommit(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: CartLineViewModel line } box)
        {
            line.CommitQuantity(box.Text);
        }
    }

    private void OnQuantityKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            ScanInput.Focus(FocusState.Programmatic);
            e.Handled = true;
        }
    }

    private void OnClearCancel(object sender, RoutedEventArgs e) => ClearFlyout.Hide();

    private void OnClearConfirm(object sender, RoutedEventArgs e)
    {
        ClearFlyout.Hide();
        ViewModel.ClearSaleCommand.Execute(null);
        ScanInput.Focus(FocusState.Programmatic);
    }

    private void OnPaymentFlyoutOpening(object? sender, object e)
    {
        if (sender is not Flyout flyout)
        {
            return;
        }

        _activePaymentFlyout = flyout;
        _paymentInputErrorStoryboard?.Stop();
        _paymentInputErrorStoryboard = null;
        _lastValidPaymentAmount = string.Empty;
        _lastValidPaymentCaret = 0;
        ViewModel.AmountText = string.Empty;
        DispatcherQueue.TryEnqueue(() =>
        {
            if (FindDescendant<Button>(flyout.Content as DependencyObject) is { Tag: PaymentMethod method })
            {
                _activePaymentMethod = method;
            }

            if (FindDescendant<TextBlock>(flyout.Content as DependencyObject) is { } currencySymbol)
            {
                currencySymbol.Text = ViewModel.CurrentCurrencySymbol;
            }

            if (FindDescendant<TextBox>(flyout.Content as DependencyObject) is { } input)
            {
                input.Text = string.Empty;
                ResetPaymentInputVisual(input);
                input.Focus(FocusState.Programmatic);
            }
        });
    }

    private void OnPaymentMethodClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PaymentMethod method } button)
        {
            _activePaymentMethod = method;
            ViewModel.SelectMethod(method);
            if (button.Flyout is Flyout flyout && !flyout.IsOpen)
            {
                flyout.ShowAt(button);
            }
        }
    }

    private void OnPaymentMethodPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Button { Tag: PaymentMethod method, Flyout: Flyout flyout } button)
        {
            return;
        }

        _activePaymentMethod = method;
        ViewModel.SelectMethod(method);
        if (!flyout.IsOpen)
        {
            flyout.ShowAt(button);
        }
    }

    private void OnPaymentAmountChanged(object sender, TextChangedEventArgs e)
    {
        if (_isRestoringPaymentAmount || sender is not TextBox input)
        {
            return;
        }

        var candidate = input.Text;
        if (IsNumericEntry(candidate))
        {
            _lastValidPaymentAmount = candidate;
            _lastValidPaymentCaret = input.SelectionStart;
            ViewModel.AmountText = candidate;
            return;
        }

        ShakePaymentInput(input);
        _isRestoringPaymentAmount = true;
        input.Text = _lastValidPaymentAmount;
        input.SelectionStart = Math.Min(_lastValidPaymentCaret, input.Text.Length);
        _isRestoringPaymentAmount = false;
    }

    private bool IsNumericEntry(string value)
    {
        var parts = value.Split([ViewModel.AmountDecimalSeparator], StringSplitOptions.None);
        return parts.Length <= 2 && parts.All(part => part.All(char.IsAsciiDigit));
    }

    private void OnPaymentAmountKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            if (_activePaymentMethod is PaymentMethod method)
            {
                SubmitPayment(method);
            }

            e.Handled = true;
        }
    }

    private void OnPaymentSubmit(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PaymentMethod method })
        {
            SubmitPayment(method);
        }
    }

    /// <summary>Commits only the current payment amount; sale completion remains a separate action.</summary>
    private void SubmitPayment(PaymentMethod method)
    {
        if (ViewModel.CanAddPayment && ViewModel.AddPayment(method))
        {
            _activePaymentFlyout?.Hide();
            _activePaymentFlyout = null;
            _activePaymentMethod = null;
        }
    }

    private void OnPaymentRowPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement row)
        {
            AnimatePaymentRowHover(row, isHovered: true);
        }
    }

    private void OnPaymentRowPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement row)
        {
            AnimatePaymentRowHover(row, isHovered: false);
        }
    }

    private void AnimatePaymentRowHover(FrameworkElement row, bool isHovered)
    {
        var remove = FindDescendant<Button>(row);
        if (remove is null)
        {
            return;
        }

        remove.IsHitTestVisible = isHovered;
        _paymentRowHoverStoryboard?.Stop();

        var transform = remove.RenderTransform as CompositeTransform ?? new CompositeTransform();
        remove.RenderTransform = transform;
        var duration = TimeSpan.FromMilliseconds(160);
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var storyboard = new Storyboard();
        var slide = new DoubleAnimation
        {
            To = isHovered ? 0 : 12,
            Duration = duration,
            EasingFunction = easing,
        };
        Storyboard.SetTarget(slide, transform);
        Storyboard.SetTargetProperty(slide, "TranslateX");
        var fade = new DoubleAnimation
        {
            To = isHovered ? 1 : 0,
            Duration = duration,
            EasingFunction = easing,
        };
        Storyboard.SetTarget(fade, remove);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(slide);
        storyboard.Children.Add(fade);
        _paymentRowHoverStoryboard = storyboard;
        storyboard.Begin();
    }

    private static T? FindDescendant<T>(DependencyObject? parent) where T : DependencyObject
    {
        if (parent is null)
        {
            return null;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            if (FindDescendant<T>(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    private void ShakePaymentInput(TextBox input)
    {
        if (input.Parent is not Border border)
        {
            return;
        }

        _paymentInputErrorStoryboard?.Stop();
        var transform = ShakeMotion.Prepare(border);
        border.BorderBrush = ThemeBrushes.Get(border, "EqDangerBrush");
        var storyboard = new Storyboard();
        ShakeMotion.Add(storyboard, transform, 45, -4, 4, -2, 0);
        storyboard.Completed += (_, _) => ResetPaymentInputVisual(input);
        _paymentInputErrorStoryboard = storyboard;
        storyboard.Begin();
    }

    private static void ResetPaymentInputVisual(TextBox input)
    {
        if (input.Parent is not Border border)
        {
            return;
        }

        if (border.RenderTransform is CompositeTransform transform)
        {
            transform.TranslateX = 0;
        }

        border.BorderBrush = ThemeBrushes.Get(border, "EqBorderBrush");
    }

    private void OnDiscountOpening(object? sender, object e)
    {
        ViewModel.PrepareDiscountEditor();
    }

    private void OnDiscountPresetClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag }
            || !decimal.TryParse(tag, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var percent))
        {
            return;
        }

        if (ViewModel.TryApplyDiscountPreset(percent))
        {
            DiscountFlyout.Hide();
            ScanInput.Focus(FocusState.Programmatic);
        }
        else if (ViewModel.NeedsApproval)
        {
            DispatcherQueue.TryEnqueue(() => DiscountApprovalInput.Focus(FocusState.Programmatic));
        }
    }

    private void OnDiscountKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            OnDiscountApply(sender, e);
            e.Handled = true;
        }
    }

    private void OnDiscountApply(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TryApplyDiscount())
        {
            DiscountFlyout.Hide();
            ScanInput.Focus(FocusState.Programmatic);
        }
    }

    private void OnDiscountRemove(object sender, RoutedEventArgs e)
    {
        ViewModel.RemoveDiscountCommand.Execute(null);
        DiscountFlyout.Hide();
    }

    private void OnNewSaleClick(object sender, RoutedEventArgs e) =>
        DispatcherQueue.TryEnqueue(() => ScanInput.Focus(FocusState.Programmatic));
}
