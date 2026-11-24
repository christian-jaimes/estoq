using Estoq.Core.Setup;

namespace Estoq.Core.Sales;

/// <summary>The money of a sale, all in the base currency.</summary>
/// <param name="Subtotal">Sum of line totals, as entered (with tax inside when prices include tax).</param>
/// <param name="Discount">Percentage discount taken off the subtotal.</param>
/// <param name="Tax">Tax in the total: added when prices exclude tax, contained when they include it.</param>
/// <param name="Total">What the customer owes.</param>
public readonly record struct SaleTotals(decimal Subtotal, decimal Discount, decimal Tax, decimal Total)
{
    /// <summary>
    /// Discount first, then tax on the discounted amount. Tax not included: the total is net plus tax.
    /// Tax included: tax is the part of the discounted amount that is tax, and the total is unchanged.
    /// </summary>
    public static SaleTotals Calculate(decimal subtotal, decimal discountPercent, TaxRate taxRate, TaxMode taxMode)
    {
        subtotal = Money.Round(subtotal);
        var discount = Money.Round(subtotal * discountPercent / 100m);
        var net = subtotal - discount;

        if (taxMode == TaxMode.Included)
        {
            var contained = Money.Round(net - (net / (1m + taxRate.Fraction)));
            return new SaleTotals(subtotal, discount, contained, net);
        }

        var added = Money.Round(net * taxRate.Fraction);
        return new SaleTotals(subtotal, discount, added, net + added);
    }
}
