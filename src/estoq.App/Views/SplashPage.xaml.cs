using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Views;

/// <summary>Launch splash. MainWindow keeps it on screen for two seconds, then opens the wizard or the shell.</summary>
public sealed partial class SplashPage : Page
{
    public SplashPage() => InitializeComponent();
}
