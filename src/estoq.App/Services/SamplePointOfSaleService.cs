using System.Globalization;
using System.Text;
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
    private readonly Dictionary<PaymentMethod, int> _paymentUsage = [];
    private readonly List<SaleVariant> _catalog;

    private int _lastSaleNumber = 124;

    public SamplePointOfSaleService(INotificationService notifications)
    {
        _notifications = notifications;
        _catalog = LoadCatalog();
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
        Assert16(out var tax);
        return new SaleSettings(rates, ["USD", "EUR", "VES", "COP"], tax, TaxMode.Excluded, DiscountPolicy.DefaultCapPercent);
    }

    public IReadOnlyList<DiscountPreset> GetDiscountPresets() => LoadDiscountPresets();

    public IReadOnlyDictionary<PaymentMethod, int> GetPaymentMethodUsage() => PaymentMethods.All
        .ToDictionary(method => method, GetPaymentUsage);

    public SaleVariant? FindByCode(string code)
    {
        var trimmed = code.Trim();
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

        return new SaleReceipt(number, request.Totals, settlement.Change);
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

    private static List<SaleVariant> LoadCatalog()
    {
        const int demoOnHand = 100;
        var path = Path.Combine(AppContext.BaseDirectory, "data", "dummy_inventory.csv");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The sample POS inventory CSV was not copied to the app's data folder.", path);
        }

        var rows = File.ReadAllLines(path);
        if (rows.Length < 2)
        {
            throw new InvalidDataException("The sample POS inventory CSV must contain a header and at least one item.");
        }

        var headers = ParseCsvRow(rows[0]);
        var columns = headers.Select((name, index) => (name, index))
            .ToDictionary(pair => pair.name, pair => pair.index, StringComparer.OrdinalIgnoreCase);
        var requiredColumns = new[] { "article_id", "article_name", "price", "barcode", "sku" };
        foreach (var required in requiredColumns)
        {
            if (!columns.ContainsKey(required))
            {
                throw new InvalidDataException($"The sample POS inventory CSV is missing the '{required}' column.");
            }
        }

        var catalog = new List<SaleVariant>();
        for (var rowIndex = 1; rowIndex < rows.Length; rowIndex++)
        {
            if (string.IsNullOrWhiteSpace(rows[rowIndex]))
            {
                continue;
            }

            var values = ParseCsvRow(rows[rowIndex]);
            if (values.Length != headers.Length)
            {
                throw new InvalidDataException($"Sample POS inventory row {rowIndex + 1} has {values.Length} fields; expected {headers.Length}.");
            }

            string Value(string column) => values[columns[column]].Trim();

            var id = Value("article_id");
            var name = Value("article_name");
            var barcode = Value("barcode");
            var sku = Value("sku");
            if (id.Length == 0 || name.Length == 0 || barcode.Length == 0 || sku.Length == 0)
            {
                throw new InvalidDataException($"Sample POS inventory row {rowIndex + 1} needs an article ID, name, barcode, and SKU.");
            }

            if (!decimal.TryParse(Value("price"), NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0m)
            {
                throw new InvalidDataException($"Sample POS inventory row {rowIndex + 1} has an invalid price.");
            }

            catalog.Add(new SaleVariant(id, name, sku, barcode, price, demoOnHand));
        }

        if (catalog.Count == 0)
        {
            throw new InvalidDataException("The sample POS inventory CSV does not contain any sellable items.");
        }

        return catalog;
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

    private static void Assert16(out TaxRate tax)
    {
        // Sample store charges 16 % to exercise the tax rows; the setup default is 0 %.
        if (!TaxRate.TryCreate(16m, out tax))
        {
            tax = TaxRate.Default;
        }
    }
}
