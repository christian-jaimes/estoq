using Estoq.App.ViewModels.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Views.Settings;

/// <summary>The Staff section of the Settings pop-up. All behaviour lives in <see cref="StaffSettingsViewModel"/>.</summary>
public sealed partial class StaffSection : UserControl
{
    public StaffSection()
    {
        ViewModel = App.Current.Services.GetRequiredService<StaffSettingsViewModel>();
        InitializeComponent();
    }

    public StaffSettingsViewModel ViewModel { get; }

    private void OnEditClick(object sender, RoutedEventArgs e) => ViewModel.EditCommand.Execute(RowOf(sender));

    private void OnRegenerateClick(object sender, RoutedEventArgs e) => ViewModel.RegenerateCodeCommand.Execute(RowOf(sender));

    private void OnToggleStatusClick(object sender, RoutedEventArgs e) => ViewModel.ToggleStatusCommand.Execute(RowOf(sender));

    private static StaffRowViewModel? RowOf(object sender) => (sender as FrameworkElement)?.DataContext as StaffRowViewModel;
}
