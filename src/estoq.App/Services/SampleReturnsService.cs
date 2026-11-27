using System.Globalization;
using Estoq.Core.Returns;

namespace Estoq.App.Services;

/// <summary>
/// Saves returns on the sample data. Returned units are appended to <c>dummy_stock_movements.csv</c> as "return" rows
/// and exchange items as "sale" rows; stock on hand is still the sum of an article's movements.
/// </summary>
public sealed class SampleReturnsService : IReturnsService
{
    private const string MovementFile = "dummy_stock_movements.csv";
    private const string ArticleFile = "dummy_article.csv";
    private const string DefaultLocation = "STORE-GHENT-CENTER";

    private readonly ISaleHistory _history;
    private readonly IPointOfSaleService _pointOfSale;
    private readonly INotificationService _notifications;
    private int _lastReturnNumber;

    public SampleReturnsService(ISaleHistory history, IPointOfSaleService pointOfSale, INotificationService notifications)
    {
        _history = history;
        _pointOfSale = pointOfSale;
        _notifications = notifications;
    }

    /// <summary>Sample store setting: a 30-day window, so the seeded 45-day-old sale shows the "outside the window" message.</summary>
    public int? ReturnWindowDays => 30;

    public SettledSale? FindSale(string receiptNumber) => _history.Find(receiptNumber);

    public ReturnReceipt Save(ReturnRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _lastReturnNumber++;
        var number = ReturnRules.FormatReturnNumber(_lastReturnNumber);

        WriteMovements(request);
        foreach (var line in request.Returned)
        {
            _pointOfSale.ApplyStockChange(line.VariantId, line.Quantity);
        }

        foreach (var line in request.NewItems)
        {
            _pointOfSale.ApplyStockChange(line.VariantId, -line.Quantity);
        }

        if (request.SaleNumber is { } saleNumber)
        {
            _history.ApplyReturn(saleNumber, request.Returned, request.RefundBase);
        }
        else
        {
            // A return without a receipt is allowed at the current price and flagged for the owner.
            _notifications.Add(new AppNotification(
                $"return-{number}",
                NotificationKind.ReturnWithoutReceipt,
                [number],
                [number],
                DateTimeOffset.Now,
                false));
        }

        return new ReturnReceipt(number);
    }

    private static void WriteMovements(ReturnRequest request)
    {
        var costs = SamplePointOfSaleService.ReadCsvRows(ArticleFile, "article", ["article_id", "standard_cost_price"])
            .ToDictionary(row => row["article_id"], row => row["standard_cost_price"], StringComparer.OrdinalIgnoreCase);
        var path = SamplePointOfSaleService.ResolveDataCsvPath(MovementFile);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The sample stock movements CSV was not copied to the app's data folder.", path);
        }

        var next = File.ReadAllLines(path)
            .Select(row => row.Split(',')[0])
            .Where(id => id.StartsWith("MOV-", StringComparison.OrdinalIgnoreCase))
            .Select(id => int.TryParse(id.AsSpan(4), NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : 0)
            .DefaultIfEmpty(0).Max() + 1;
        var now = DateTime.Now.ToString("dd-MM-yy HH:mm", CultureInfo.InvariantCulture);

        string Row(string variantId, string type, decimal price, int quantity) => string.Join(',',
            $"MOV-{next++}", variantId, type, costs.GetValueOrDefault(variantId, "0"),
            price.ToString("0.##", CultureInfo.InvariantCulture), quantity.ToString(CultureInfo.InvariantCulture), now, DefaultLocation);

        var rows = request.Returned.Select(line => Row(line.VariantId, "return", line.UnitPrice, line.Quantity)).ToList();
        rows.AddRange(request.NewItems.Select(line => Row(line.VariantId, "sale", line.UnitPrice, -line.Quantity)));
        if (rows.Count == 0)
        {
            return;
        }

        var text = File.ReadAllText(path);
        var prefix = text.Length == 0 || text.EndsWith('\n') ? string.Empty : Environment.NewLine;
        File.AppendAllText(path, prefix + string.Join(Environment.NewLine, rows) + Environment.NewLine);
    }
}
