namespace Estoq.Core.Settings;

/// <summary>The three stock states shown across the app.</summary>
public enum StockLevel
{
    InStock,
    LowStock,
    NoStock,
}

/// <summary>
/// The low-stock threshold, set by the owner in Store settings (setting key <see cref="SettingKey"/>, default
/// <see cref="DefaultThreshold"/>). An item is out of stock at 0 or less, low when on hand is at or below the threshold,
/// and in stock above it.
/// </summary>
public static class LowStockPolicy
{
    /// <summary>Name of the setting; also the audit field name.</summary>
    public const string SettingKey = "LowStockThreshold";

    public const int DefaultThreshold = 5;

    public const int MinThreshold = 1;

    public const int MaxThreshold = 10_000;

    public static bool IsValid(int threshold) => threshold is >= MinThreshold and <= MaxThreshold;

    public static StockLevel Classify(int onHand, int threshold) =>
        onHand <= 0 ? StockLevel.NoStock : onHand <= threshold ? StockLevel.LowStock : StockLevel.InStock;
}
