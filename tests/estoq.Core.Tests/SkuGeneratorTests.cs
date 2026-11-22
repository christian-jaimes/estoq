using Estoq.Core.Products;
using Xunit;

namespace Estoq.Core.Tests;

public class SkuGeneratorTests
{
    private static readonly HashSet<string> None = [];

    [Fact]
    public void SkuJoinsTypeColourAndSize() =>
        Assert.Equal("TS-WHT-S", SkuGenerator.Generate("T-shirt", "White", "S", None));

    [Fact]
    public void PartsWithoutValueAreLeftOut()
    {
        Assert.Equal("TS-S", SkuGenerator.Generate("T-shirt", null, "S", None));
        Assert.Equal("TS-RD", SkuGenerator.Generate("T-shirt", "Red", null, None));
        Assert.Equal("TS", SkuGenerator.Generate("T-shirt", null, null, None));
    }

    [Fact]
    public void TakenSkuGetsANumberedSuffix()
    {
        var taken = new HashSet<string> { "ts-wht-s", "TS-WHT-S-2" };
        Assert.Equal("TS-WHT-S-3", SkuGenerator.Generate("T-shirt", "White", "S", taken));
    }

    [Theory]
    [InlineData("T-shirt", "TS")]
    [InlineData("White", "WHT")]
    [InlineData("Navy", "NVY")]
    [InlineData("Red", "RD")]
    [InlineData("Camisón", "CMS")]
    [InlineData("Polo shirt long sleeve extra", "PSL")]
    [InlineData("  ", "")]
    public void AbbreviationFollowsTheRules(string text, string expected) =>
        Assert.Equal(expected, SkuGenerator.Abbreviate(text));
}
