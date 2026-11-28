using Estoq.App.ViewModels.Settings;
using Estoq.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.Views.Settings;

/// <summary>The body of the Settings pop-up: section list, the selected section and the history. Behaviour is in <see cref="SettingsViewModel"/>.</summary>
public sealed partial class SettingsView : UserControl
{
    public SettingsView()
    {
        ViewModel = App.Current.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }

    private ScrollViewer? _popupBodyScrollViewer;

    public SettingsViewModel ViewModel { get; }

    // The pop-up body fills the shell exactly: each section scrolls inside its card, the frame never moves.
    private void OnSettingsLoaded(object sender, RoutedEventArgs e)
    {
        if (_popupBodyScrollViewer is null)
        {
            for (DependencyObject? current = this; current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is ScrollViewer scrollViewer)
                {
                    _popupBodyScrollViewer = scrollViewer;
                    _popupBodyScrollViewer.SizeChanged += (_, _) => UpdateHeight();
                    break;
                }
            }
        }

        UpdateHeight();
    }

    private void UpdateHeight()
    {
        var viewportHeight = _popupBodyScrollViewer?.ViewportHeight ?? 0;
        if (viewportHeight > 0 && !double.IsInfinity(viewportHeight))
        {
            SettingsRoot.Height = viewportHeight;
        }
    }

    private void OnSectionClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: SettingsSection section })
        {
            ViewModel.SelectCommand.Execute(section);
        }
    }
}
