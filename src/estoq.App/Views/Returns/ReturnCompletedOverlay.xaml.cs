using Estoq.App.ViewModels.Returns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Estoq.App.Views.Returns;

/// <summary>The "Return completed" confirmation shown over the whole Returns pop-up.</summary>
public sealed partial class ReturnCompletedOverlay : UserControl
{
    public ReturnCompletedOverlay()
    {
        ViewModel = App.Current.Services.GetRequiredService<ReturnsViewModel>();
        InitializeComponent();
    }

    public ReturnsViewModel ViewModel { get; }

    /// <summary>Keeps a click on the confirmation from reaching the scrim behind it, which would close the pop-up.</summary>
    private void OnTapped(object sender, TappedRoutedEventArgs e) => e.Handled = true;
}
