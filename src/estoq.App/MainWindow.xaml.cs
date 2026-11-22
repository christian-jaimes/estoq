using Estoq.App.Services;
using Estoq.App.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    public void ShowShell()
    {
        HostFrame.Navigate(typeof(ShellPage));
        if (HostFrame.Content is ShellPage shell)
        {
            SetTitleBar(shell.TitleBarDragRegion);
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
