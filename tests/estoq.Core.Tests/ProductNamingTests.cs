using Estoq.Core.Products;
using Xunit;

namespace Estoq.Core.Tests;

public class ProductNamingTests
{
    [Fact]
    public void NameComesFromBrandTypeAndModelLabel() =>
        Assert.Equal("Hugo Boss T-shirt, Red graphic", ProductNaming.GenerateName("Hugo Boss", "T-shirt", "Red graphic"));

    [Fact]
    public void ExtraWhiteSpaceIsCleaned() =>
        Assert.Equal("Hugo Boss T-shirt, Red graphic", ProductNaming.GenerateName("  Hugo   Boss ", " T-shirt", "Red  graphic "));

    [Fact]
    public void MissingBrandIsLeftOut() =>
        Assert.Equal("T-shirt, Red graphic", ProductNaming.GenerateName(null, "T-shirt", "Red graphic"));

    [Theory]
    [InlineData("Hugo Boss", "", "Red graphic")]
    [InlineData("Hugo Boss", "T-shirt", " ")]
    [InlineData("Hugo Boss", null, null)]
    public void NameIsEmptyWithoutTypeOrModelLabel(string brand, string? type, string? model) =>
        Assert.Equal(string.Empty, ProductNaming.GenerateName(brand, type, model));

    [Theory]
    [InlineData("  Red   Graphic ", "RED GRAPHIC")]
    [InlineData("Camisón", "CAMISON")]
    [InlineData(null, "")]
    public void KeyIgnoresCaseAccentsAndSpacing(string? text, string expected) =>
        Assert.Equal(expected, ProductNaming.Key(text));
}
