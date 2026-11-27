using System.Globalization;
using Estoq.Core.Returns;
using Estoq.Core.Sales;
using Estoq.Core.Setup;

namespace Estoq.App.Services;

/// <summary>
/// In-memory sales until the back end exists. Four sample sales are built from the article table so Returns can be tried
/// straight away: receipts 000120 to 000123, the last one outside a 30-day return window. Sales completed at the till are added.
/// </summary>
public sealed class SampleSaleHistory : ISaleHistory
{
    private readonly Dictionary<string, SettledSale> _sales = [];

    public SampleSaleHistory()
    {
        try
        {
            Seed();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or KeyNotFoundException)
        {
            // Without the sample articles there are simply no seeded sales; sales rung up this session still work.
            _sales.Clear();
        }

        LoadLoggedSales();
    }

    public SettledSale? Find(string receiptNumber) => _sales.GetValueOrDefault(Normalize(receiptNumber));

    public void Record(SettledSale sale) => _sales[Normalize(sale.Number)] = sale;

    public void AddItems(string receiptNumber, IReadOnlyList<CartLine> items)
    {
        var key = Normalize(receiptNumber);
        if (items.Count == 0 || !_sales.TryGetValue(key, out var sale))
        {
            return;
        }

        var lines = sale.Lines.ToList();
        foreach (var item in items)
        {
            var index = lines.FindIndex(line => line.VariantId == item.VariantId);
            if (index >= 0)
            {
                lines[index] = lines[index] with { QuantitySold = lines[index].QuantitySold + item.Quantity };
            }
            else
            {
                lines.Add(new SoldLine(item.VariantId, item.Name, item.Sku, item.Sku, item.UnitPrice, item.Quantity, 0));
            }
        }

        var added = SaleTotals.Calculate(items.Sum(item => item.LineTotal), 0m, SampleStoreTax.Rate, SampleStoreTax.Mode);
        _sales[key] = sale with { Lines = lines, Subtotal = sale.Subtotal + added.Subtotal, Total = sale.Total + added.Total };
    }

    public void ApplyReturn(string receiptNumber, IReadOnlyList<ReturnedLine> returned, decimal refund)
    {
        var key = Normalize(receiptNumber);
        if (!_sales.TryGetValue(key, out var sale))
        {
            return;
        }

        var lines = new List<SoldLine>();
        foreach (var line in sale.Lines)
        {
            var units = returned.Where(item => item.VariantId == line.VariantId).Sum(item => item.Quantity);
            lines.Add(units > 0 ? line with { QuantityReturned = line.QuantityReturned + units } : line);
        }

        _sales[key] = sale with { Lines = lines, Refunded = sale.Refunded + refund };
    }

    /// <summary>Receipt numbers are six digits; anything typed around them ("#", "S-", spaces) is ignored.</summary>
    internal static string Normalize(string receiptNumber)
    {
        var digits = new string(receiptNumber.Where(char.IsAsciiDigit).ToArray());
        return digits.Length == 0 ? receiptNumber.Trim() : digits.TrimStart('0').PadLeft(6, '0');
    }

