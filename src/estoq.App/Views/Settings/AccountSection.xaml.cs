using Estoq.App.ViewModels.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Views.Settings;

/// <summary>The Account section of the Settings pop-up. All behaviour lives in <see cref="AccountSettingsViewModel"/>.</summary>
public sealed partial class AccountSection : UserControl
{
    public AccountSection()
    {
        ViewModel = App.Current.Services.GetRequiredService<AccountSettingsViewModel>();
        InitializeComponent();
    }

    public AccountSettingsViewModel ViewModel { get; }
}
