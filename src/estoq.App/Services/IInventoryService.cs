namespace Estoq.App.Services;

/// <summary>One variant of a product as the inventory page shows it: a row of the article table with its stock on hand.</summary>
public sealed record InventoryVariant(
    string ArticleId,
    string Sku,
    string Barcode,
    string? Size,
    string? Colour,
    int OnHand,
    decimal SellingPrice,
    decimal CostPrice);

/// <summary>A product (brand, product type and model label) with the variants that have stock history.</summary>
public sealed record InventoryProduct(
    string Id,
    string Name,
    string Code,
    string Category,
    string Brand,
    string Supplier,
    string ProductType,
    string ModelLabel,
    int LowStockThreshold,
    DateTime UpdatedAt,
    IReadOnlyList<InventoryVariant> Variants);

/// <summary>One stock movement. Sales and defects are negative; additions and found stock are positive.</summary>
public sealed record InventoryMovement(string MovementId, string ArticleId, string Type, int Quantity, string Location, DateTime CreatedAt);

/// <summary>
/// Products, variants and stock history for the Inventory page. The sample implementation reads the development CSV
/// fixtures (the article dimension and the stock movement facts); the back end replaces it with the database.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Every product, read fresh so products added from the F2 pop-up appear. Variants that
    /// never had stock are not listed, and a product without a listed variant is left out.
    /// </summary>
    IReadOnlyList<InventoryProduct> GetProducts();

    /// <summary>The stock movements of every variant of a product, newest first.</summary>
    IReadOnlyList<InventoryMovement> GetMovements(string productId);

}
