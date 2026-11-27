using System.Globalization;
using System.Text;
using Estoq.Core.Inventory;
using Estoq.Core.Products;

namespace Estoq.App.Services;

/// <summary>
/// Inventory read from the development CSV fixtures: <c>dummy_article.csv</c> is the dimension, <c>dummy_stock_movements.csv</c>
/// the facts. Stock on hand is the sum of a variant's movements. Archived products are remembered for the session only.
/// </summary>
public sealed class SampleInventoryService : IInventoryService
{
    private const string ArticleFile = "dummy_article.csv";
    private const string MovementFile = "dummy_stock_movements.csv";
    private const string BrandFile = "dummy_brands.csv";

    private readonly HashSet<string> _archived = new(StringComparer.Ordinal);

    public IReadOnlyList<InventoryProduct> GetProducts()
    {
        var movements = LoadMovements();
        var onHand = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastMovement = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        foreach (var movement in movements)
        {
            onHand[movement.ArticleId] = onHand.GetValueOrDefault(movement.ArticleId) + movement.Quantity;
            if (!lastMovement.TryGetValue(movement.ArticleId, out var last) || movement.CreatedAt > last)
            {
                lastMovement[movement.ArticleId] = movement.CreatedAt;
            }
        }

        var suppliers = SampleCsv.Read(BrandFile)
            .Where(row => SampleCsv.Get(row, "brand_name").Length > 0)
            .GroupBy(row => SampleCsv.Get(row, "brand_name"), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => SampleCsv.Get(group.First(), "default_supplier"), StringComparer.OrdinalIgnoreCase);

        var products = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var article in SampleCsv.Read(ArticleFile))
        {
            var id = SampleCsv.Get(article, "article_id");
            if (id.Length == 0
                || (bool.TryParse(SampleCsv.Get(article, "is_active"), out var active) && !active)
                || !onHand.ContainsKey(id))
            {
                // Inactive articles, and variants that never had stock, are not shown.
                continue;
            }

            var key = ProductKey(article);
            if (!products.TryGetValue(key, out var rows))
            {
                rows = [];
                products[key] = rows;
                order.Add(key);
            }

            rows.Add(article);
        }

        var result = new List<InventoryProduct>(order.Count);
        foreach (var key in order)
        {
            var rows = products[key];
            var first = rows[0];
            var brand = SampleCsv.Get(first, "brand_name");
            var type = SampleCsv.Get(first, "product_type");
            var model = SampleCsv.Get(first, "model_label");
            var name = ProductNaming.GenerateName(brand, type, model);
            if (name.Length == 0)
            {
                name = SampleCsv.Get(first, "article_name");
            }

            var variants = rows.Select(row => ToVariant(row, onHand)).ToList();
            var updated = rows.Select(row => lastMovement.GetValueOrDefault(SampleCsv.Get(row, "article_id"))).Max();
            result.Add(new InventoryProduct(
                key,
                name,
                CommonCode(variants.Select(variant => variant.Sku).ToList()),
                SampleCsv.Get(first, "category_name"),
                brand,
                suppliers.GetValueOrDefault(brand) ?? string.Empty,
                type,
                model,
                StockLevels.DefaultLowStockThreshold,
                _archived.Contains(key),
                updated,
                variants));
        }

