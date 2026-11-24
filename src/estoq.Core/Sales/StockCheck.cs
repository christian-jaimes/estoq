namespace Estoq.Core.Sales;

/// <summary>What the till knows about stock for a line. Selling past stock is allowed with a warning (docs/features/point-of-sale.md).</summary>
public enum StockLevel
{
    /// <summary>On hand covers the quantity.</summary>
    Enough,

    /// <summary>Some stock, but less than the quantity: the rest is sold without stock.</summary>
    Partial,

    /// <summary>Nothing on hand.</summary>
    OutOfStock,
}

public static class StockCheck
{
    public static StockLevel Evaluate(int onHand, int quantity)
    {
        if (onHand >= quantity)
        {
            return StockLevel.Enough;
        }

        return onHand <= 0 ? StockLevel.OutOfStock : StockLevel.Partial;
    }

    /// <summary>
    /// How many units of a line are sold without stock. Those raise a notification and flag the variant
    /// "needs stock correction".
    /// </summary>
    public static int UnitsWithoutStock(int onHand, int quantity) => Math.Max(0, quantity - Math.Max(0, onHand));
}
