using Estoq.App.Services;
using Estoq.App.Views;
using Estoq.App.Views.Setup;
using Estoq.App.Views.SignIn;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Estoq.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "estoq";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1440, 900));
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        // Use the initialized window content as the root. This also avoids a null generated
        // x:Name field when Visual Studio launches with stale XAML design-time artifacts.
        var root = Content as FrameworkElement
            ?? throw new InvalidOperationException("MainWindow XAML did not create its root content.");

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

    public void ShowShell(bool animateEntrance = false)
    {
        HostFrame.Navigate(typeof(ShellPage), animateEntrance, new SuppressNavigationTransitionInfo());
        HostFrame.BackStack.Clear();
        if (HostFrame.Content is ShellPage shell)
        {
            SetTitleBar(shell.TitleBarDragRegion);
        }
    }

    /// <summary>Shows the sign-in screen full window.</summary>
    public void ShowSignIn()
    {
        HostFrame.Navigate(typeof(SignInPage), null, new SuppressNavigationTransitionInfo());
        HostFrame.BackStack.Clear();
        if (HostFrame.Content is SignInPage signIn)
        {
            SetTitleBar(signIn.TitleBarDragRegion);
        }
    }

    /// <summary>Shows the first-run wizard full window. Also used to rebuild it after a language change.</summary>
    public void ShowSetup()
    {
        HostFrame.Navigate(typeof(SetupWizardPage), null, new SuppressNavigationTransitionInfo());
        HostFrame.BackStack.Clear();
        if (HostFrame.Content is SetupWizardPage wizard)
        {
            SetTitleBar(wizard.TitleBarDragRegion);
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