    /// <summary>
    /// Rebuilds the sales logged in <c>dummy_stock_movements.csv</c> by receipt number (rows of type "sale" and "return"), so a sale rung up
    /// in an earlier session can still be returned. The file keeps no discount or payment, so the total follows the sample store tax (<see cref="SampleStoreTax"/>).
    /// </summary>
    private void LoadLoggedSales()
    {
        try
        {
            var movements = SampleCsv.Read(StockMovementLog.MovementFile)
                .Where(row => SampleCsv.Get(row, "receipt_number").Length > 0)
                .GroupBy(row => Normalize(SampleCsv.Get(row, "receipt_number")));
            var articles = SampleCsv.Read(StockMovementLog.ArticleFile)
                .GroupBy(row => SampleCsv.Get(row, "article_id"), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            foreach (var receipt in movements)
            {
                var sold = receipt.Where(row => SampleCsv.Get(row, "movement_type") == "sale").ToList();
                var returned = receipt.Where(row => SampleCsv.Get(row, "movement_type") == "return").ToList();
                if (_sales.ContainsKey(receipt.Key))
                {
                    // A seeded sale has no logged sale rows of its own, so any here are exchange items taken against it.
                    AddItems(receipt.Key, [.. sold.GroupBy(row => SampleCsv.Get(row, "article_id"), StringComparer.OrdinalIgnoreCase).Select(group =>
                    {
                        var article = articles.GetValueOrDefault(group.Key);
                        var sku = article is null ? group.Key : SampleCsv.Get(article, "sku");
                        var price = decimal.Parse(SampleCsv.Get(group.First(), "selling_price"), NumberStyles.Number, CultureInfo.InvariantCulture);
                        var units = group.Sum(row => -int.Parse(SampleCsv.Get(row, "stock_quantity"), NumberStyles.Integer, CultureInfo.InvariantCulture));
                        return new CartLine(group.Key, article is null ? group.Key : SampleCsv.Get(article, "article_name"), sku, price, units);
                    })]);
                    ApplyLoggedReturns(receipt.Key, returned, articles);
                    continue;
                }

                if (sold.Count == 0)
                {
                    continue;
                }

                var lines = new List<SoldLine>();
                foreach (var group in sold.GroupBy(row => SampleCsv.Get(row, "article_id"), StringComparer.OrdinalIgnoreCase))
                {
                    var articleId = group.Key;
                    var article = articles.GetValueOrDefault(articleId);
                    var price = decimal.Parse(SampleCsv.Get(group.First(), "selling_price"), NumberStyles.Number, CultureInfo.InvariantCulture);
                    var units = group.Sum(row => -int.Parse(SampleCsv.Get(row, "stock_quantity"), NumberStyles.Integer, CultureInfo.InvariantCulture));
                    var back = returned.Where(row => string.Equals(SampleCsv.Get(row, "article_id"), articleId, StringComparison.OrdinalIgnoreCase))
                        .Sum(row => int.Parse(SampleCsv.Get(row, "stock_quantity"), NumberStyles.Integer, CultureInfo.InvariantCulture));
                    var sku = article is null ? articleId : SampleCsv.Get(article, "sku");
                    lines.Add(new SoldLine(articleId, article is null ? articleId : SampleCsv.Get(article, "article_name"), sku, sku, price, units, back));
                }

                var subtotal = lines.Sum(line => Money.Round(line.UnitPrice * line.QuantitySold));
                var totals = SaleTotals.Calculate(subtotal, 0m, SampleStoreTax.Rate, SampleStoreTax.Mode);
                var returnedValue = lines.Sum(line => Money.Round(line.UnitPrice * line.QuantityReturned));
                var refunded = subtotal == 0m ? 0m : Money.Round(returnedValue * totals.Total / subtotal);
                var date = DateTime.TryParseExact(SampleCsv.Get(sold[0], "created_at"), "dd-MM-yy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                    ? parsed
                    : DateTime.Now;
                _sales[receipt.Key] = new SettledSale(receipt.Key, date, lines, totals.Subtotal, totals.Total, refunded);
            }
        }
        catch (Exception ex) when (ex is IOException or FormatException or OverflowException)
        {
            // A damaged log leaves the seeded and this-session sales; it never stops the app.
        }
    }

    private void ApplyLoggedReturns(string receipt, List<Dictionary<string, string>> returned, Dictionary<string, Dictionary<string, string>> articles)
    {
        var lines = returned
            .GroupBy(row => SampleCsv.Get(row, "article_id"), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var article = articles.GetValueOrDefault(group.Key);
                var sku = article is null ? group.Key : SampleCsv.Get(article, "sku");
                var price = decimal.Parse(SampleCsv.Get(group.First(), "selling_price"), NumberStyles.Number, CultureInfo.InvariantCulture);
                var units = group.Sum(row => int.Parse(SampleCsv.Get(row, "stock_quantity"), NumberStyles.Integer, CultureInfo.InvariantCulture));
                return new ReturnedLine(group.Key, article is null ? group.Key : SampleCsv.Get(article, "article_name"), sku, price, units);
            })
            .ToList();
        var sale = _sales[receipt];
        var value = lines.Sum(line => Money.Round(line.UnitPrice * line.Quantity));
        ApplyReturn(receipt, lines, sale.Subtotal == 0m ? 0m : Money.Round(value * sale.Total / sale.Subtotal));
    }

    private void Seed()
    {
        var articles = SamplePointOfSaleService.ReadCsvRows("dummy_article.csv", "article", ["article_id", "article_name", "retail_selling_price", "sku"])
            .Where(row => !row.TryGetValue("is_active", out var active) || !bool.TryParse(active, out var isActive) || isActive)
            .Take(8)
            .ToList();
        if (articles.Count < 7)
        {
            return;
        }

        SoldLine Line(int index, int quantity)
        {
            var row = articles[index];
            var detail = string.Join(" · ", new[] { Get(row, "color"), Get(row, "size") }.Where(part => part.Length > 0));
            var price = decimal.Parse(row["retail_selling_price"], NumberStyles.Number, CultureInfo.InvariantCulture);
            return new SoldLine(row["article_id"], row["article_name"], row["sku"], detail.Length == 0 ? row["sku"] : detail, price, quantity, 0);
        }

        void Add(string number, int daysAgo, decimal discountPercent, params SoldLine[] lines)
        {
            var subtotal = lines.Sum(line => Money.Round(line.UnitPrice * line.QuantitySold));
            var totals = SaleTotals.Calculate(subtotal, discountPercent, SampleStoreTax.Rate, SampleStoreTax.Mode);
            Record(new SettledSale(number, DateTime.Now.AddDays(-daysAgo), lines, totals.Subtotal, totals.Total, 0m));
        }

        Add("000120", 1, 0m, Line(0, 1), Line(1, 2));
        Add("000121", 3, 10m, Line(2, 1));
        Add("000122", 10, 0m, Line(3, 1), Line(4, 1), Line(5, 3));
        Add("000123", 45, 0m, Line(6, 1));
    }

    private static string Get(IReadOnlyDictionary<string, string> row, string column) => row.TryGetValue(column, out var value) ? value : string.Empty;
}
