using Estoq.Core.Products;

namespace Estoq.App.Services;

/// <summary>In-memory catalogue with seeded clothing master data. Nothing is kept after the app closes.</summary>
public sealed class SampleCatalogService : ICatalogService
{
    private static readonly string[] LetterSizes = ["XS", "S", "M", "L", "XL", "XXL"];

    private readonly List<ProductTypeInfo> _types =
    [
        new("T-shirt", LetterSizes),
        new("Polo shirt", LetterSizes),
        new("Shirt", LetterSizes),
        new("Dress", ["XS", "S", "M", "L", "XL"]),
        new("Jeans", ["28", "30", "32", "34", "36", "38"]),
        new("Sneakers", ["38", "39", "40", "41", "42", "43", "44", "45"]),
        new("Belt", ["S", "M", "L"]),
        new("Cap", []),
    ];

    private readonly List<string> _categories = ["Tops", "Bottoms", "Dresses", "Footwear", "Accessories"];

    private readonly List<string> _brands = ["Hugo Boss", "Nike", "Adidas", "Levi's", "Zara", "Estoq Apparel"];

    private readonly List<ExistingProduct> _products =
    [
        new("sample-1", new ProductIdentity("Hugo Boss", "T-shirt", "Blue graphic"), "Hugo Boss T-shirt, Blue graphic"),
    ];

    private readonly HashSet<string> _skus = new(StringComparer.OrdinalIgnoreCase) { "TS-BLC-S" };

    public string DefaultCurrencyCode => "USD";

    public IReadOnlyList<ProductTypeInfo> ProductTypes => _types;

    public IReadOnlyList<string> Categories => _categories;

    public IReadOnlyList<string> Brands => _brands;

    public IReadOnlyList<ColourInfo> Colours { get; } =
    [
        new("White", "#F4F4F2", true),
        new("Black", "#1B1B1B", true),
        new("Navy", "#1F2F55", true),
        new("Red", "#B22C2C", true),
        new("Beige", "#E7D3B5", true),
        new("Grey", "#8A8D91", false),
        new("Green", "#3F7D4E", false),
        new("Blue", "#3B6FC4", false),
        new("Yellow", "#E3C13A", false),
        new("Orange", "#D9792B", false),
        new("Pink", "#D77AA3", false),
        new("Purple", "#7B5BA6", false),
        new("Brown", "#7A5233", false),
    ];

    public IReadOnlyList<ExistingProduct> ExistingProducts => _products;

    public IReadOnlySet<string> TakenSkus => _skus;

    // Next free internal barcode sequence number; moves on after every save.
    public long NextBarcodeSequence { get; private set; } = 1;

    public Task SaveAsync(NewProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);

        AddIfNew(_categories, product.Category);
        AddIfNew(_brands, product.Brand);
        if (!_types.Any(t => ProductNaming.Key(t.Name) == ProductNaming.Key(product.ProductType)))
        {
            _types.Add(new ProductTypeInfo(ProductNaming.Clean(product.ProductType), []));
        }

        _products.Add(new ExistingProduct(
            Guid.NewGuid().ToString("N"),
            new ProductIdentity(product.Brand, product.ProductType, product.ModelLabel),
            product.Name));

        foreach (var variant in product.Variants)
        {
            _skus.Add(variant.Sku);
        }

        // Keep every barcode unique even when the form dropped some variants.
        var highest = product.Variants
            .Select(v => long.Parse(v.InternalBarcode[1..12], System.Globalization.CultureInfo.InvariantCulture))
            .DefaultIfEmpty(NextBarcodeSequence - 1)
            .Max();
        NextBarcodeSequence = Math.Max(NextBarcodeSequence, highest + 1);
        return Task.CompletedTask;
    }

    private static void AddIfNew(List<string> list, string value)
    {
        var key = ProductNaming.Key(value);
        if (!list.Any(v => ProductNaming.Key(v) == key))
        {
            list.Add(ProductNaming.Clean(value));
        }
    }
}
