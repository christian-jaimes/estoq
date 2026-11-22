namespace Estoq.Core.Products;

/// <summary>What the form has to check before Save product is allowed.</summary>
public sealed record ProductDraft(
    string? ProductType,
    string? Category,
    string? Brand,
    string? ModelLabel,
    string? Name,
    decimal? CostPrice,
    decimal? SellingPrice,
    IReadOnlyList<VariantDraft> Variants);

public sealed record VariantDraft(string Sku, int OpeningStock);

public static class ProductRules
{
    /// <summary>The first problem found, in form order, or <see cref="ProductValidationCode.None"/>.</summary>
    public static ProductValidationCode Validate(ProductDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (ProductNaming.Clean(draft.ProductType).Length == 0)
        {
            return ProductValidationCode.ProductTypeRequired;
        }

        if (ProductNaming.Clean(draft.Category).Length == 0)
        {
            return ProductValidationCode.CategoryRequired;
        }

        if (ProductNaming.Clean(draft.Brand).Length == 0)
        {
            return ProductValidationCode.BrandRequired;
        }

        if (ProductNaming.Clean(draft.ModelLabel).Length == 0)
        {
            return ProductValidationCode.ModelLabelRequired;
        }

        if (ProductNaming.Clean(draft.Name).Length == 0)
        {
            return ProductValidationCode.NameRequired;
        }

        if (draft.SellingPrice is null)
        {
            return ProductValidationCode.SellingPriceRequired;
        }

        if (draft.SellingPrice < 0 || draft.CostPrice < 0)
        {
            return ProductValidationCode.PriceInvalid;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var variant in draft.Variants)
        {
            var sku = variant.Sku.Trim().ToUpperInvariant();
            if (sku.Length == 0)
            {
                return ProductValidationCode.SkuRequired;
            }

            if (!seen.Add(sku))
            {
                return ProductValidationCode.SkuDuplicate;
            }

            if (variant.OpeningStock < 0)
            {
                return ProductValidationCode.StockInvalid;
            }
        }

        return ProductValidationCode.None;
    }
}
