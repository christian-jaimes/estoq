using Estoq.App.ViewModels.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Views.Setup;

public sealed partial class WelcomeStepView : UserControl
{
    public WelcomeStepView()
    {
        ViewModel = App.Current.Services.GetRequiredService<SetupWizardViewModel>().Welcome;
        InitializeComponent();
    }

    public WelcomeStepViewModel ViewModel { get; }
}
