using System.ComponentModel;
using Estoq.App.Controls;
using Estoq.App.Services;
using Estoq.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;

namespace Estoq.App.Views.SignIn;

/// <summary>Full-window sign-in (docs/features/sign-in.md): code first, email and password on demand.</summary>
public sealed partial class SignInPage : Page
{
    private bool? _lastEmailMode;
    private bool _identifierHasFocus;
    private bool _passwordHasFocus;
    private Storyboard? _failureStoryboard;
    private bool _isTransitioningToShell;

    public SignInPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<SignInViewModel>();
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ActualThemeChanged += (_, _) => ApplySilk();
    }

    /// <summary>The silk background follows the app theme: dark texture in dark, ivory in light.</summary>
    private void ApplySilk() =>
        SilkImage.Source = new BitmapImage(new Uri(ActualTheme == ElementTheme.Light
            ? "ms-appx:///Assets/Backgrounds/signin-silk-light.jpg"
            : "ms-appx:///Assets/Backgrounds/signin-silk.jpg"));

    public SignInViewModel ViewModel { get; }

    /// <summary>The window uses this area as its title bar so the app can be dragged from it.</summary>
    public UIElement TitleBarDragRegion => DragRegion;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.SignedIn += OnSignedIn;
        ViewModel.FailureAnimationRequested += OnFailureAnimationRequested;
        _lastEmailMode = ViewModel.IsEmailMode;
        ApplySilk();
        ApplyMessageColor();
        CodeInput.Focus(FocusState.Programmatic);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.SignedIn -= OnSignedIn;
        ViewModel.FailureAnimationRequested -= OnFailureAnimationRequested;
        ViewModel.Stop();
    }

    private void OnSignedIn(object? sender, EventArgs e) =>
        DispatcherQueue.TryEnqueue(BeginShellTransition);

    private void BeginShellTransition()
    {
        if (_isTransitioningToShell)
        {
            return;
        }

        // The window cross-fades from this screen to the shell, so the page itself does not animate out.
        _isTransitioningToShell = true;
        SignInRoot.IsHitTestVisible = false;
        App.Current.MainWindow?.ShowShell();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SignInViewModel.IsMessageError):
                ApplyMessageColor();
                break;
            case nameof(SignInViewModel.IsPasswordRevealed):
                PasswordInput.PasswordRevealMode = ViewModel.IsPasswordRevealed ? PasswordRevealMode.Visible : PasswordRevealMode.Hidden;
                break;
            case nameof(SignInViewModel.IsEmailMode):
                if (_lastEmailMode != ViewModel.IsEmailMode)
                {
                    _lastEmailMode = ViewModel.IsEmailMode;
                    OnModeChanged();
                }
                break;
            case nameof(SignInViewModel.IsLocked) when !ViewModel.IsLocked:
                // The inputs were disabled during the lock; hand focus back.
                DispatcherQueue.TryEnqueue(() => FocusIdentifier());
                break;
        }
    }

    /// <summary>Moves the form smoothly when switching between code and email sign-in.</summary>
    private void OnModeChanged()
    {
        var showingPassword = ViewModel.IsEmailMode;
        const double travel = 10;
        const double passwordRowSpace = 72;
        const double centeredContentOffset = passwordRowSpace / 2;
        var duration = TimeSpan.FromMilliseconds(230);
        var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

        var passwordSlide = PasswordBox.RenderTransform as CompositeTransform ?? new CompositeTransform();
        PasswordBox.RenderTransform = passwordSlide;
        var topSlide = (CompositeTransform)TopPanel.RenderTransform;
        var actionsSlide = (CompositeTransform)ActionsPanel.RenderTransform;

        PasswordBox.Visibility = Visibility.Visible;
        PasswordBox.IsHitTestVisible = showingPassword;

        if (showingPassword)
        {
            passwordSlide.TranslateY = -travel;
            topSlide.TranslateY = centeredContentOffset;
            actionsSlide.TranslateY = -centeredContentOffset;
            PasswordBox.Opacity = 0;
        }

        var storyboard = new Storyboard();
        AddFadeSlideMotion(storyboard, PasswordBox, passwordSlide, showingPassword, travel, duration, easing);
        AddSlideMotion(storyboard, topSlide, showingPassword ? 0 : centeredContentOffset, duration, easing);
        AddSlideMotion(storyboard, actionsSlide, showingPassword ? 0 : -centeredContentOffset, duration, easing);

        if (!showingPassword)
        {
            storyboard.Completed += (_, _) =>
            {
                PasswordBox.Visibility = Visibility.Collapsed;
                topSlide.TranslateY = 0;
                actionsSlide.TranslateY = 0;
            };
        }

        // The visible identifier control changes with the mode. Restore focus after the
        // visibility bindings have updated, keeping the user's typing flow uninterrupted.
        DispatcherQueue.TryEnqueue(() => FocusIdentifier());

        storyboard.Begin();
    }

    private void FocusIdentifier()
    {
        if (ViewModel.IsEmailMode)
        {
            EmailIdentifierInput.Focus(FocusState.Programmatic);
            EmailIdentifierInput.SelectionStart = EmailIdentifierInput.Text.Length;
            return;
        }

        CodeInput.Focus(FocusState.Programmatic);
    }

    private static void AddFadeSlideMotion(Storyboard storyboard, UIElement element, CompositeTransform transform,
        bool showing, double travel, TimeSpan duration, CubicEase easing)
    {
        var fade = new DoubleAnimation
        {
            To = showing ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, element);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var move = new DoubleAnimation
        {
            To = showing ? 0 : -travel,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(move, transform);
        Storyboard.SetTargetProperty(move, "TranslateY");
        storyboard.Children.Add(fade);
        storyboard.Children.Add(move);
    }

    private static void AddSlideMotion(Storyboard storyboard, CompositeTransform transform,
        double targetOffset, TimeSpan duration, CubicEase easing)
    {
        var move = new DoubleAnimation
        {
            To = targetOffset,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(move, transform);
        Storyboard.SetTargetProperty(move, "TranslateY");
        storyboard.Children.Add(move);
    }

    private void ApplyMessageColor() =>
        MessageText.Foreground = ThemeBrushes.Get(MessageText, ViewModel.IsMessageError ? "EqDangerBrush" : "EqTextSecondaryBrush");

    private void OnFailureAnimationRequested(bool emailMode)
    {
        _failureStoryboard?.Stop();

        if (emailMode)
        {
            AddInvalidFieldMotion(IdentifierBox);
            AddInvalidFieldMotion(PasswordBox);
        }
        else
        {
            AddInvalidFieldMotion(IdentifierBox);
        }

        var storyboard = new Storyboard();
        AddShake(storyboard, IdentifierBox);
        if (emailMode)
        {
            AddShake(storyboard, PasswordBox);
        }

        storyboard.Completed += (_, _) =>
        {
            RestoreFieldBorder(IdentifierBox, _identifierHasFocus);
            if (emailMode)
            {
                RestoreFieldBorder(PasswordBox, _passwordHasFocus);
            }
        };
        _failureStoryboard = storyboard;
        storyboard.Begin();
    }

    private static void AddInvalidFieldMotion(FrameworkElement element)
    {
        ShakeMotion.Prepare(element);
        ((Border)element).BorderBrush = ThemeBrushes.Get(element, "EqDangerBrush");
    }

    private static void AddShake(Storyboard storyboard, FrameworkElement element) =>
        ShakeMotion.Add(storyboard, (CompositeTransform)element.RenderTransform, 45, -8, 8, -6, 6, 0);

    private static void RestoreFieldBorder(Border border, bool hasFocus)
    {
        if (border.RenderTransform is CompositeTransform transform)
        {
            transform.TranslateX = 0;
        }

        SetGoldBorder(border, hasFocus);
    }

    private void OnIdentifierGotFocus(object sender, RoutedEventArgs e)
    {
        _identifierHasFocus = true;
        SetGoldBorder(IdentifierBox, true);
    }

    private void OnIdentifierLostFocus(object sender, RoutedEventArgs e)
    {
        _identifierHasFocus = false;
        SetGoldBorder(IdentifierBox, false);
    }

    private void OnPasswordGotFocus(object sender, RoutedEventArgs e)
    {
        _passwordHasFocus = true;
        SetGoldBorder(PasswordBox, true);
    }

    private void OnPasswordLostFocus(object sender, RoutedEventArgs e)
    {
        _passwordHasFocus = false;
        SetGoldBorder(PasswordBox, false);
    }

    private static void SetGoldBorder(Border box, bool focused) =>
        box.BorderBrush = ThemeBrushes.Get(box, focused ? "EqGoldBrush" : "EqBorderBrush");

    private void OnIdentifierKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
        {
            return;
        }

        e.Handled = true;
        if (ViewModel.IsEmailMode && string.IsNullOrEmpty(ViewModel.Password))
        {
            PasswordInput.Focus(FocusState.Keyboard);
        }
        else
        {
            ViewModel.SubmitCommand.Execute(null);
        }
    }

    private void OnPasswordKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            ViewModel.SubmitCommand.Execute(null);
        }
    }
}
