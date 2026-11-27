using System.Globalization;
using System.Text;
using Estoq.Core.Returns;
using Estoq.Core.Roles;
using Estoq.Core.Sales;
using Estoq.Core.Setup;
using Windows.Storage;

namespace Estoq.App.Services;

/// <summary>
/// In-memory till catalog until the back end exists. Stock drops per sale for the session; payment-method
/// usage counts persist locally to sort the method tiles by store activity.
/// </summary>
public sealed class SamplePointOfSaleService : IPointOfSaleService
{
    private const string ApprovalCode = "000000";
    private const string PaymentUsagePrefix = "pos_payment_usage_";

    private readonly INotificationService _notifications;
    private readonly ISaleHistory _history;
    private readonly Dictionary<PaymentMethod, int> _paymentUsage = [];
    private readonly List<SaleVariant> _catalog;

    private int _lastSaleNumber;

    public SamplePointOfSaleService(INotificationService notifications, ISaleHistory history)
    {
        _notifications = notifications;
        _history = history;
        _catalog = LoadCatalog();
        _lastSaleNumber = LastReceiptNumber();
    }

    public UserRole CurrentRole => UserRole.Owner;

    public SaleSettings GetSettings()
    {
        var rates = new ExchangeRates(Currencies.Default, new Dictionary<string, decimal>
        {
            ["EUR"] = 0.92m,
            ["VES"] = 36.50m,
            ["COP"] = 4100m,
        });
        return new SaleSettings(rates, ["USD", "EUR", "VES", "COP"], SampleStoreTax.Rate, SampleStoreTax.Mode, DiscountPolicy.DefaultCapPercent);
    }

    public IReadOnlyList<DiscountPreset> GetDiscountPresets() => LoadDiscountPresets();

    public IReadOnlyDictionary<PaymentMethod, int> GetPaymentMethodUsage() => PaymentMethods.All
        .ToDictionary(method => method, GetPaymentUsage);

