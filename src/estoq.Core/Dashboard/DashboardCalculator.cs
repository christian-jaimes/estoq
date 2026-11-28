using Estoq.Core.Sales;
using Estoq.Core.Setup;

namespace Estoq.Core.Dashboard;

/// <param name="Transactions">Sales rung up.</param>
/// <param name="Returns">Returns taken (one per receipt returned against, or per return logged without one).</param>
public sealed record DashboardTotals(int Transactions, decimal Sales, int Returns, decimal Refunds, int ItemsSold)
{
    public static DashboardTotals Empty { get; } = new(0, 0m, 0, 0m, 0);
}

public enum SeriesGranularity
{
    Daily,
    Weekly,
}

/// <summary>One point of sales over time: the days it covers (a single day for Daily) and the sales in them.</summary>
public sealed record SeriesPoint(DateOnly Start, DateOnly End, decimal Amount);

public sealed record PaymentShare(PaymentMethod Method, decimal Amount, decimal Share);

public sealed record CategoryShare(string Category, decimal Amount, decimal Share);

public sealed record TopProduct(int Rank, string ArticleId, string Name, string Sku, int Units, decimal Sales);

/// <summary>
/// The dashboard figures. Pure functions over <see cref="DashboardLine"/>s. Amounts are in the store's default currency.
/// "Sales" is what customers paid (tax included), before refunds; refunds are shown beside it, not subtracted from it.
/// </summary>
public static class DashboardCalculator
{
    /// <summary>What the customer pays for a line: the listed price, plus tax when prices are listed without it.</summary>
    public static decimal Gross(DashboardLine line, TaxRate rate, TaxMode mode)
    {
        var listed = line.UnitPrice * line.Units;
        return Money.Round(mode == TaxMode.Included ? listed : listed * (1m + rate.Fraction));
    }

    public static IReadOnlyList<DashboardLine> InPeriod(IEnumerable<DashboardLine> lines, DatePeriod period) =>
        [.. lines.Where(line => period.Contains(line.At))];

    /// <summary>Keeps only one person's lines. A line with no seller never counts as anyone's own.</summary>
    public static IReadOnlyList<DashboardLine> OwnedBy(IEnumerable<DashboardLine> lines, string seller) =>
        [.. lines.Where(line => line.Seller.Length > 0 && string.Equals(line.Seller, seller, StringComparison.OrdinalIgnoreCase))];

    public static DashboardTotals Totals(IEnumerable<DashboardLine> lines, DatePeriod period, TaxRate rate, TaxMode mode)
    {
        var inPeriod = InPeriod(lines, period);
        var sold = inPeriod.Where(line => !line.IsReturn).ToList();
        var returned = inPeriod.Where(line => line.IsReturn).ToList();
        if (sold.Count == 0 && returned.Count == 0)
        {
            return DashboardTotals.Empty;
        }

        return new DashboardTotals(
            sold.Select(line => line.Transaction).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            sold.Sum(line => Gross(line, rate, mode)),
            returned.Select(line => line.Transaction).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            returned.Sum(line => Gross(line, rate, mode)),
            sold.Sum(line => line.Units));
    }

    /// <summary>Change against the previous period as a percentage (12.5 means +12.5 %). Null when there is nothing before to compare with.</summary>
    public static decimal? PercentChange(decimal current, decimal previous) =>
        previous == 0m ? null : Math.Round((current - previous) / Math.Abs(previous) * 100m, 1, MidpointRounding.AwayFromZero);

    /// <summary>Sales per day, or per calendar week (Monday to Sunday, clipped to the period), with a point for every empty day or week.</summary>
    public static IReadOnlyList<SeriesPoint> SalesOverTime(
        IEnumerable<DashboardLine> lines, DatePeriod period, SeriesGranularity granularity, TaxRate rate, TaxMode mode)
    {
        var sold = InPeriod(lines, period).Where(line => !line.IsReturn).ToList();
        var points = new List<SeriesPoint>();
        var start = period.Start;
        while (start <= period.End)
        {
            var end = granularity == SeriesGranularity.Daily ? start : EndOfWeek(start) < period.End ? EndOfWeek(start) : period.End;
            var bucket = new DatePeriod(start, end);
            points.Add(new SeriesPoint(start, end, sold.Where(line => bucket.Contains(line.At)).Sum(line => Gross(line, rate, mode))));
            start = end.AddDays(1);
        }

        return points;
    }

    /// <summary>Sales per payment method, biggest first. Shares add up to 1 (before display rounding).</summary>
    public static IReadOnlyList<PaymentShare> ByPayment(IEnumerable<DashboardLine> lines, DatePeriod period, TaxRate rate, TaxMode mode)
    {
        var sold = InPeriod(lines, period).Where(line => !line.IsReturn).ToList();
        var total = sold.Sum(line => Gross(line, rate, mode));
        return [.. sold.GroupBy(line => line.Payment)
            .Select(group => (Method: group.Key, Amount: group.Sum(line => Gross(line, rate, mode))))
            .OrderByDescending(item => item.Amount).ThenBy(item => item.Method)
            .Select(item => new PaymentShare(item.Method, item.Amount, total == 0m ? 0m : item.Amount / total))];
    }

    /// <summary>Sales per category, biggest first.</summary>
    public static IReadOnlyList<CategoryShare> ByCategory(IEnumerable<DashboardLine> lines, DatePeriod period, TaxRate rate, TaxMode mode)
    {
        var sold = InPeriod(lines, period).Where(line => !line.IsReturn).ToList();
        var total = sold.Sum(line => Gross(line, rate, mode));
        return [.. sold.GroupBy(line => line.Category, StringComparer.OrdinalIgnoreCase)
            .Select(group => (Category: group.Key, Amount: group.Sum(line => Gross(line, rate, mode))))
            .OrderByDescending(item => item.Amount).ThenBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
            .Select(item => new CategoryShare(item.Category, item.Amount, total == 0m ? 0m : item.Amount / total))];
    }

    /// <summary>The best sellers by units (ties by sales, then name), ranked from 1.</summary>
    public static IReadOnlyList<TopProduct> TopProducts(IEnumerable<DashboardLine> lines, DatePeriod period, TaxRate rate, TaxMode mode, int count = 5)
    {
        var ranked = InPeriod(lines, period).Where(line => !line.IsReturn)
            .GroupBy(line => line.ArticleId, StringComparer.OrdinalIgnoreCase)
            .Select(group => (Line: group.First(), Units: group.Sum(line => line.Units), Sales: group.Sum(line => Gross(line, rate, mode))))
            .OrderByDescending(item => item.Units).ThenByDescending(item => item.Sales).ThenBy(item => item.Line.Name, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .ToList();
        return [.. ranked.Select((item, index) => new TopProduct(index + 1, item.Line.ArticleId, item.Line.Name, item.Line.Sku, item.Units, item.Sales))];
    }

    private static DateOnly EndOfWeek(DateOnly day) => day.AddDays((7 - (int)day.DayOfWeek) % 7);
}
