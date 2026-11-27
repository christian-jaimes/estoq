using Estoq.Core.Products;
using Xunit;

namespace Estoq.Core.Tests;

public class ProductDuplicateCheckTests
{
    private static readonly ExistingProduct[] Catalogue =
    [
        new("p1", new ProductIdentity("Hugo Boss", "T-shirt", "Blue graphic"), "Hugo Boss T-shirt, Blue graphic"),
        new("p2", new ProductIdentity("Hugo Boss", "T-shirt", "Red graphic"), "Hugo Boss T-shirt, Red graphic"),
    ];

    [Fact]
    public void SameBrandTypeAndModelLabelIsFound()
    {
        var found = ProductDuplicateCheck.Find(Catalogue, new ProductIdentity("hugo  boss", "T-SHIRT", " red graphic "));
        Assert.Equal("p2", found?.Id);
    }

    [Fact]
    public void DifferentModelLabelIsANewProduct() =>
        Assert.Null(ProductDuplicateCheck.Find(Catalogue, new ProductIdentity("Hugo Boss", "T-shirt", "Green graphic")));

    [Fact]
    public void DifferentBrandIsANewProduct() =>
        Assert.Null(ProductDuplicateCheck.Find(Catalogue, new ProductIdentity("Nike", "T-shirt", "Red graphic")));

    [Fact]
    public void EmptyModelLabelNeverMatches() =>
        Assert.Null(ProductDuplicateCheck.Find(Catalogue, new ProductIdentity("Hugo Boss", "T-shirt", "")));
}
