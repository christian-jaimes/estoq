using Estoq.Core.Returns;
using Estoq.Core.Sales;

namespace Estoq.App.Services;

/// <summary>
/// Everything a saved return hands over (docs/features/returns-and-voids.md). Money is in the base currency except the
/// settlement line, which is in the currency the cashier handed over or took.
/// </summary>
/// <param name="SaleNumber">The receipt number, or null for a return without a receipt.</param>
/// <param name="Reason">Optional.</param>
/// <param name="Returned">The units taking their way back to stock.</param>
/// <param name="NewItems">Exchange only: the items the customer takes instead.</param>
/// <param name="RefundBase">What the returned items are worth to the customer.</param>
/// <param name="BalanceBase">Exchange only: positive when the customer pays, negative when the store hands money back.</param>
/// <param name="SettleCurrency">Currency of the payment line; the line's amount is <paramref name="SettleAmount"/> at <paramref name="SettleRate"/> units per base unit.</param>
public sealed record ReturnRequest(
    string? SaleNumber,
    ReturnType Type,
    ReturnReason? Reason,
    string Note,
    IReadOnlyList<ReturnedLine> Returned,
    IReadOnlyList<CartLine> NewItems,
    decimal RefundBase,
    decimal BalanceBase,
    string SettleCurrency,
    decimal SettleAmount,
    decimal SettleRate)
{
    public bool IsNoReceipt => SaleNumber is null;
}

/// <summary>What the pop-up shows after a return is saved.</summary>
public sealed record ReturnReceipt(string ReturnNumber);

/// <summary>
/// Finds sales and saves returns. The sample implementation appends the stock movements to the sample CSV and updates the
/// in-memory sale; the back end saves the return, its items, payment line and stock movements in one transaction.
/// </summary>
public interface IReturnsService
{
    /// <summary>The owner's return window in days; null means no limit (a Settings option when Settings is built).</summary>
    int? ReturnWindowDays { get; }

    /// <summary>The sale with this receipt number, or null when there is none.</summary>
    SettledSale? FindSale(string receiptNumber);

    ReturnReceipt Save(ReturnRequest request);
}
