using Estoq.Core.Sales;

namespace Estoq.Core.Dashboard;

/// <summary>
/// One line of a sale or of a return, the unit every dashboard figure is built from (the same transaction data as the Sales page,
/// so the figures never disagree). Prices are the listed prices; whether they contain tax is the store's tax mode.
/// </summary>
/// <param name="Transaction">Receipt number, or the movement id for a line logged without a receipt. Lines sharing it are one transaction.</param>
/// <param name="Seller">Who rang it up; empty when the source does not say. A cashier's "own" figures match on it.</param>
/// <param name="Units">Units sold or returned; always positive.</param>
/// <param name="IsReturn">True for units given back by a customer.</param>
public sealed record DashboardLine(
    string Transaction,
    DateTime At,
    string ArticleId,
    string Name,
    string Sku,
    string Category,
    string Seller,
    PaymentMethod Payment,
    int Units,
    decimal UnitPrice,
    decimal UnitCost,
    bool IsReturn);

/// <summary>Stock on hand of one article, summed across locations.</summary>
public sealed record StockOnHand(string ArticleId, string Name, string Sku, int OnHand);
