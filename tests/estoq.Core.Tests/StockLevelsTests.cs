using Estoq.Core.Inventory;
using Estoq.Core.Settings;
using Xunit;

namespace Estoq.Core.Tests;

public class StockLevelsTests
{
    [Theory]
    [InlineData(0, 5, StockLevel.NoStock)]
    [InlineData(-2, 5, StockLevel.NoStock)]
    [InlineData(1, 5, StockLevel.LowStock)]
    [InlineData(5, 5, StockLevel.LowStock)]
    [InlineData(6, 5, StockLevel.InStock)]
    [InlineData(1, 0, StockLevel.InStock)]
    [InlineData(1, -3, StockLevel.InStock)]
    public void VariantStatusFollowsTheThreshold(int onHand, int threshold, StockLevel expected) =>
        Assert.Equal(expected, StockLevels.Classify(onHand, threshold));

    [Fact]
    public void ProductIsOutOnlyWhenEveryVariantIsOut() =>
        Assert.Equal(StockLevel.NoStock, StockLevels.ClassifyProduct([0, 0], 5));

    [Fact]
    public void ProductIsLowWhenAnyVariantIsLowOrOut()
    {
        Assert.Equal(StockLevel.LowStock, StockLevels.ClassifyProduct([40, 3], 5));
        Assert.Equal(StockLevel.LowStock, StockLevels.ClassifyProduct([40, 0], 5));
    }

    [Fact]
    public void ProductIsInStockWhenEveryVariantIsAboveTheThreshold() =>
        Assert.Equal(StockLevel.InStock, StockLevels.ClassifyProduct([10, 20], 5));

    [Fact]
    public void ProductWithoutVariantsHasNoStock() => Assert.Equal(StockLevel.NoStock, StockLevels.ClassifyProduct([], 5));
}
