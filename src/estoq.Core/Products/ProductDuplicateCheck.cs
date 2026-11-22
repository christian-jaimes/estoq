namespace Estoq.Core.Products;

/// <summary>The three values that make a product unique: brand, product type and model label.</summary>
public sealed record ProductIdentity(string Brand, string ProductType, string ModelLabel);

/// <summary>An existing product as the duplicate check sees it.</summary>
public sealed record ExistingProduct(string Id, ProductIdentity Identity, string Name);

/// <summary>
/// Step 3 of the add-product flow: the same brand, product type and model label is the same product, so the
/// owner is offered to open it instead. A different model label is a new product.
/// </summary>
public static class ProductDuplicateCheck
{
    public static ExistingProduct? Find(IEnumerable<ExistingProduct> existing, ProductIdentity candidate)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(candidate);

        var model = ProductNaming.Key(candidate.ModelLabel);
        if (model.Length == 0)
        {
            return null;
        }

        var brand = ProductNaming.Key(candidate.Brand);
        var type = ProductNaming.Key(candidate.ProductType);
        return existing.FirstOrDefault(p =>
            ProductNaming.Key(p.Identity.Brand) == brand
            && ProductNaming.Key(p.Identity.ProductType) == type
            && ProductNaming.Key(p.Identity.ModelLabel) == model);
    }
}
