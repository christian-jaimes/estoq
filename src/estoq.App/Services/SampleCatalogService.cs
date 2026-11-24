using System.Text;
using Estoq.Core.Products;

namespace Estoq.App.Services;

/// <summary>Sample catalog backed by the development master-data CSV fixtures.</summary>
public sealed class SampleCatalogService : ICatalogService
{
    private readonly List<ProductTypeInfo> _types;
    private readonly List<string> _categories;
    private readonly List<BrandInfo> _brandDetails;

    private readonly List<ExistingProduct> _products =
    [
        new("sample-1", new ProductIdentity("Hugo Boss", "T-shirt", "Blue graphic"), "Hugo Boss T-shirt, Blue graphic"),
    ];

    private readonly HashSet<string> _skus = new(StringComparer.OrdinalIgnoreCase) { "TS-BLC-S" };

    public SampleCatalogService()
    {
        _brandDetails = LoadBrands();
        _categories = LoadCategories();
        _types = LoadTypes(_categories);
    }

    public string DefaultCurrencyCode => "USD";

    public IReadOnlyList<ProductTypeInfo> ProductTypes => _types;

    public IReadOnlyList<string> Categories => _categories;

    public IReadOnlyList<string> Brands => _brandDetails.Where(b => b.IsActive).Select(b => b.Name).ToList();

    public IReadOnlyList<BrandInfo> BrandDetails => _brandDetails;

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

        AddCategoryIfNew(product.Category);
        AddBrandIfNew(product.Brand);
        AddTypeIfNew(product.ProductType);

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

    private void AddBrandIfNew(string value)
    {
        var name = ProductNaming.Clean(value);
        if (name.Length == 0 || _brandDetails.Any(b => ProductNaming.Key(b.Name) == ProductNaming.Key(name))) return;
        var used = _brandDetails.Select(b => b.Id).Where(id => id.StartsWith("BRD-", StringComparison.OrdinalIgnoreCase))
            .Select(id => int.TryParse(id.AsSpan(4), out var number) ? number : 0).ToHashSet();
        var next = 1;
        while (used.Contains(next)) next++;
        var skuCode = CreateSkuCode(name, _brandDetails.Select(brand => brand.SkuCode));
        var brand = new BrandInfo($"BRD-{next:00}", name, skuCode, true);
        var csvPath = ResolveCsvPath("dummy_brands.csv");
        var csvRow = $"{brand.Id},{EscapeCsv(brand.Name)},{brand.SkuCode},,,,,True";
        File.AppendAllText(csvPath, Environment.NewLine + csvRow);
        _brandDetails.Add(brand);
    }

    private void AddCategoryIfNew(string value)
    {
        var name = ProductNaming.Clean(value);
        if (name.Length == 0 || _categories.Any(item => ProductNaming.Key(item) == ProductNaming.Key(name))) return;
        var id = NextId("dummy_categories.csv", "category_id", "CAT-");
        File.AppendAllText(ResolveCsvPath("dummy_categories.csv"), Environment.NewLine + $"{id},{EscapeCsv(name)},,,TRUE");
        _categories.Add(name);
    }

    private void AddTypeIfNew(string value)
    {
        var name = ProductNaming.Clean(value);
        if (name.Length == 0 || _types.Any(item => ProductNaming.Key(item.Name) == ProductNaming.Key(name))) return;
        var id = NextId("dummy_product_types.csv", "type_id", "TYP-");
        var code = CreateSkuCode(name, ReadCsv("dummy_product_types.csv").Select(row => Get(row, "sku_code")));
        File.AppendAllText(ResolveCsvPath("dummy_product_types.csv"), Environment.NewLine + $"{id},{EscapeCsv(name)},,{code},,,");
        _types.Add(new ProductTypeInfo(name, []));
    }

    private static List<BrandInfo> LoadBrands() => ReadCsv("dummy_brands.csv")
        .Where(row => IsActive(row, "is_active"))
        .Select(row => new BrandInfo(Get(row, "brand_id"), Get(row, "brand_name"), Get(row, "sku_code"), true))
        .Where(brand => brand.Name.Length > 0)
        .ToList();

