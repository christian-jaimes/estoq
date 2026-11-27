namespace Estoq.App.ViewModels.StockAdjustments;

/// <summary>One item listed on the "Adjustment applied" confirmation.</summary>
/// <param name="Name">The article name.</param>
/// <param name="DetailText">Colour and size, or the SKU for an item without either.</param>
/// <param name="PreviousText">Stock before the adjustment.</param>
/// <param name="DifferenceText">The signed difference, for example "+3" or "−2".</param>
/// <param name="NewStockText">Stock after the adjustment.</param>
/// <param name="IsIncrease">True when stock went up; otherwise it went down.</param>
/// <param name="IsAlternateRow">True for every second row, which gets the table's banding.</param>
public sealed record AppliedItemViewModel(string Name, string DetailText, string PreviousText, string DifferenceText, string NewStockText, bool IsIncrease, bool IsAlternateRow)
{
    public bool IsDecrease => !IsIncrease;
}
