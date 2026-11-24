namespace Estoq.Core.Sales;

/// <summary>One payment taken: any method, in any accepted currency. The amount is in that currency.</summary>
public sealed record PaymentLine(PaymentMethod Method, string Currency, decimal Amount);

/// <summary>
/// Where the payments stand against the total, all in the base currency. A sale completes only when payments,
/// converted at the sale's rates, cover the total (docs/features/point-of-sale.md, split payments).
/// </summary>
/// <param name="Total">The amount owed.</param>
/// <param name="Paid">Payments converted to base currency.</param>
/// <param name="Remaining">What is still owed; zero when covered.</param>
/// <param name="Change">The overpayment to hand back; zero unless overpaid.</param>
/// <param name="CashPaid">The part of <paramref name="Paid"/> that was cash.</param>
public readonly record struct PaymentSettlement(decimal Total, decimal Paid, decimal Remaining, decimal Change, decimal CashPaid)
{
    public bool IsCovered => Remaining == 0m;

    /// <summary>Only cash is handed back, so change cannot exceed the cash taken.</summary>
    public bool CanGiveChange => Change <= CashPaid;

    /// <summary>The sale can be completed once payments cover the total; cash change is handled by the cashier.</summary>
    public bool CanComplete => IsCovered;

    public static PaymentSettlement Calculate(decimal total, IEnumerable<PaymentLine> payments, ExchangeRates rates)
    {
        ArgumentNullException.ThrowIfNull(payments);
        ArgumentNullException.ThrowIfNull(rates);

        var paid = 0m;
        var cash = 0m;
        foreach (var payment in payments)
        {
            if (payment.Amount <= 0m)
            {
                continue;
            }

            var inBase = rates.ToBase(payment.Amount, payment.Currency);
            paid += inBase;
            if (PaymentMethods.CanGiveChange(payment.Method))
            {
                cash += inBase;
            }
        }

        return new PaymentSettlement(total, paid, Math.Max(0m, total - paid), Math.Max(0m, paid - total), cash);
    }
}
