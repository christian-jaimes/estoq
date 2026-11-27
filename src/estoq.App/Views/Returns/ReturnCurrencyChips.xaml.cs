using Estoq.App.ViewModels.Returns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Views.Returns;

/// <summary>Currency chips for the Returns pop-up header (same look as the point of sale). Behaviour lives in <see cref="ReturnsViewModel"/>.</summary>
public sealed partial class ReturnCurrencyChips : UserControl
{
    public ReturnCurrencyChips()
    {
        ViewModel = App.Current.Services.GetRequiredService<ReturnsViewModel>();
        InitializeComponent();
    }

    public ReturnsViewModel ViewModel { get; }
}
