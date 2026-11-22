using Estoq.App.ViewModels.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Views.Setup;

public sealed partial class RecoveryStepView : UserControl
{
    public RecoveryStepView()
    {
        ViewModel = App.Current.Services.GetRequiredService<SetupWizardViewModel>().Recovery;
        InitializeComponent();
    }

    public RecoveryStepViewModel ViewModel { get; }
}
