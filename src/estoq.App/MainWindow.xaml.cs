using Estoq.App.Services;
using Estoq.App.Views;
using Estoq.App.Views.Setup;
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

        AppServices.Get<IThemeService>().Attach(Root);
        AppServices.Get<IThemeService>().Changed += (_, _) => UpdateCaptionButtons();
        UpdateCaptionButtons();
    }

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

        var timer = DispatcherQueue.CreateTimer();
        timer.Interval = SplashDuration;
        timer.IsRepeating = false;
        timer.Tick += (_, _) => next();
        timer.Start();
    }

    public void ShowShell()
    {
        HostFrame.Navigate(typeof(ShellPage));
        HostFrame.BackStack.Clear();
        if (HostFrame.Content is ShellPage shell)
        {
            SetTitleBar(shell.TitleBarDragRegion);
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
