namespace Estoq.App.Services;

/// <summary>
/// Store-wide stock settings. The owner sets the low stock threshold in the store settings (docs/features/settings.md); one
/// value applies to every product. A variant with nothing on hand has no stock, one at or below the threshold has low stock,
/// anything above is in stock (<see cref="Estoq.Core.Settings.LowStockPolicy"/>).
/// </summary>
public interface IStockSettings
{
    /// <summary>Units at or below which a variant counts as low stock.</summary>
    int LowStockThreshold { get; }
}