        return result;
    }

    public IReadOnlyList<InventoryMovement> GetMovements(string productId)
    {
        var product = GetProducts().FirstOrDefault(item => item.Id == productId);
        if (product is null)
        {
            return [];
        }

        var articles = product.Variants.Select(variant => variant.ArticleId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return LoadMovements()
            .Where(movement => articles.Contains(movement.ArticleId))
            .OrderByDescending(movement => movement.CreatedAt)
            .ThenByDescending(movement => movement.MovementId, StringComparer.Ordinal)
            .ToList();
    }

    public void SetArchived(string productId, bool archived)
    {
        if (archived)
        {
            _archived.Add(productId);
        }
        else
        {
            _archived.Remove(productId);
        }
    }

    private static InventoryVariant ToVariant(Dictionary<string, string> row, Dictionary<string, int> onHand)
    {
        var id = SampleCsv.Get(row, "article_id");
        return new InventoryVariant(
            id,
            SampleCsv.Get(row, "sku"),
            SampleCsv.Get(row, "supplier_barcode"),
            NullIfEmpty(SampleCsv.Get(row, "size")),
            NullIfEmpty(SampleCsv.Get(row, "color")),
            onHand.GetValueOrDefault(id),
            ParseMoney(SampleCsv.Get(row, "retail_selling_price")),
            ParseMoney(SampleCsv.Get(row, "standard_cost_price")));
    }

    private static List<InventoryMovement> LoadMovements()
    {
        var result = new List<InventoryMovement>();
        foreach (var row in SampleCsv.Read(MovementFile))
        {
            if (!int.TryParse(SampleCsv.Get(row, "stock_quantity"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity))
            {
                continue;
            }

            DateTime.TryParseExact(SampleCsv.Get(row, "created_at"), "dd-MM-yy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var created);
            result.Add(new InventoryMovement(
                SampleCsv.Get(row, "movement_id"),
                SampleCsv.Get(row, "article_id"),
                SampleCsv.Get(row, "movement_type"),
                quantity,
                SampleCsv.Get(row, "location_code"),
                created));
        }

        return result;
    }

    private static string ProductKey(Dictionary<string, string> article)
    {
        var model = ProductNaming.Key(SampleCsv.Get(article, "model_label"));
        if (model.Length == 0)
        {
            // Without a model label there is nothing to group on: the article is its own product.
            return SampleCsv.Get(article, "article_id");
        }

        return $"{ProductNaming.Key(SampleCsv.Get(article, "brand_name"))}|{ProductNaming.Key(SampleCsv.Get(article, "product_type"))}|{model}";
    }

    /// <summary>The leading SKU segments every variant shares ("BOSS-DSH"), or the first SKU when they share none.</summary>
    internal static string CommonCode(IReadOnlyList<string> skus)
    {
        if (skus.Count == 0)
        {
            return string.Empty;
        }

        if (skus.Count == 1)
        {
            return skus[0];
        }

        var parts = skus.Select(sku => sku.Split('-')).ToList();
        var shared = new List<string>();
        for (var index = 0; index < parts[0].Length; index++)
        {
            var segment = parts[0][index];
            if (parts.Any(other => index >= other.Length || !string.Equals(other[index], segment, StringComparison.OrdinalIgnoreCase)))
            {
                break;
            }

            shared.Add(segment);
        }

        return shared.Count == 0 ? skus[0] : string.Join('-', shared);
    }

    private static decimal ParseMoney(string text) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0m;

    private static string? NullIfEmpty(string text) => text.Length == 0 ? null : text;
}

/// <summary>Reads the development CSV fixtures. Samples only: the data layer replaces this.</summary>
internal static class SampleCsv
{
    public static List<Dictionary<string, string>> Read(string fileName)
    {
        var path = ResolvePath(fileName);
        if (!File.Exists(path))
        {
            return [];
        }

        var lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            return [];
        }

        var headers = ParseRow(lines[0]);
        var rows = new List<Dictionary<string, string>>();
        foreach (var line in lines.Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            var values = ParseRow(line);
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Length; index++)
            {
                var header = headers[index].Trim();
                if (header.Length == 0 || fields.ContainsKey(header))
                {
                    continue;
                }

                fields.Add(header, index < values.Length ? values[index].Trim() : string.Empty);
            }

            rows.Add(fields);
        }

        return rows;
    }

    public static string Get(IReadOnlyDictionary<string, string> row, string column) =>
        row.TryGetValue(column, out var value) ? value : string.Empty;

    /// <summary>The repository's data folder while developing (so rows the F2 pop-up appends show up), else the copy beside the app.</summary>
    private static string ResolvePath(string fileName)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var sourcePath = Path.Combine(directory.FullName, "data", fileName);
            if ((Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git"))) && File.Exists(sourcePath))
            {
                return sourcePath;
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "data", fileName);
    }

    private static string[] ParseRow(string row)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < row.Length; i++)
        {
            var character = row[i];
            if (character == '"')
            {
                if (quoted && i + 1 < row.Length && row[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }

        values.Add(value.ToString());
        return [.. values];
    }
}