    private static List<string> LoadCategories() => ReadCsv("dummy_categories.csv")
        .Where(row => IsActive(row, "is_active"))
        .Select(row => Get(row, "category_name"))
        .Where(name => name.Length > 0)
        .ToList();

    private static List<ProductTypeInfo> LoadTypes(IReadOnlyList<string> categories)
    {
        return ReadCsv("dummy_product_types.csv")
            .Select(row => new ProductTypeInfo(Get(row, "type_name"), SizesFor(Get(row, "type_name"), Get(row, "requires_size"))))
            .Where(type => type.Name.Length > 0)
            .ToList();
    }

    private static IReadOnlyList<string> SizesFor(string typeName, string requiresSize)
    {
        if (!bool.TryParse(requiresSize, out var needsSize) || !needsSize) return [];
        var key = ProductNaming.Key(typeName);
        if (key.Contains("shoe") || key.Contains("sneaker") || key.Contains("boot")) return ["38", "39", "40", "41", "42", "43", "44", "45"];
        if (key.Contains("jean") || key.Contains("chino")) return ["28", "30", "32", "34", "36", "38"];
        if (key.Contains("belt")) return ["S", "M", "L"];
        return ["XS", "S", "M", "L", "XL", "XXL"];
    }

    private static IEnumerable<Dictionary<string, string>> ReadCsv(string fileName)
    {
        var path = ResolveCsvPath(fileName);
        if (!File.Exists(path)) return [];
        var rows = File.ReadAllLines(path);
        if (rows.Length < 2) return [];
        var headers = ParseCsvRow(rows[0]);
        return rows.Skip(1).Where(row => !string.IsNullOrWhiteSpace(row)).Select(row =>
        {
            var values = ParseCsvRow(row);
            return headers.Select((header, index) => (header, value: index < values.Length ? values[index].Trim() : string.Empty))
                .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase);
        }).ToList();
    }

    private static bool IsActive(IReadOnlyDictionary<string, string> row, string column) =>
        !bool.TryParse(Get(row, column), out var active) || active;

    private static string Get(IReadOnlyDictionary<string, string> row, string column) => row.TryGetValue(column, out var value) ? value : string.Empty;

    private static string EscapeCsv(string value) => value.Contains(',') || value.Contains('"') || value.Contains('\n')
        ? $"\"{value.Replace("\"", "\"\"")}\""
        : value;

    private static string CreateSkuCode(string name, IEnumerable<string> existingCodes)
    {
        var used = existingCodes.Where(code => code.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var letters = new string(name.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        var baseCode = words.Length > 1
            ? string.Concat(words.Take(3).Select(word => char.ToUpperInvariant(word[0])))
            : letters[..Math.Min(3, letters.Length)];
        if (baseCode.Length == 0) baseCode = "BRD";
        if (!used.Contains(baseCode)) return baseCode;
        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseCode}{suffix}";
            if (!used.Contains(candidate)) return candidate;
        }
    }

    private static string NextId(string fileName, string idColumn, string prefix)
    {
        var used = ReadCsv(fileName).Select(row => Get(row, idColumn))
            .Where(id => id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(id => int.TryParse(id.AsSpan(prefix.Length), out var number) ? number : 0).ToHashSet();
        var next = 1;
        while (used.Contains(next)) next++;
        return $"{prefix}{next:00}";
    }

    private static string ResolveCsvPath(string fileName)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var sourcePath = Path.Combine(directory.FullName, "data", fileName);
            if ((Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git"))) && File.Exists(sourcePath))
                return sourcePath;
        }
        return Path.Combine(AppContext.BaseDirectory, "data", fileName);
    }

    private static string[] ParseCsvRow(string row)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < row.Length; i++)
        {
            var character = row[i];
            if (character == '"')
            {
                if (quoted && i + 1 < row.Length && row[i + 1] == '"') { value.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted) { values.Add(value.ToString()); value.Clear(); }
            else value.Append(character);
        }
        values.Add(value.ToString());
        return [.. values];
    }
}
