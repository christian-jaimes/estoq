using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Estoq.App.Services;
using Estoq.App.ViewModels.PointOfSale;
using Estoq.Core.Sales;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.ViewModels.Sales;

/// <summary>One row of the sales table, with every value already formatted for the current language.</summary>
public sealed partial class SaleRowViewModel : ObservableObject
{
    public SaleRowViewModel(SaleRecord sale, string currency, ILocalizer localizer, bool isAlternateRow)
    {
        Sale = sale;
        IsAlternateRow = isAlternateRow;
        var culture = localizer.Culture;
        ReceiptNumber = sale.ReceiptNumber;
        DateText = FormatDate(sale.SoldAt, culture);
        ItemsText = sale.ItemCount.ToString(culture);
        TotalText = CurrencyFormat.Amount(sale.Totals.Total, currency, culture);
        StaffText = sale.Staff;
        Status = sale.Status;
        StatusKind = KindOf(sale.Status);
        StatusText = localizer["Sales_Status_" + sale.Status];
        MethodText = localizer["Pos_Method_" + sale.PrimaryMethod];
        (MethodGlyph, MethodBrandLetter, MethodBrandBrush) = PaymentMethodViewModel.Describe(sale.PrimaryMethod);
        HasMethodGlyph = MethodGlyph.Length > 0;
        HasMethodBrand = MethodBrandLetter.Length > 0;
    }

    public SaleRecord Sale { get; }

    /// <summary>True while this sale's detail is open: the row takes the gold tint.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>Every second row is banded, as in every table (EqTableBandStyle).</summary>
    public bool IsAlternateRow { get; }

    public string ReceiptNumber { get; }

    public string DateText { get; }

    public string ItemsText { get; }

    public string TotalText { get; }

    public string StaffText { get; }

    public SaleStatus Status { get; }

    /// <summary>The StatusBadge kind: "success", "warning" or "danger".</summary>
    public string StatusKind { get; }

    public string StatusText { get; }

    public static string KindOf(SaleStatus status) => status switch
    {
        SaleStatus.Completed => "success",
        SaleStatus.Returned => "warning",
        _ => "danger",
    };

    public string MethodText { get; }

    public string MethodGlyph { get; }

    public bool HasMethodGlyph { get; }

    public string MethodBrandLetter { get; }

    public bool HasMethodBrand { get; }

    public Brush? MethodBrandBrush { get; }

    /// <summary>Short date and time in the app language, for example "11/27/2026, 4:32 PM".</summary>
    public static string FormatDate(DateTime value, CultureInfo culture) =>
        $"{value.ToString("d", culture)}, {value.ToString("t", culture)}";
}

/// <summary>One item line in the detail panel.</summary>
public sealed record SaleDetailLineViewModel(string Name, string Sku, string PriceText, string QuantityText, string TotalText, bool IsAlternateRow);

/// <summary>One payment in the detail panel: the method, the amount in the currency paid, and the rate snapshot when it is not the base currency.</summary>
public sealed record SalePaymentViewModel(string MethodText, string AmountText, string RateText, bool HasRate);

/// <summary>An entry of a filter drop-down. An empty <see cref="Key"/> means "all".</summary>
public sealed record SalesFilterOption(string Key, string Label);
