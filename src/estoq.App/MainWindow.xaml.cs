using Estoq.App.Services;
using Estoq.App.Views;
using Estoq.App.Views.Setup;
using Estoq.App.Views.SignIn;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;

namespace Estoq.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "estoq";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1440, 900));
        // Always open maximized; the size above is what the window restores to.
        (AppWindow.Presenter as OverlappedPresenter)?.Maximize();
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        // Use the initialized window content as the root. This also avoids a null generated
        // x:Name field when Visual Studio launches with stale XAML design-time artifacts.
        var root = Content as FrameworkElement
            ?? throw new InvalidOperationException("MainWindow XAML did not create its root content.");

        // Like Root, resolve the named child from the live XAML tree as well as the generated field.
        // This keeps startup working if Visual Studio has stale generated name fields after a XAML edit.
        HostFrame ??= root.FindName("HostFrame") as Frame ?? FindVisualDescendant<Frame>(root)
            ?? throw new InvalidOperationException("MainWindow XAML did not create its host frame.");

        AppServices.Get<IThemeService>().Attach(root);
        AppServices.Get<IThemeService>().Changed += (_, _) => UpdateCaptionButtons();
        UpdateCaptionButtons();

        // Logging out, by choice or by inactivity, always lands on the sign-in screen.
        AppServices.Get<IInactivityMonitor>().Attach(root, DispatcherQueue);
        AppServices.Get<ISessionService>().SignedOut += (_, _) => DispatcherQueue.TryEnqueue(() =>
        {
            AppServices.Get<IPopupService>().Close();
            ShowSignIn();
        });
    }

    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _splashTimer;

    /// <summary>The time the splash stays on screen.</summary>
    private static readonly TimeSpan SplashDuration = TimeSpan.FromSeconds(2);

    private static T? FindVisualDescendant<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) return match;
            if (FindVisualDescendant<T>(child) is { } nested) return nested;
        }

        return null;
    }

    /// <summary>Shows the splash, then runs <paramref name="next"/> after two seconds.</summary>
    public void ShowSplash(Action next)
    {
        HostFrame.Navigate(typeof(SplashPage), null, new SuppressNavigationTransitionInfo());
        if (HostFrame.Content is SplashPage splash)
        {
            // The whole splash is draggable; there is nothing to click.
            SetTitleBar(splash);
        }

        // Kept in a field: a timer nobody references can be collected before it fires, which left the app on the splash.
        var timer = _splashTimer = DispatcherQueue.CreateTimer();
        timer.Interval = SplashDuration;
        timer.IsRepeating = false;
        timer.Tick += (_, _) => next();
        timer.Start();
    }

    /// <summary>Shows the app shell. <paramref name="crossFade"/> is false when the page is only rebuilt in place (language change).</summary>
    public void ShowShell(bool crossFade = true) => Transition(() =>
    {
        HostFrame.Navigate(typeof(ShellPage), null, new SuppressNavigationTransitionInfo());
        HostFrame.BackStack.Clear();
        if (HostFrame.Content is ShellPage shell)
        {
            SetTitleBar(shell.TitleBarDragRegion);
        }
    }, crossFade, QuickFadeMilliseconds);

    /// <summary>Shows the sign-in screen full window.</summary>
    public void ShowSignIn() => Transition(() =>
    {
        HostFrame.Navigate(typeof(SignInPage), null, new SuppressNavigationTransitionInfo());
        HostFrame.BackStack.Clear();
        if (HostFrame.Content is SignInPage signIn)
        {
            SetTitleBar(signIn.TitleBarDragRegion);
        }
    }, true, HostFrame.Content is SplashPage ? SplashFadeMilliseconds : QuickFadeMilliseconds);

    /// <summary>Shows the first-run wizard full window. Also used to rebuild it after a language change (no cross-fade).</summary>
    public void ShowSetup(bool crossFade = true) => Transition(() =>
    {
        HostFrame.Navigate(typeof(SetupWizardPage), null, new SuppressNavigationTransitionInfo());
        HostFrame.BackStack.Clear();
        if (HostFrame.Content is SetupWizardPage wizard)
        {
            SetTitleBar(wizard.TitleBarDragRegion);
        }
    }, crossFade);

    /// <summary>Fade between sign-in and the app, which people repeat often, so it stays quick.</summary>
    private const int QuickFadeMilliseconds = 150;

    /// <summary>Fade from the splash into sign-in, a one-off that can be gentler.</summary>
    private const int SplashFadeMilliseconds = 300;

    private bool _isTransitioning;

    /// <summary>
    /// Swaps the page with a soft cross-fade: a picture of the old screen is laid over the window, the page changes
    /// underneath, and the picture fades away over <paramref name="fadeMilliseconds"/>. With animations off, while another fade runs, or on any
    /// failure the page just changes.
    /// </summary>
    private async void Transition(Action swap, bool crossFade, int fadeMilliseconds = SplashFadeMilliseconds)
    {
        if (!crossFade || _isTransitioning || Content is not Grid host || HostFrame.Content is null
            || host.ActualWidth <= 0 || !new UISettings().AnimationsEnabled)
        {
            swap();
            return;
        }

        _isTransitioning = true;
        Image? snapshot = null;
        var swapped = false;
        try
        {
            snapshot = await ScreenSnapshot.CaptureAsync(HostFrame);
            host.Children.Add(snapshot);
            swapped = true;
            swap();
            await ScreenSnapshot.NextFrameAsync();

            var fade = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(fadeMilliseconds),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            };
            Storyboard.SetTarget(fade, snapshot);
            Storyboard.SetTargetProperty(fade, "Opacity");
            var storyboard = new Storyboard();
            storyboard.Children.Add(fade);
            var shown = snapshot;
            storyboard.Completed += (_, _) =>
            {
                host.Children.Remove(shown);
                _isTransitioning = false;
            };
            storyboard.Begin();
        }
        catch
        {
            if (snapshot is not null)
            {
                host.Children.Remove(snapshot);
            }

            if (!swapped)
            {
                swap();
            }

            _isTransitioning = false;
        }
    }

    public void ShowStartupError(string message) => HostFrame.Content = new TextBlock
    {
        Text = message,
        Margin = new Thickness(48),
        TextWrapping = TextWrapping.Wrap,
        FontSize = 16,
    };

    private void UpdateCaptionButtons()
    {
        // Caption glyphs follow the app theme, not the Windows theme.
        var foreground = AppServices.Get<IThemeService>().IsDark ? Colors.White : Colors.Black;
        AppWindow.TitleBar.ButtonForegroundColor = foreground;
        AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
    }
}

internal static class AppServices
{
    public static T Get<T>()
        where T : notnull => App.Current.Services.GetRequiredService<T>();
}
