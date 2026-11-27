using Estoq.Core.Stock;
using Xunit;

namespace Estoq.Core.Tests;

public class StockAdjustmentRulesTests
{
    [Fact]
    public void DifferenceIsCountedMinusCurrent()
    {
        Assert.Equal(3, new AdjustmentLine(24, 27).Difference);
        Assert.Equal(-2, new AdjustmentLine(18, 16).Difference);
        Assert.Equal(0, new AdjustmentLine(5, 5).Difference);
    }

    [Fact]
    public void SummaryAddsIncreasesAndDecreasesSeparately()
    {
        var summary = StockAdjustmentRules.Summarize([new(24, 27), new(18, 16), new(42, 37)]);
        Assert.Equal(3, summary.Increases);
        Assert.Equal(7, summary.Decreases);
        Assert.Equal(-4, summary.Net);
    }

    [Fact]
    public void EmptySummaryIsZero()
    {
        var summary = StockAdjustmentRules.Summarize([]);
        Assert.Equal(0, summary.Net);
    }

    [Theory]
    [InlineData(AdjustmentType.Recount, true)]
    [InlineData(AdjustmentType.SoldWithoutStock, true)]
    [InlineData(AdjustmentType.Damaged, false)]
    [InlineData(AdjustmentType.Lost, false)]
    public void OnlyCountTypesMayIncreaseStock(AdjustmentType type, bool allowed) =>
        Assert.Equal(allowed, StockAdjustmentRules.AllowsIncrease(type));

    [Fact]
    public void CountedNeverGoesBelowZero() =>
        Assert.Equal(0, StockAdjustmentRules.ClampCounted(AdjustmentType.Recount, 4, -3));

    [Fact]
    public void DamagedAndLostCannotExceedCurrentStock()
    {
        Assert.Equal(4, StockAdjustmentRules.ClampCounted(AdjustmentType.Damaged, 4, 9));
        Assert.Equal(4, StockAdjustmentRules.ClampCounted(AdjustmentType.Lost, 4, 9));
        Assert.Equal(9, StockAdjustmentRules.ClampCounted(AdjustmentType.Recount, 4, 9));
    }

    [Fact]
    public void WithoutATypeYetOnlyTheFloorApplies() =>
        Assert.Equal(9, StockAdjustmentRules.ClampCounted(null, 4, 9));

    [Fact]
    public void QuantityIsCappedAtTheMaximum() =>
        Assert.Equal(StockAdjustmentRules.MaxQuantity, StockAdjustmentRules.ClampCounted(AdjustmentType.Recount, 1, int.MaxValue));

    [Fact]
    public void StepMovesTheCountedQuantityWithinTheRules()
    {
        Assert.Equal(6, StockAdjustmentRules.Step(AdjustmentType.Recount, 5, 5, 1));
        Assert.Equal(0, StockAdjustmentRules.Step(AdjustmentType.Recount, 5, 0, -1));
        Assert.Equal(5, StockAdjustmentRules.Step(AdjustmentType.Lost, 5, 5, 1));
    }

    [Fact]
    public void EachTypeHasOneReasonAndItsOwnMovementType()
    {
        var movementTypes = new HashSet<string>();
        foreach (var type in Enum.GetValues<AdjustmentType>())
        {
            Assert.Single(StockAdjustmentRules.Reasons(type));
            Assert.True(movementTypes.Add(StockAdjustmentRules.MovementType(type)));
        }
    }

    [Fact]
    public void DamagedWritesTheExistingDefectMovementType() =>
        Assert.Equal("defect", StockAdjustmentRules.MovementType(AdjustmentType.Damaged));

    [Fact]
    public void ValidationNeedsAnItemWithADifference()
    {
        Assert.Equal(AdjustmentValidation.NoItems, StockAdjustmentRules.Validate([]));
        Assert.Equal(AdjustmentValidation.NothingChanged, StockAdjustmentRules.Validate([new(5, 5)]));
        Assert.Equal(AdjustmentValidation.None, StockAdjustmentRules.Validate([new(5, 5), new(5, 4)]));
    }
}
