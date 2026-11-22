using Estoq.App.ViewModels.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Views.Setup;

public sealed partial class StoreStepView : UserControl
{
    public StoreStepView()
    {
        ViewModel = App.Current.Services.GetRequiredService<SetupWizardViewModel>().Store;
        InitializeComponent();
    }

    public StoreStepViewModel ViewModel { get; }
}
