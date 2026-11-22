using Estoq.App.ViewModels.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Views.Setup;

/// <summary>Full-window first-run wizard (docs/features/setup-wizard.md).</summary>
public sealed partial class SetupWizardPage : Page
{
    public SetupWizardPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<SetupWizardViewModel>();
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public SetupWizardViewModel ViewModel { get; }

    /// <summary>The window uses this area as its title bar so the app can be dragged from it.</summary>
    public UIElement TitleBarDragRegion => DragRegion;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LanguageApplied += OnLanguageApplied;
        ViewModel.Completed += OnCompleted;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LanguageApplied -= OnLanguageApplied;
        ViewModel.Completed -= OnCompleted;
    }

    /// <summary>Static labels are read when a page is built, so a language change builds the page again. Entered values live in the view models.</summary>
    private void OnLanguageApplied(object? sender, EventArgs e) =>
        DispatcherQueue.TryEnqueue(() => App.Current.MainWindow?.ShowSetup());

    private void OnCompleted(object? sender, EventArgs e) =>
        DispatcherQueue.TryEnqueue(() => App.Current.MainWindow?.ShowShell());
}
