using Estoq.Core.Inventory;
using Xunit;

namespace Estoq.Core.Tests;

public class StockLevelsTests
{
    [Theory]
    [InlineData(0, 5, StockStatus.Out)]
    [InlineData(-2, 5, StockStatus.Out)]
    [InlineData(1, 5, StockStatus.Low)]
    [InlineData(5, 5, StockStatus.Low)]
    [InlineData(6, 5, StockStatus.InStock)]
    [InlineData(1, 0, StockStatus.InStock)]
    [InlineData(1, -3, StockStatus.InStock)]
    public void VariantStatusFollowsTheThreshold(int onHand, int threshold, StockStatus expected) =>
        Assert.Equal(expected, StockLevels.Classify(onHand, threshold));

    [Fact]
    public void ProductIsOutOnlyWhenEveryVariantIsOut() =>
        Assert.Equal(StockStatus.Out, StockLevels.ClassifyProduct([0, 0], 5));

    [Fact]
    public void ProductIsLowWhenAnyVariantIsLowOrOut()
    {
        Assert.Equal(StockStatus.Low, StockLevels.ClassifyProduct([40, 3], 5));
        Assert.Equal(StockStatus.Low, StockLevels.ClassifyProduct([40, 0], 5));
    }

    [Fact]
    public void ProductIsInStockWhenEveryVariantIsAboveTheThreshold() =>
        Assert.Equal(StockStatus.InStock, StockLevels.ClassifyProduct([10, 20], 5));

    [Fact]
    public void ProductWithoutVariantsIsOut() => Assert.Equal(StockStatus.Out, StockLevels.ClassifyProduct([], 5));
}
