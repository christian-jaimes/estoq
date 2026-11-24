namespace Estoq.App.Services;

/// <summary>A kind of product and the sizes it offers. No sizes means a one-size item.</summary>
public sealed record ProductTypeInfo(string Name, IReadOnlyList<string> Sizes, string Id = "");

/// <summary>A seeded colour. <see cref="Key"/> names the translated label (<c>Colour_&lt;Key&gt;</c>); <see cref="Hex"/> is the swatch.</summary>
public sealed record ColourInfo(string Key, string Hex, bool IsQuickPick, string Family = "");

/// <summary>A seeded size from the size master data.</summary>
public sealed record SizeInfo(string Name, int SortOrder, string ProductTypeId = "");

/// <summary>A brand displayed by the product editor, with optional user-facing code metadata.</summary>
public sealed record BrandInfo(string Id, string Name, string SkuCode, bool IsActive);

/// <summary>One variant of a product about to be created.</summary>
public sealed record NewVariant(string? Size, string? Colour, string Sku, string InternalBarcode, int OpeningStock, decimal? SellingPrice = null);

/// <summary>Everything the Add product pop-up hands over on Save (docs/features/add-product.md).</summary>
public sealed record NewProduct(
    string ProductType,
    string Category,
    string Brand,
    string ModelLabel,
    string Name,
    string Description,
    string SupplierBarcode,
    decimal? CostPrice,
    decimal SellingPrice,
    IReadOnlyList<string> ImagePaths,
    IReadOnlyList<NewVariant> Variants);

/// <summary>
/// Master data and products for the Add product flow. The sample implementation keeps everything in memory;
/// the back end replaces it, and Save then creates the product and its variants in one transaction.
/// </summary>
public interface ICatalogService
{
    /// <summary>Currency code prices are entered in: the store's base currency.</summary>
    string DefaultCurrencyCode { get; }

    IReadOnlyList<ProductTypeInfo> ProductTypes { get; }

    IReadOnlyList<string> Categories { get; }

    IReadOnlyList<string> Brands { get; }

    IReadOnlyList<BrandInfo> BrandDetails { get; }

    IReadOnlyList<ColourInfo> Colours { get; }

    IReadOnlyList<SizeInfo> Sizes { get; }

    /// <summary>The products that exist, for the duplicate check.</summary>
    IReadOnlyList<Estoq.Core.Products.ExistingProduct> ExistingProducts { get; }

    /// <summary>Every SKU in use, so generated and typed SKUs stay unique.</summary>
    IReadOnlySet<string> TakenSkus { get; }

    /// <summary>The next unused internal barcode sequence number.</summary>
    long NextBarcodeSequence { get; }

    /// <summary>Creates the product and its variants, and any category, brand or product type that is new.</summary>
    Task SaveAsync(NewProduct product);
}
