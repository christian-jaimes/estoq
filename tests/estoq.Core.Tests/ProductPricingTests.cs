using Estoq.Core.Products;
using Xunit;

namespace Estoq.Core.Tests;

public class ProductPricingTests
{
    [Fact]
    public void MarginIsShownAsAmountAndPercentOfSellingPrice()
    {
        var margin = ProductPricing.Estimate(8m, 12m);
        Assert.Equal(4m, margin.Amount);
        Assert.Equal(33.33m, margin.Percent);
    }

    [Fact]
    public void SellingBelowCostIsNegative()
    {
        var margin = ProductPricing.Estimate(10m, 8m);
        Assert.Equal(-2m, margin.Amount);
        Assert.Equal(-25m, margin.Percent);
    }

    [Fact]
    public void NoSellingPriceHasNoPercent() => Assert.Null(ProductPricing.Estimate(5m, 0m).Percent);
}
