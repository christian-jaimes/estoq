namespace Estoq.Core.Inventory;

/// <summary>How much of a variant is on hand, as the inventory page words it. Always shown as text, never colour alone.</summary>
public enum StockStatus
{
    InStock,
    Low,
    Out,
}

/// <summary>Stock status rules (docs/features/inventory.md).</summary>
public static class StockLevels
{
    /// <summary>Used until the store settings define a default (docs/features/inventory.md: "default from settings").</summary>
    public const int DefaultLowStockThreshold = 5;

    /// <summary>
    /// Nothing or less on hand is out of stock; at or below the threshold is low stock; anything above is in stock.
    /// A negative threshold counts as zero.
    /// </summary>
    public static StockStatus Classify(int onHand, int lowStockThreshold)
    {
        if (onHand <= 0)
        {
            return StockStatus.Out;
        }

        return onHand <= Math.Max(0, lowStockThreshold) ? StockStatus.Low : StockStatus.InStock;
    }

    /// <summary>
    /// The status of a product from its variants' stock: out of stock only when every variant is out, low stock
    /// when any variant is low or out, otherwise in stock. A product without variants is out of stock.
    /// </summary>
    public static StockStatus ClassifyProduct(IEnumerable<int> variantOnHand, int lowStockThreshold)
    {
        ArgumentNullException.ThrowIfNull(variantOnHand);

        var statuses = variantOnHand.Select(onHand => Classify(onHand, lowStockThreshold)).ToList();
        if (statuses.Count == 0 || statuses.All(status => status == StockStatus.Out))
        {
            return StockStatus.Out;
        }

        return statuses.Any(status => status != StockStatus.InStock) ? StockStatus.Low : StockStatus.InStock;
    }
}
