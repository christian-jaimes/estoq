namespace Estoq.Core.Sales;

/// <summary>Where a saved sale stands. Returned and voided sales stay in the history (docs/features/sales.md).</summary>
public enum SaleStatus
{
    Completed,
    Returned,
    Voided,
}

/// <summary>One line of a saved sale. Prices are in the base currency.</summary>
/// <param name="Variant">Colour and size as shown under the name, for example "Beige / M".</param>
public sealed record SaleLineRecord(string Name, string Sku, string Variant, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => Money.Round(UnitPrice * Quantity);
}

/// <summary>One payment of a saved sale: the amount in the currency handed over and the rate snapshot of that day.</summary>
/// <param name="Rate">Units of <paramref name="Currency"/> per one unit of the base currency; 1 for the base currency.</param>
public sealed record SalePaymentRecord(PaymentMethod Method, string Currency, decimal Amount, decimal Rate)
{
    /// <summary>What this payment is worth in the base currency, at the rate of the sale.</summary>
    public decimal BaseValue => Rate <= 0m ? 0m : Money.Round(Amount / Rate);
}

/// <summary>A saved sale as the Sales page lists it. Customers are not captured in the MVP.</summary>
public sealed record SaleRecord(
    string ReceiptNumber,
    DateTime SoldAt,
    string Staff,
    SaleStatus Status,
    IReadOnlyList<SaleLineRecord> Lines,
    decimal DiscountPercent,
    decimal TaxPercent,
    SaleTotals Totals,
    IReadOnlyList<SalePaymentRecord> Payments)
{
    /// <summary>Units sold, not lines.</summary>
    public int ItemCount => Lines.Sum(line => line.Quantity);

    /// <summary>The method that carried most of the sale; the table shows one badge per sale.</summary>
    public PaymentMethod PrimaryMethod => Payments.Count == 0
        ? PaymentMethod.Other
        : Payments.OrderByDescending(payment => payment.BaseValue).First().Method;
}
