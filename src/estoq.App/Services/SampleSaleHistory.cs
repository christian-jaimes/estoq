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
    }

    public SettledSale? Find(string receiptNumber) => _sales.GetValueOrDefault(Normalize(receiptNumber));

    public void Record(SettledSale sale) => _sales[Normalize(sale.Number)] = sale;

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

    private void Seed()
    {
        var articles = SamplePointOfSaleService.ReadCsvRows("dummy_article.csv", "article", ["article_id", "article_name", "retail_selling_price", "sku"])
            .Where(row => !row.TryGetValue("is_active", out var active) || !bool.TryParse(active, out var isActive) || isActive)
            .Take(8)
            .ToList();
        if (articles.Count < 7 || !TaxRate.TryCreate(16m, out var tax))
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
            var totals = SaleTotals.Calculate(subtotal, discountPercent, tax, TaxMode.Excluded);
            Record(new SettledSale(number, DateTime.Now.AddDays(-daysAgo), lines, totals.Subtotal, totals.Total, 0m));
        }

        Add("000120", 1, 0m, Line(0, 1), Line(1, 2));
        Add("000121", 3, 10m, Line(2, 1));
        Add("000122", 10, 0m, Line(3, 1), Line(4, 1), Line(5, 3));
        Add("000123", 45, 0m, Line(6, 1));
    }

    private static string Get(IReadOnlyDictionary<string, string> row, string column) => row.TryGetValue(column, out var value) ? value : string.Empty;
}
