namespace Estoq.Core.Products;

/// <summary>Why the add-product form cannot be saved yet. Codes, not messages: the UI maps them to <c>Error_&lt;Code&gt;</c> text.</summary>
public enum ProductValidationCode
{
    None,
    ProductTypeRequired,
    CategoryRequired,
    BrandRequired,
    ModelLabelRequired,
    NameRequired,
    SellingPriceRequired,
    PriceInvalid,
    SkuRequired,
    SkuDuplicate,
    StockInvalid,
}
