namespace Estoq.Core.Dashboard;

/// <summary>Low-stock rule: an article at or below its threshold, and above zero, is low; at zero it is out of stock (docs/features/inventory.md).</summary>
public static class LowStock
{
    /// <summary>Until Settings stores the owner's default low-stock threshold.</summary>
    public const int DefaultThreshold = 5;

    public static bool IsLow(StockOnHand level, int threshold) => level.OnHand > 0 && level.OnHand <= threshold;

    /// <summary>Every low article, the scarcest first (then by name).</summary>
    public static IReadOnlyList<StockOnHand> Items(IEnumerable<StockOnHand> levels, int threshold = DefaultThreshold) =>
        [.. levels.Where(level => IsLow(level, threshold))
            .OrderBy(level => level.OnHand).ThenBy(level => level.Name, StringComparer.OrdinalIgnoreCase)];
}