    public SaleVariant? FindByCode(string code)
    {
        var trimmed = code.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        return _catalog.FirstOrDefault(v =>
            string.Equals(v.Barcode, trimmed, StringComparison.Ordinal)
            || string.Equals(v.Sku, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<SaleVariant> Search(string text, int maxResults)
    {
        var query = text.Trim();
        if (query.Length == 0)
        {
            return [];
        }

        return _catalog
            .Where(v => v.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || v.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase)
                || v.Sku.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(v => v.Name.StartsWith(query, StringComparison.CurrentCultureIgnoreCase)
                || v.Barcode.StartsWith(query, StringComparison.OrdinalIgnoreCase)
                || v.Sku.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();
    }

    /// <summary>Stand-in until accounts exist: a fixed sample manager code, never a real credential.</summary>
    public bool ApproveAsManager(string userCode) => string.Equals(userCode, ApprovalCode, StringComparison.Ordinal);

    public SaleReceipt Complete(SaleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var settlement = PaymentSettlement.Calculate(request.Totals.Total, request.Payments, request.Rates);
        RecordPaymentUsage(request.Payments);
        _lastSaleNumber++;
        var number = _lastSaleNumber.ToString("D6", CultureInfo.InvariantCulture);

        foreach (var line in request.Lines)
        {
            var index = _catalog.FindIndex(v => v.VariantId == line.VariantId);
            if (index < 0)
            {
                continue;
            }

            var variant = _catalog[index];
            var withoutStock = StockCheck.UnitsWithoutStock(variant.OnHand, line.Quantity);
            _catalog[index] = variant with { OnHand = variant.OnHand - line.Quantity };
            if (withoutStock > 0)
            {
                _notifications.Add(new AppNotification(
                    $"sale-{number}-{variant.VariantId}",
                    NotificationKind.SoldWithoutStock,
                    [variant.Name],
                    [number],
                    DateTimeOffset.Now,
                    false));
            }
        }

        // Log the sale's stock movements with the receipt number, so Returns can find the sale again (also after a restart).
        try
        {
            StockMovementLog.Append([.. request.Lines.Select(line => new MovementEntry(line.VariantId, "sale", line.UnitPrice, -line.Quantity, number))]);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The sample CSV is a development fixture: a log failure must not lose a sale that is already rung up.
        }

        // Keep the sale so Returns (F4) can find it by receipt number.
        _history.Record(new SettledSale(
            number,
            DateTime.Now,
            [.. request.Lines.Select(line => new SoldLine(line.VariantId, line.Name, line.Sku, line.Sku, line.UnitPrice, line.Quantity, 0))],
            request.Totals.Subtotal,
            request.Totals.Total,
            0m));

        return new SaleReceipt(number, request.Totals, settlement.Change);
    }

    /// <summary>The highest receipt number used so far: the seeded sales (up to 000124) or the last one logged in the movements file.</summary>
    private static int LastReceiptNumber()
    {
        var last = 124;
        try
        {
            foreach (var row in SampleCsv.Read(StockMovementLog.MovementFile))
            {
                var digits = SampleCsv.Get(row, "receipt_number");
                if (int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                {
                    last = Math.Max(last, number);
                }
            }
        }
        catch (IOException)
        {
            // Without the file the numbering simply starts after the seeded sales.
        }

        return last;
    }

    /// <summary>Moves a variant's on-hand count by <paramref name="delta"/> (a customer return adds, an exchange item takes).</summary>
    public void ApplyStockChange(string variantId, int delta)
    {
        var index = _catalog.FindIndex(v => v.VariantId == variantId);
        if (index >= 0)
        {
            _catalog[index] = _catalog[index] with { OnHand = Math.Max(0, _catalog[index].OnHand + delta) };
        }
    }

    private int GetPaymentUsage(PaymentMethod method)
    {
        if (_paymentUsage.TryGetValue(method, out var usage))
        {
            return usage;
        }

        try
        {
            usage = ApplicationData.Current.LocalSettings.Values[PaymentUsagePrefix + method] is int count ? count : 0;
        }
        catch (InvalidOperationException)
        {
            usage = 0;
        }

        _paymentUsage[method] = usage;
        return usage;
    }

    private void RecordPaymentUsage(IEnumerable<PaymentLine> payments)
    {
        var counts = payments.GroupBy(payment => payment.Method)
            .ToDictionary(group => group.Key, group => GetPaymentUsage(group.Key) + group.Count());
        foreach (var (method, count) in counts)
        {
            _paymentUsage[method] = count;
        }

        try
        {
            var values = ApplicationData.Current.LocalSettings.Values;
            foreach (var (method, count) in counts)
            {
                values[PaymentUsagePrefix + method] = count;
            }
        }
        catch (InvalidOperationException)
        {
            // Keep session-local counts when a development run is unpackaged.
        }
    }

    /// <summary>
    /// Builds the till catalog from the article table (<c>dummy_article.csv</c>) and the stock movements fact table
    /// (<c>dummy_stock_movements.csv</c>): stock on hand is the sum of an article's movements across locations.
    /// </summary>
    private static List<SaleVariant> LoadCatalog()
    {
        var articles = ReadCsvRows("dummy_article.csv", "article", ["article_id", "article_name", "retail_selling_price", "sku"]);
        var movements = ReadCsvRows("dummy_stock_movements.csv", "stock movements", ["article_id", "stock_quantity"]);

        var onHand = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var movement in movements)
        {
            if (!int.TryParse(movement["stock_quantity"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity))
            {
                throw new InvalidDataException($"A stock movement for '{movement["article_id"]}' has an invalid stock_quantity.");
            }

            onHand[movement["article_id"]] = onHand.GetValueOrDefault(movement["article_id"]) + quantity;
        }

        var catalog = new List<SaleVariant>();
        foreach (var article in articles)
        {
            if (article.TryGetValue("is_active", out var active) && bool.TryParse(active, out var isActive) && !isActive)
            {
                continue;
            }

            var id = article["article_id"];
            var name = article["article_name"];
            var sku = article["sku"];
            var barcode = article.GetValueOrDefault("supplier_barcode", string.Empty);
            if (id.Length == 0 || name.Length == 0 || sku.Length == 0)
            {
                throw new InvalidDataException("Every article needs an article ID, name, and SKU.");
            }

            if (!decimal.TryParse(article["retail_selling_price"], NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0m)
            {
                throw new InvalidDataException($"Article '{id}' has an invalid retail_selling_price.");
            }

            catalog.Add(new SaleVariant(id, name, sku, barcode, price, Math.Max(0, onHand.GetValueOrDefault(id))));
        }

        if (catalog.Count == 0)
        {
            throw new InvalidDataException("The article table does not contain any sellable items.");
        }

        return catalog;
    }

    internal static List<Dictionary<string, string>> ReadCsvRows(string fileName, string label, string[] requiredColumns)
    {
        var path = ResolveDataCsvPath(fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The sample {label} CSV was not copied to the app's data folder.", path);
        }

        var rows = File.ReadAllLines(path);
        if (rows.Length < 2)
        {
            return [];
        }

        var headers = ParseCsvRow(rows[0]).Select(header => header.Trim()).ToArray();
        foreach (var required in requiredColumns)
        {
            if (!headers.Contains(required, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The sample {label} CSV is missing the '{required}' column.");
            }
        }

        var result = new List<Dictionary<string, string>>();
        for (var rowIndex = 1; rowIndex < rows.Length; rowIndex++)
        {
            if (string.IsNullOrWhiteSpace(rows[rowIndex]))
            {
                continue;
            }

            var values = ParseCsvRow(rows[rowIndex]);
            if (values.Length != headers.Length)
            {
                throw new InvalidDataException($"Row {rowIndex + 1} of the sample {label} CSV has {values.Length} fields; expected {headers.Length}.");
            }

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var column = 0; column < headers.Length; column++)
            {
                row[headers[column]] = values[column].Trim();
            }

            result.Add(row);
        }

        return result;
    }

    private static List<DiscountPreset> LoadDiscountPresets()
    {
        var path = ResolveDiscountCsvPath();
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The sample POS discount CSV was not copied to the app's data folder.", path);
        }

        var rows = File.ReadAllLines(path);
        if (rows.Length < 2)
        {
            return [];
        }

        var headers = ParseCsvRow(rows[0]);
        var columns = headers.Select((name, index) => (name, index))
            .ToDictionary(pair => pair.name, pair => pair.index, StringComparer.OrdinalIgnoreCase);
        foreach (var required in new[] { "discount", "is_active" })
        {
            if (!columns.ContainsKey(required))
            {
                throw new InvalidDataException($"The sample POS discount CSV is missing the '{required}' column.");
            }
        }

        var presets = new List<DiscountPreset>();
        for (var rowIndex = 1; rowIndex < rows.Length; rowIndex++)
        {
            if (string.IsNullOrWhiteSpace(rows[rowIndex]))
            {
                continue;
            }

            var values = ParseCsvRow(rows[rowIndex]);
            if (values.Length != headers.Length)
            {
                throw new InvalidDataException($"Sample POS discount row {rowIndex + 1} has {values.Length} fields; expected {headers.Length}.");
            }

            var rawPercent = values[columns["discount"]].Trim();
            if (rawPercent.Length == 0)
            {
                continue;
            }

            if (!decimal.TryParse(rawPercent, NumberStyles.Number, CultureInfo.InvariantCulture, out var percent)
                || percent <= 0m || percent > 100m)
            {
                throw new InvalidDataException($"Sample POS discount row {rowIndex + 1} has an invalid percentage.");
            }

            var rawStatus = values[columns["is_active"]].Trim();
            if (!bool.TryParse(rawStatus, out var isActive))
            {
                throw new InvalidDataException($"Sample POS discount row {rowIndex + 1} has an invalid is_active value.");
            }

            presets.Add(new DiscountPreset(percent, isActive));
        }

        return presets.Where(preset => preset.IsActive)
            .DistinctBy(preset => preset.Percent)
            .OrderBy(preset => preset.Percent)
            .ToList();
    }

    private static string ResolveDiscountCsvPath()
    {
        // In development, use the repository data file so status edits take effect on the next flyout open,
        // without requiring a rebuild to refresh the copied output file. Packaged builds use their local copy.
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var repositoryMarker = Path.Combine(directory.FullName, ".git");
            var sourcePath = Path.Combine(directory.FullName, "data", "dummy_discount.csv");
            if ((Directory.Exists(repositoryMarker) || File.Exists(repositoryMarker)) && File.Exists(sourcePath))
            {
                return sourcePath;
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "data", "dummy_discount.csv");
    }

    /// <summary>In development, reads the repository data file so products added in the app show up after a restart.</summary>
    internal static string ResolveDataCsvPath(string fileName)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var repositoryMarker = Path.Combine(directory.FullName, ".git");
            var sourcePath = Path.Combine(directory.FullName, "data", fileName);
            if ((Directory.Exists(repositoryMarker) || File.Exists(repositoryMarker)) && File.Exists(sourcePath))
            {
                return sourcePath;
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "data", fileName);
    }

    private static string[] ParseCsvRow(string row)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var insideQuotes = false;

        for (var index = 0; index < row.Length; index++)
        {
            var character = row[index];
            if (character == '"')
            {
                if (insideQuotes && index + 1 < row.Length && row[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (character == ',' && !insideQuotes)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }

        if (insideQuotes)
        {
            throw new InvalidDataException("The sample POS inventory CSV contains an unclosed quoted field.");
        }

        values.Add(value.ToString());
        return [.. values];
    }
}
