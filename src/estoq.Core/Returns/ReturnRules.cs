using System.Globalization;
using Estoq.Core.Sales;

namespace Estoq.Core.Returns;

/// <summary>What a return is worth, all in the base currency.</summary>
/// <param name="Value">The returned lines at their sale prices (the "Subtotal" of the refund summary).</param>
/// <param name="Refund">What the customer gets back: their share of what they actually paid, never more than is left to refund.</param>
/// <param name="IsCapped">True when the refund was cut down to what is left to refund on the sale.</param>
public readonly record struct ReturnAmounts(decimal Value, decimal Refund, bool IsCapped);

/// <summary>What a settled sale holds for the refund limit: its line subtotal, what was paid and what was already refunded.</summary>
public readonly record struct SaleBasis(decimal Subtotal, decimal Paid, decimal AlreadyRefunded);

/// <summary>
/// Return rules (docs/features/returns-and-voids.md): quantity limits, the return window, the refund and its cap,
/// and the balance of an exchange. Pure code, no database.
/// </summary>
public static class ReturnRules
{
    /// <summary>Units of a line that can still be taken back.</summary>
    public static int ReturnableQuantity(int sold, int alreadyReturned) => Math.Max(0, sold - alreadyReturned);

    /// <summary>Keeps a typed quantity between 1 and what can still be returned; 0 when nothing can.</summary>
    public static int ClampQuantity(int quantity, int returnable) =>
        returnable <= 0 ? 0 : Math.Clamp(quantity, 1, returnable);

    /// <summary>
    /// True when the sale is inside the owner's return window. No window (null) means no limit.
    /// The window counts whole days from the sale date, so a sale made on day 0 is still returnable on day <paramref name="windowDays"/>.
    /// </summary>
    public static bool IsWithinWindow(DateTime saleDate, DateTime now, int? windowDays) =>
        windowDays is null || (now.Date - saleDate.Date).TotalDays <= windowDays.Value;

    /// <summary>
    /// The value and refund of the returned lines. With a receipt (<paramref name="basis"/>) the refund is the same share of
    /// what was paid as the lines are of the sale subtotal, so a discount on the sale is not refunded twice, and it
    /// cannot exceed what is left to refund. Without a receipt the refund is the lines at their current price.
    /// </summary>
    public static ReturnAmounts Calculate(IEnumerable<ReturnedLine> lines, SaleBasis? basis)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var value = Money.Round(lines.Sum(line => Money.Round(line.UnitPrice * line.Quantity)));
        if (basis is not { } sale)
        {
            return new ReturnAmounts(value, value, false);
        }

        var share = sale.Subtotal > 0m ? sale.Paid / sale.Subtotal : 1m;
        var refund = Money.Round(value * share);
        var left = Math.Max(0m, sale.Paid - sale.AlreadyRefunded);
        return refund > left ? new ReturnAmounts(value, left, true) : new ReturnAmounts(value, refund, false);
    }

    /// <summary>
    /// The exchange balance: positive when the customer pays the difference, negative when the store hands money back,
    /// zero when the new items match the credit.
    /// </summary>
    public static decimal ExchangeBalance(decimal credit, decimal newItemsTotal) => Money.Round(newItemsTotal - credit);

    /// <summary>The next return number, for example <c>R-000007</c>.</summary>
    public static string FormatReturnNumber(int sequence) => "R-" + sequence.ToString("D6", CultureInfo.InvariantCulture);
}
