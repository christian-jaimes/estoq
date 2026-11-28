using CommunityToolkit.Mvvm.ComponentModel;
using Estoq.Core.Sales;

namespace Estoq.App.ViewModels.Sales;

/// <summary>A period tab (All sales, Today, ...). Exactly one is selected.</summary>
public sealed partial class PeriodTabViewModel : ObservableObject
{
    private readonly Action<SalesPeriod> _select;

    public PeriodTabViewModel(SalesPeriod period, string label, bool isSelected, Action<SalesPeriod> select)
    {
        _select = select;
        Period = period;
        Label = label;
        IsSelected = isSelected;
    }

    public SalesPeriod Period { get; }

    public string Label { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
        {
            _select(Period);
        }
    }
}
