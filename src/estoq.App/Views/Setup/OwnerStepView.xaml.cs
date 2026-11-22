using Estoq.App.ViewModels.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Views.Setup;

public sealed partial class OwnerStepView : UserControl
{
    public OwnerStepView()
    {
        ViewModel = App.Current.Services.GetRequiredService<SetupWizardViewModel>().Owner;
        InitializeComponent();
    }

    public OwnerStepViewModel ViewModel { get; }
}
