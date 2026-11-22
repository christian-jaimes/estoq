using Estoq.Core.Products;
using Xunit;

namespace Estoq.Core.Tests;

public class VariantPlannerTests
{
    [Fact]
    public void ThreeSizesAndTwoColoursGiveSixVariantsColoursFirst()
    {
        var slots = VariantPlanner.Plan(["S", "M", "L"], ["White", "Black"]);

        Assert.Equal(6, slots.Count);
        Assert.Equal(new VariantSlot("S", "White"), slots[0]);
        Assert.Equal(new VariantSlot("L", "White"), slots[2]);
        Assert.Equal(new VariantSlot("S", "Black"), slots[3]);
    }

    [Fact]
    public void NoColoursLeavesColourOut()
    {
        var slots = VariantPlanner.Plan(["S", "M"], []);
        Assert.Equal(new[] { new VariantSlot("S", null), new VariantSlot("M", null) }, slots.ToArray());
    }

    [Fact]
    public void NoSizesLeavesSizeOut()
    {
        var slots = VariantPlanner.Plan([], ["Red"]);
        Assert.Equal(new[] { new VariantSlot(null, "Red") }, slots.ToArray());
    }

    [Fact]
    public void NothingTickedGivesOnePlainVariant() =>
        Assert.Equal(new[] { new VariantSlot(null, null) }, VariantPlanner.Plan([], []).ToArray());

    [Fact]
    public void RepeatedValuesAreIgnored() =>
        Assert.Equal(2, VariantPlanner.Plan(["S", " s ", "M"], []).Count);
}
