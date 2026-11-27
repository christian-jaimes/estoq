using Estoq.Core.Products;
using Xunit;

namespace Estoq.Core.Tests;

public class ProductRulesTests
{
    private static ProductDraft Valid() => new(
        "T-shirt", "Tops", "Hugo Boss", "Red graphic", "Hugo Boss T-shirt, Red graphic", 8m, 12m,
        [new VariantDraft("TS-RD-S", 0), new VariantDraft("TS-RD-M", 4)]);

    [Fact]
    public void CompleteDraftIsAccepted() => Assert.Equal(ProductValidationCode.None, ProductRules.Validate(Valid()));

    [Fact]
    public void MissingMasterDataIsReportedInFormOrder()
    {
        Assert.Equal(ProductValidationCode.ProductTypeRequired, ProductRules.Validate(Valid() with { ProductType = " " }));
        Assert.Equal(ProductValidationCode.CategoryRequired, ProductRules.Validate(Valid() with { Category = null }));
        Assert.Equal(ProductValidationCode.BrandRequired, ProductRules.Validate(Valid() with { Brand = "" }));
        Assert.Equal(ProductValidationCode.ModelLabelRequired, ProductRules.Validate(Valid() with { ModelLabel = "" }));
        Assert.Equal(ProductValidationCode.NameRequired, ProductRules.Validate(Valid() with { Name = "  " }));
    }

    [Fact]
    public void SellingPriceIsRequiredButCostIsOptional()
    {
        Assert.Equal(ProductValidationCode.SellingPriceRequired, ProductRules.Validate(Valid() with { SellingPrice = null }));
        Assert.Equal(ProductValidationCode.None, ProductRules.Validate(Valid() with { CostPrice = null }));
    }

    [Fact]
    public void NegativePricesAreRejected()
    {
        Assert.Equal(ProductValidationCode.PriceInvalid, ProductRules.Validate(Valid() with { SellingPrice = -1m }));
        Assert.Equal(ProductValidationCode.PriceInvalid, ProductRules.Validate(Valid() with { CostPrice = -1m }));
    }

    [Fact]
    public void SkuMustBePresentAndUniqueIgnoringCase()
    {
        Assert.Equal(ProductValidationCode.SkuRequired,
            ProductRules.Validate(Valid() with { Variants = [new VariantDraft(" ", 0)] }));
        Assert.Equal(ProductValidationCode.SkuDuplicate,
            ProductRules.Validate(Valid() with { Variants = [new VariantDraft("A-1", 0), new VariantDraft("a-1", 0)] }));
    }

    [Fact]
    public void OpeningStockCannotBeNegative() =>
        Assert.Equal(ProductValidationCode.StockInvalid,
            ProductRules.Validate(Valid() with { Variants = [new VariantDraft("A-1", -1)] }));
}
