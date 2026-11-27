using CommunityToolkit.Mvvm.ComponentModel;
using Estoq.Core.Stock;

namespace Estoq.App.ViewModels.StockAdjustments;

/// <summary>One of the four adjustment types, shown as a toggle chip. Exactly one is selected at a time.</summary>
public sealed partial class AdjustmentTypeChoiceViewModel : ObservableObject
{
    public AdjustmentTypeChoiceViewModel(AdjustmentType type, string name)
    {
        Type = type;
        Name = name;
    }

    public AdjustmentType Type { get; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
