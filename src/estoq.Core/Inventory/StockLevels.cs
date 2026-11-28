using Estoq.Core.Settings;

namespace Estoq.Core.Inventory;

/// <summary>
/// Product-level stock status for the inventory page (docs/features/inventory.md). The per-variant rule and the low stock
/// threshold live in <see cref="LowStockPolicy"/>; the threshold is one store-wide value the owner sets in the store settings.
/// </summary>
public static class StockLevels
{
    /// <summary>Variant status: no stock at 0 or less, low stock at or below the threshold, otherwise in stock.</summary>
    public static StockLevel Classify(int onHand, int lowStockThreshold) => LowStockPolicy.Classify(onHand, lowStockThreshold);

    /// <summary>
    /// The status of a product from its variants' stock: no stock only when every variant has none, low stock
    /// when any variant is low or has none, otherwise in stock. A product without variants has no stock.
    /// </summary>
    public static StockLevel ClassifyProduct(IEnumerable<int> variantOnHand, int lowStockThreshold)
    {
        ArgumentNullException.ThrowIfNull(variantOnHand);

        var statuses = variantOnHand.Select(onHand => Classify(onHand, lowStockThreshold)).ToList();
        if (statuses.Count == 0 || statuses.All(status => status == StockLevel.NoStock))
        {
            return StockLevel.NoStock;
        }

        return statuses.Any(status => status != StockLevel.InStock) ? StockLevel.LowStock : StockLevel.InStock;
    }
}
