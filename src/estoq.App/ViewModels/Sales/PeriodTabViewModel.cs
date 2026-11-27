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

/// <summary>A button of the pager: a page number or the ellipsis between distant pages.</summary>
public sealed class PageButtonViewModel
{
    public PageButtonViewModel(int number, bool isCurrent, string automationName, Action<int> go)
    {
        Number = number;
        IsGap = number == SalesPaging.Gap;
        IsCurrent = isCurrent;
        Text = IsGap ? "…" : number.ToString(System.Globalization.CultureInfo.CurrentUICulture);
        AutomationName = automationName;
        IsLink = !IsGap && !isCurrent;
        _go = go;
    }

    private readonly Action<int> _go;

    public int Number { get; }

    public bool IsGap { get; }

    public bool IsCurrent { get; }

    /// <summary>A page that can be opened: not the current page and not the ellipsis.</summary>
    public bool IsLink { get; }

    public string Text { get; }

    public string AutomationName { get; }

    public void Go() => _go(Number);
}
