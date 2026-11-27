using Estoq.Core.Returns;
using Estoq.Core.Sales;

namespace Estoq.App.Services;

/// <summary>A settled sale as the return flow needs it. Money is in the base currency.</summary>
/// <param name="Subtotal">Sum of the line totals before discount and tax.</param>
/// <param name="Total">What the customer paid.</param>
/// <param name="Refunded">What earlier returns already gave back.</param>
public sealed record SettledSale(string Number, DateTime Date, IReadOnlyList<SoldLine> Lines, decimal Subtotal, decimal Total, decimal Refunded)
{
    public SaleBasis Basis => new(Subtotal, Total, Refunded);
}

/// <summary>
/// The sales that can be returned. The sample implementation keeps a few seeded sales and the sales completed at the
/// till this session; the back end replaces it with the stored sales.
/// </summary>
public interface ISaleHistory
{
    /// <summary>The sale with this receipt number, written with or without leading zeros; null when there is none.</summary>
    SettledSale? Find(string receiptNumber);

    void Record(SettledSale sale);

    /// <summary>
    /// Adds the new items of an exchange to the sale they were exchanged on, so they can be returned or exchanged later under the same
    /// receipt number. The sale's subtotal and total grow by what the new items cost.
    /// </summary>
    void AddItems(string receiptNumber, IReadOnlyList<CartLine> items);

    /// <summary>Counts the returned units and the refund against the sale so a second return cannot exceed it.</summary>
    void ApplyReturn(string receiptNumber, IReadOnlyList<ReturnedLine> returned, decimal refund);
}
